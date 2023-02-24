using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class RemoveAsyncAwaitCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.RemoveAsyncAwaitRule0006.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = (MethodDeclarationSyntax)root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Remove async and await",
                        token => DoCodeFix(context.Document, declaration, token),
                        equivalenceKey: "MakeAsyncCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Document> DoCodeFix(Document document, MethodDeclarationSyntax methodDeclarationSyntax, CancellationToken token)
        {
            if (methodDeclarationSyntax.Body != null)
            {
                var lastStatement = (ExpressionStatementSyntax)methodDeclarationSyntax.Body.Statements.Last();
                var awaitExpressionSyntax = (AwaitExpressionSyntax)lastStatement.Expression;
                var leadingTrivia = lastStatement.GetLeadingTrivia();
                var trailingTrivia = lastStatement.GetTrailingTrivia();

                var statements = methodDeclarationSyntax.Body.Statements
                    .RemoveAt(methodDeclarationSyntax.Body.Statements.Count - 1)
                    .Add(
                        SyntaxFactory.ReturnStatement(
                            RemoveConfigureAwait(awaitExpressionSyntax).WithLeadingTrivia(SyntaxFactory.Space)
                        )
                        .WithLeadingTrivia(leadingTrivia)
                        .WithTrailingTrivia(trailingTrivia)
                    );

                var body = methodDeclarationSyntax.Body.WithStatements(statements);

                var newMethodDeclarationSyntax = methodDeclarationSyntax
                    .WithModifiers(
                        methodDeclarationSyntax.Modifiers.Remove(methodDeclarationSyntax.Modifiers.First(x => x.ValueText == "async"))
                    )
                    .WithBody(body);

                var sourceText = await document.GetTextAsync(token).ConfigureAwait(false);
                sourceText = sourceText.Replace(
                    methodDeclarationSyntax.FullSpan,
                    newMethodDeclarationSyntax.ToFullString()
                );

                return document.WithText(sourceText);
            }

            if (methodDeclarationSyntax.ExpressionBody != null)
            {
                var awaitExpressionSyntax = (AwaitExpressionSyntax)methodDeclarationSyntax.ExpressionBody.Expression;
                var leadingTrivia = methodDeclarationSyntax.ExpressionBody.Expression.GetLeadingTrivia();
                var trailingTrivia = methodDeclarationSyntax.ExpressionBody.GetTrailingTrivia();
                var lastStatement = methodDeclarationSyntax.ExpressionBody
                    .WithExpression(
                        RemoveConfigureAwait(awaitExpressionSyntax)
                            .WithLeadingTrivia(leadingTrivia)
                            .WithTrailingTrivia(trailingTrivia)
                    );

                var newMethodDeclarationSyntax = methodDeclarationSyntax
                    .WithModifiers(
                        methodDeclarationSyntax.Modifiers.Remove(
                            methodDeclarationSyntax.Modifiers.First(x => x.ValueText == "async")
                        )
                    )
                    .WithExpressionBody(lastStatement);

                var sourceText = await document.GetTextAsync(token).ConfigureAwait(false);
                sourceText = sourceText.Replace(
                    methodDeclarationSyntax.FullSpan,
                    newMethodDeclarationSyntax.ToFullString()
                );

                return document.WithText(sourceText);
            }

            return document;
        }

        private static ExpressionSyntax RemoveConfigureAwait(AwaitExpressionSyntax awaitExpressionSyntax)
        {
            var syntax = awaitExpressionSyntax.Expression;

            if (awaitExpressionSyntax.Expression is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                var memberAccessExpressionSyntax2 = (MemberAccessExpressionSyntax)invocationExpressionSyntax.Expression;
                syntax = memberAccessExpressionSyntax2.Expression;
            }

            return syntax;
        }
    }
}