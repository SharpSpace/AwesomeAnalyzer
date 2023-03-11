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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class RemoveAsyncAwaitCodeFixProvider : CodeFixProvider
    {
        private const string TextAsync = "async";

        public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.Rule0006RemoveAsyncAwait.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

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

        private async Task<Document> DoCodeFix(
            Document document,
            MethodDeclarationSyntax methodDeclarationSyntax,
            CancellationToken token)
        {
            MethodDeclarationSyntax newMethodDeclarationSyntax = null;
            if (methodDeclarationSyntax.Body != null)
            {
                var lastStatement = (ExpressionStatementSyntax)methodDeclarationSyntax.Body.Statements.Last();
                var awaitExpressionSyntax = (AwaitExpressionSyntax)lastStatement.Expression;
                var leadingTrivia = lastStatement.GetLeadingTrivia();
                var trailingTrivia = lastStatement.GetTrailingTrivia();

                var expressionSyntax = RemoveConfigureAwait(awaitExpressionSyntax);
                var statements = methodDeclarationSyntax.Body.Statements
                .RemoveAt(methodDeclarationSyntax.Body.Statements.Count - 1)
                .Add(
                    SyntaxFactory.ReturnStatement(
                        expressionSyntax.WithLeadingTrivia(SyntaxFactory.Space)
                    )
                    .WithLeadingTrivia(leadingTrivia)
                    .WithTrailingTrivia(trailingTrivia)
                );

                var body = methodDeclarationSyntax.Body.WithStatements(statements);

                newMethodDeclarationSyntax = methodDeclarationSyntax
                .WithReturnType(GetReturnType(methodDeclarationSyntax, expressionSyntax))
                .WithModifiers(
                    methodDeclarationSyntax.Modifiers.Remove(
                        methodDeclarationSyntax.Modifiers.First(x => x.ValueText == TextAsync)
                    )
                )
                .WithBody(body);
            }

            if (methodDeclarationSyntax.ExpressionBody != null)
            {
                var awaitExpressionSyntax = (AwaitExpressionSyntax)methodDeclarationSyntax.ExpressionBody.Expression;
                var leadingTrivia = methodDeclarationSyntax.ExpressionBody.Expression.GetLeadingTrivia();
                var trailingTrivia = methodDeclarationSyntax.ExpressionBody.GetTrailingTrivia();

                var expressionSyntax = RemoveConfigureAwait(awaitExpressionSyntax);
                var lastStatement = methodDeclarationSyntax.ExpressionBody
                .WithExpression(
                    expressionSyntax
                    .WithLeadingTrivia(leadingTrivia)
                    .WithTrailingTrivia(trailingTrivia)
                );

                newMethodDeclarationSyntax = methodDeclarationSyntax
                .WithReturnType(GetReturnType(methodDeclarationSyntax, expressionSyntax))
                .WithModifiers(
                    methodDeclarationSyntax.Modifiers.Remove(
                        methodDeclarationSyntax.Modifiers.First(x => x.ValueText == TextAsync)
                    )
                )
                .WithExpressionBody(lastStatement);
            }

            var sourceText = await document.GetTextAsync(token).ConfigureAwait(false);
            sourceText = sourceText.Replace(
                methodDeclarationSyntax.FullSpan,
                newMethodDeclarationSyntax?.ToFullString() ?? string.Empty
            );

            return document.WithText(sourceText);
        }

        private static TypeSyntax GetReturnType(
            MethodDeclarationSyntax methodDeclarationSyntax,
            ExpressionSyntax expressionSyntax)
        {
            if (expressionSyntax is ObjectCreationExpressionSyntax objectCreationExpressionSyntax)
            {
                return objectCreationExpressionSyntax.Type.WithTrailingTrivia(SyntaxFactory.Space);
            }

            Debug.WriteLine(expressionSyntax.GetType().Name);
            Debug.WriteLine(expressionSyntax.WithLeadingTrivia().ToFullString());

            return methodDeclarationSyntax.ReturnType;
        }

        private static ExpressionSyntax RemoveConfigureAwait(AwaitExpressionSyntax awaitExpressionSyntax)
        {
            var syntax = awaitExpressionSyntax.Expression;

            if (!(awaitExpressionSyntax.Expression is InvocationExpressionSyntax invocationExpressionSyntax))
            {
                return syntax;
            }

            if (!(invocationExpressionSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax))
            {
                return syntax;
            }

            if (memberAccessExpressionSyntax.Name.Identifier.ValueText == "ConfigureAwait")
            {
                syntax = memberAccessExpressionSyntax.Expression.WithoutTrailingTrivia();
            }

            return syntax;
        }
    }
}