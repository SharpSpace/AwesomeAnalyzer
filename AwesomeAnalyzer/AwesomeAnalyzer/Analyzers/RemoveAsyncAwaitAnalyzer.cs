using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using FleetManagement.Service;
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
            DiagnosticDescriptors.Rule0006RemoveAsyncAwait
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            using (var _ = new MeasureTime())
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0006RemoveAsyncAwait.Id))
                {
                    return;
                }

                var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
                if (methodDeclarationSyntax.AttributeLists.Any(
                        x => x.Attributes.Any(
                            y =>
                                y.Name.ToFullString() == "TestMethod" || y.Name.ToFullString() == "Fact"
                        )
                    )
                   ) return;
                if (methodDeclarationSyntax.Modifiers.All(x => x.ValueText != Textasync)) return;
                if (methodDeclarationSyntax.Body == null)
                {
                    if (methodDeclarationSyntax.ExpressionBody == null) return;

                    if (!(methodDeclarationSyntax.ExpressionBody.Expression is AwaitExpressionSyntax)) return;

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.Rule0006RemoveAsyncAwait,
                            methodDeclarationSyntax.Identifier.GetLocation(),
                            methodDeclarationSyntax.Identifier.ValueText
                        )
                    );
                    return;
                }

                if (Regex.Matches(methodDeclarationSyntax.Body.ToFullString(), "await ").Count > 1) return;
                var lastStatement = methodDeclarationSyntax.Body.Statements.Last();
                if (!((lastStatement as ExpressionStatementSyntax)?.Expression is AwaitExpressionSyntax)) return;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Rule0006RemoveAsyncAwait,
                        methodDeclarationSyntax.Identifier.GetLocation(),
                        methodDeclarationSyntax.Identifier.ValueText
                    )
                );
            }
        }
    }
}