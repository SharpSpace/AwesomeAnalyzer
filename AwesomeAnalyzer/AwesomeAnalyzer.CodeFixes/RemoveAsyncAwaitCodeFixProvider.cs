using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveAsyncAwaitCodeFixProvider))]
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
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Remove async and await",
                        token => DoCodeFixAsync(context.Document, declaration, token),
                        equivalenceKey: "MakeAsyncCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private static async Task<Document> DoCodeFixAsync(
            Document document,
            SyntaxNode syntaxNode,
            CancellationToken token)
        {
            var sourceText = await document.GetTextAsync(token).ConfigureAwait(false);
            var replaceList = new List<(TextSpan, string)>();

            if (syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                MethodDeclarationSyntax newMethodDeclarationSyntax = null;

                if (methodDeclarationSyntax.Body != null)
                {
                    newMethodDeclarationSyntax = DoCodeFixBodyAsync(methodDeclarationSyntax);
                }

                if (methodDeclarationSyntax.ExpressionBody != null)
                {
                    newMethodDeclarationSyntax = DoCodeFixExpressionBodyAsync(methodDeclarationSyntax);
                }

                foreach (var returnStatementSyntax in syntaxNode.DescendantNodes().OfType<ReturnStatementSyntax>())
                {
                    if (returnStatementSyntax.ToString() == "return;")
                    {
                        replaceList.Add((new TextSpan(returnStatementSyntax.Span.Start - 6, returnStatementSyntax.Span.Length), "return Task.CompletedTask;"));
                    }
                }

                replaceList = replaceList.OrderByDescending(x => x.Item1.Start).ToList();
                replaceList.Insert(
                    0,
                    (
                        new TextSpan(syntaxNode.Span.Start, syntaxNode.Span.Length),
                        newMethodDeclarationSyntax?.ToFullString() ?? string.Empty
                    )
                );
            }
            else
            {
                var parenthesizedLambdaExpressionSyntax = (ParenthesizedLambdaExpressionSyntax)syntaxNode;

                replaceList.Add((
                    new TextSpan(
                        parenthesizedLambdaExpressionSyntax.AsyncKeyword.Span.Start,
                        parenthesizedLambdaExpressionSyntax.AsyncKeyword.Span.Length + 1
                    ),
                    string.Empty
                ));

                var awaitExpressionSyntaxes = parenthesizedLambdaExpressionSyntax.DescendantNodes().OfType<AwaitExpressionSyntax>().First();

                replaceList.Add((
                    new TextSpan(
                        awaitExpressionSyntaxes.AwaitKeyword.Span.Start,
                        awaitExpressionSyntaxes.AwaitKeyword.Span.Length + 1
                    ),
                    string.Empty
                ));

                replaceList = replaceList.OrderByDescending(x => x.Item1.Start).ToList();
            }

            sourceText = replaceList
                .Aggregate(
                    sourceText,
                    (text, tuple) =>
                    {
                        var start = tuple.Item1.Start;
                        var length = tuple.Item1.Length;
                        return text.Replace(
                            start,
                            length,
                            tuple.Item2
                        );
                    }
                );

            return document.WithText(sourceText);
        }

        private static MethodDeclarationSyntax DoCodeFixBodyAsync(MethodDeclarationSyntax methodDeclarationSyntax)
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

            var newMethodDeclarationSyntax = methodDeclarationSyntax
                .WithReturnType(GetReturnType(methodDeclarationSyntax, expressionSyntax))
                .WithModifiers(
                    methodDeclarationSyntax.Modifiers.Remove(
                        Enumerable.Range(0, methodDeclarationSyntax.Modifiers.Count)
                            .Select(x => methodDeclarationSyntax.Modifiers[x])
                            .First(x => x.ValueText == TextAsync)
                    )
                )
                .WithBody(body)
                .WithoutLeadingTrivia()
                .WithoutTrailingTrivia();
            return newMethodDeclarationSyntax;
        }

        private static MethodDeclarationSyntax DoCodeFixExpressionBodyAsync(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            MethodDeclarationSyntax newMethodDeclarationSyntax;
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
                        Enumerable.Range(0, methodDeclarationSyntax.Modifiers.Count).Select(x => methodDeclarationSyntax.Modifiers[x]).First(x => x.ValueText == TextAsync)
                    )
                )
                .WithExpressionBody(lastStatement)
                .WithoutLeadingTrivia()
                .WithoutTrailingTrivia();
            return newMethodDeclarationSyntax;
        }

        private static TypeSyntax GetReturnType(
            MethodDeclarationSyntax methodDeclarationSyntax,
            ExpressionSyntax expressionSyntax)
        {
            if (expressionSyntax is ObjectCreationExpressionSyntax objectCreationExpressionSyntax)
            {
                return objectCreationExpressionSyntax.Type.WithTrailingTrivia(SyntaxFactory.Space);
            }

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