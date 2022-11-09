using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AddAsyncAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.MakeAsyncRule0102
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax invocationExpressionSyntax)) return;

            if (!(invocationExpressionSyntax.Expression is IdentifierNameSyntax identifierNameSyntax)) return;

            if (identifierNameSyntax.Identifier.ValueText.EndsWith("Async")) return;

            var typeSymbol = context.SemanticModel.GetTypeInfo(invocationExpressionSyntax);
            if (typeSymbol.Type.Name != "Task") return;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MakeAsyncRule0102,
                identifierNameSyntax.Identifier.GetLocation(),
                messageArgs: identifierNameSyntax.Identifier.ValueText
            ));
        }
    }
}