using System.Collections.Immutable;
using System.Linq;
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
            context.RegisterSyntaxNodeAction(AnalyzeLambdaExpressionNode, SyntaxKind.ParenthesizedLambdaExpression);
        }

        private static void AnalyzeLambdaExpressionNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0006RemoveAsyncAwait.Id))
            {
                return;
            }

            var node = (ParenthesizedLambdaExpressionSyntax)context.Node;
            if (!node.ToString().StartsWith("async")) return;

            // Only trigger if awaiting a completion task (no actual work happening)
            var awaitExpressions = node.DescendantNodes().OfType<AwaitExpressionSyntax>().ToList();
            if (awaitExpressions.Count != 1) return;
            
            var awaitExpression = awaitExpressions[0];
            if (!IsCompletionTask(awaitExpression.Expression)) return;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.Rule0006RemoveAsyncAwait,
                    node.GetLocation(),
                    node.ToString()
                )
            );
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            using (_ = new MeasureTime(true))
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0006RemoveAsyncAwait.Id))
                {
                    return;
                }

                var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
                if (methodDeclarationSyntax.AttributeLists.Any(x =>
                    x.Attributes.Any(y =>
                        y.Name.ToFullString() == "TestMethod" ||
                        y.Name.ToFullString() == "Fact"
                    )
                )) return;
                if (methodDeclarationSyntax.Modifiers.All(x => x.ValueText != Textasync)) return;
                
                // Handle expression-bodied methods
                if (methodDeclarationSyntax.Body == null)
                {
                    if (methodDeclarationSyntax.ExpressionBody == null) return;

                    if (!(methodDeclarationSyntax.ExpressionBody.Expression is AwaitExpressionSyntax awaitExpr)) return;
                    
                    // Only trigger if awaiting a completion task (no actual work happening)
                    if (!IsCompletionTask(awaitExpr.Expression)) return;

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.Rule0006RemoveAsyncAwait,
                            methodDeclarationSyntax.Identifier.GetLocation(),
                            methodDeclarationSyntax.Identifier.ValueText
                        )
                    );
                    return;
                }

                // Handle methods with body
                var awaitCount = methodDeclarationSyntax.Body.DescendantNodes().OfType<AwaitExpressionSyntax>().Count();
                if (awaitCount > 1) return;
                var lastStatement = methodDeclarationSyntax.Body.Statements.Last();
                if (!((lastStatement as ExpressionStatementSyntax)?.Expression is AwaitExpressionSyntax awaitExpression)) return;

                // Only trigger if awaiting a completion task (no actual work happening)
                if (!IsCompletionTask(awaitExpression.Expression)) return;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Rule0006RemoveAsyncAwait,
                        methodDeclarationSyntax.Identifier.GetLocation(),
                        methodDeclarationSyntax.Identifier.ValueText
                    )
                );
            }
        }

        /// <summary>
        /// Checks if the expression is a completion task that doesn't do any actual work.
        /// Examples: Task.CompletedTask, Task.FromResult(...), new ValueTask(...)
        /// </summary>
        private static bool IsCompletionTask(ExpressionSyntax expression)
        {
            // Remove ConfigureAwait if present
            var expr = expression;
            if (expr is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText == "ConfigureAwait")
            {
                expr = memberAccess.Expression;
            }

            var exprString = expr.ToString();
            
            // Check for Task.CompletedTask
            if (exprString.Contains("Task.CompletedTask"))
                return true;
            
            // Check for Task.FromResult
            if (exprString.Contains("Task.FromResult"))
                return true;
            
            // Check for ValueTask construction
            if (exprString.StartsWith("new ValueTask"))
                return true;
            
            // Check for Task.Delay(0)
            if (exprString.Contains("Task.Delay(0)"))
                return true;

            return false;
        }
    }
}