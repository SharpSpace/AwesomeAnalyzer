using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AddAsyncAnalyzer : DiagnosticAnalyzer
    {
        private const string TextAsync = "Async";
        private const string TextTask = "Task";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.AddAsyncRule0102
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpressionSyntax = context.Node as InvocationExpressionSyntax;
            if (!(invocationExpressionSyntax?.Expression is IdentifierNameSyntax identifierNameSyntax)) return;

            if (identifierNameSyntax.Span.Length > TextAsync.Length) return;

            var identifierValueText = identifierNameSyntax.Identifier.ValueText.AsSpan();
            if (identifierValueText.EndsWith(TextAsync.AsSpan())) return;

            var typeSymbol = ModelExtensions.GetTypeInfo(context.SemanticModel, invocationExpressionSyntax);
            if (typeSymbol.Type == null) return;
            if (typeSymbol.Type.Name != TextTask) return;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AddAsyncRule0102,
                identifierNameSyntax.Identifier.GetLocation(),
                messageArgs: identifierValueText.ToString()
            ));
        }
    }
}