using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FleetManagement.Service;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MakeConstAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0003MakeConst
        );

        private static Dictionary<SyntaxNode, Diagnostic> _cache = new Dictionary<SyntaxNode, Diagnostic>();

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            using (var _ = new MeasureTime())
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0003MakeConst.Id))
                {
                    return;
                }

                if (_cache.ContainsKey(context.Node))
                {
                    var diagnostic = _cache[context.Node];
                    if (diagnostic == null) return;
                    context.ReportDiagnostic(diagnostic);
                }

                var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

                if (localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword)) return;

                var variableTypeName = localDeclaration.Declaration.Type;
                var variableType = ModelExtensions.GetTypeInfo(
                        context.SemanticModel,
                        variableTypeName,
                        context.CancellationToken
                    )
                    .ConvertedType;
                if (variableType == null) return;

                var variables = localDeclaration.Declaration.Variables;
                var variableDeclaratorSyntaxes = Enumerable.Range(0, variables.Count)
                    .Select(x => variables[x])
                    .ToImmutableList()
                    ;

                foreach (var initializer in variableDeclaratorSyntaxes.Select(s => s.Initializer))
                {
                    if (initializer == null) return;

                    var constantValue = context.SemanticModel.GetConstantValue(
                        initializer.Value,
                        context.CancellationToken
                    );
                    if (!constantValue.HasValue) return;

                    if (constantValue.Value is string)
                    {
                        if (variableType.SpecialType != SpecialType.System_String) return;
                    }
                    else if (variableType.IsReferenceType && constantValue.Value != null)
                    {
                        return;
                    }

                    var conversion = context.SemanticModel.ClassifyConversion(initializer.Value, variableType);
                    if (!conversion.Exists || conversion.IsUserDefined) return;
                }

                var dataFlowAnalysis = ModelExtensions.AnalyzeDataFlow(context.SemanticModel, localDeclaration);

                if (variableDeclaratorSyntaxes
                    .Select(x => ModelExtensions.GetDeclaredSymbol(context.SemanticModel, x, context.CancellationToken))
                    .Any(x => dataFlowAnalysis.WrittenOutside.Contains(x)))
                {
                    return;
                }

                var diagnostic1 = Diagnostic.Create(
                    DiagnosticDescriptors.Rule0003MakeConst,
                    context.Node.GetLocation(),
                    string.Join(",", variableDeclaratorSyntaxes.Select(x => x.Identifier.ValueText))
                );
                _cache.Add(context.Node, diagnostic1);
                context.ReportDiagnostic(
                    diagnostic1
                );
            }
        }
    }
}