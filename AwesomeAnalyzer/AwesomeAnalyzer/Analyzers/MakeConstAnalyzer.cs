using System.Collections.Immutable;
using System.Linq;
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

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0003MakeConst.Id))
            {
                return;
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

            foreach (var initializer in localDeclaration.Declaration.Variables.Select(s => s.Initializer))
            {
                if (initializer == null) return;

                var constantValue = context.SemanticModel.GetConstantValue(
                    initializer.Value,
                    context.CancellationToken
                );
                if (!constantValue.HasValue) return;

                var conversion = context.SemanticModel.ClassifyConversion(initializer.Value, variableType);
                if (!conversion.Exists || conversion.IsUserDefined) return;

                if (constantValue.Value is string)
                {
                    if (variableType.SpecialType != SpecialType.System_String) return;
                }
                else if (variableType.IsReferenceType && constantValue.Value != null)
                {
                    return;
                }
            }

            var dataFlowAnalysis = ModelExtensions.AnalyzeDataFlow(context.SemanticModel, localDeclaration);

            if (localDeclaration.Declaration.Variables
                .Select(x => ModelExtensions.GetDeclaredSymbol(context.SemanticModel, x, context.CancellationToken))
                .Any(x => dataFlowAnalysis.WrittenOutside.Contains(x)))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.Rule0003MakeConst,
                    context.Node.GetLocation(),
                    string.Join(",", localDeclaration.Declaration.Variables.Select(x => x.Identifier.ValueText))
                )
            );
        }
    }
}