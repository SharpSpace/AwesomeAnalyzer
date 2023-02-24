using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RemoveAsyncAwaitAnalyzer : DiagnosticAnalyzer
    {
        private const string Textasync = "async";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.RemoveAsyncAwaitRule0006
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
            if (methodDeclarationSyntax.Modifiers.All(x => x.ValueText != Textasync)) return;
            if (methodDeclarationSyntax.Body == null)
            {
                if (methodDeclarationSyntax.ExpressionBody == null) return;

                if (!(methodDeclarationSyntax.ExpressionBody.Expression is AwaitExpressionSyntax)) return;

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.RemoveAsyncAwaitRule0006,
                    methodDeclarationSyntax.Identifier.GetLocation(),
                    methodDeclarationSyntax.Identifier.ValueText
                ));
                return;
            }

            if (Regex.Matches(methodDeclarationSyntax.Body.ToFullString(), "await ").Count > 1) return;
            var lastStatement = methodDeclarationSyntax.Body.Statements.Last();
            if (!((lastStatement as ExpressionStatementSyntax)?.Expression is AwaitExpressionSyntax)) return;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.RemoveAsyncAwaitRule0006,
                methodDeclarationSyntax.Identifier.GetLocation(),
                methodDeclarationSyntax.Identifier.ValueText
            ));
        }
    }
}