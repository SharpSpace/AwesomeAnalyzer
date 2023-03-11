using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DisposedAnalyzer : DiagnosticAnalyzer
    {
        private const string TextUsing = "using";
        private const string TextIDisposable = "IDisposable";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0004Disposed
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0004Disposed.Id))
            {
                return;
            }

            if (!(context.Node is ObjectCreationExpressionSyntax objectCreationExpressionSyntax)) return;
            if (!(objectCreationExpressionSyntax.Parent is EqualsValueClauseSyntax equalsValueClauseSyntax)) return;
            if (!(equalsValueClauseSyntax.Parent is VariableDeclaratorSyntax variableDeclaratorSyntax)) return;
            if (!(variableDeclaratorSyntax.Parent is VariableDeclarationSyntax variableDeclarationSyntax)) return;
            if (!(variableDeclarationSyntax.Parent is LocalDeclarationStatementSyntax localDeclarationStatementSyntax))
                return;

            if (localDeclarationStatementSyntax.UsingKeyword.ValueText == TextUsing) return;

            if (!(localDeclarationStatementSyntax.Parent is BlockSyntax blockSyntax)) return;

            var expression = variableDeclaratorSyntax.Identifier.ValueText;
            if (blockSyntax.Statements
                .OfType<ExpressionStatementSyntax>()
                .Any(
                    x =>
                    x.Expression.ToString().StartsWith(expression)
                )
               ) return;

            var typeSymbol = ModelExtensions.GetTypeInfo(context.SemanticModel, objectCreationExpressionSyntax).Type;
            if (typeSymbol == null) return;

            var interfaces = typeSymbol.AllInterfaces;
            if (interfaces.Any(x => x.Name == TextIDisposable) == false) return;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.Rule0004Disposed,
                    variableDeclarationSyntax.GetLocation(),
                    messageArgs: variableDeclarationSyntax.ToString()
                )
            );
        }
    }
}