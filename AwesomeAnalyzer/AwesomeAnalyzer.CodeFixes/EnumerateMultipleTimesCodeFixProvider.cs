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

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumerateMultipleTimesCodeFixProvider))]
    [Shared]
    public sealed class EnumerateMultipleTimesCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0010EnumerateMultipleTimes.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var variable = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent?.AncestorsAndSelf()
                    .OfType<VariableDeclaratorSyntax>()
                    .FirstOrDefault();

                if (variable?.Initializer == null) continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Convert to list",
                        createChangedDocument: token => ConvertToListAsync(context.Document, variable, token),
                        equivalenceKey: "EnumerateMultipleTimesCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private static async Task<Document> ConvertToListAsync(
            Document document,
            VariableDeclaratorSyntax variable,
            CancellationToken cancellationToken)
        {
            if (variable.Initializer == null) return document;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            var initializerExpression = variable.Initializer.Value;

            // Parenthesize if the expression has lower precedence than member access,
            // so that .ToList() binds to the full expression (e.g. (a ?? b).ToList()).
            ExpressionSyntax target;
            if (NeedsParentheses(initializerExpression))
            {
                target = SyntaxFactory.ParenthesizedExpression(
                    initializerExpression.WithoutTrivia()
                ).WithTriviaFrom(initializerExpression);
            }
            else
            {
                target = initializerExpression;
            }

            var toListInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    target.WithoutTrailingTrivia(),
                    SyntaxFactory.IdentifierName("ToList")
                )
            ).WithTrailingTrivia(initializerExpression.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(initializerExpression, toListInvocation);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return await EnsureUsingSystemLinqAsync(newDocument, cancellationToken).ConfigureAwait(false);
        }

        private static bool NeedsParentheses(ExpressionSyntax expression)
        {
            return expression is ConditionalExpressionSyntax ||
                   expression is BinaryExpressionSyntax ||
                   expression is AssignmentExpressionSyntax ||
                   expression is AwaitExpressionSyntax ||
                   expression is CastExpressionSyntax ||
                   expression is ThrowExpressionSyntax ||
                   expression is LambdaExpressionSyntax ||
                   expression is QueryExpressionSyntax;
        }

        private static async Task<Document> EnsureUsingSystemLinqAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!(root is CompilationUnitSyntax compilationUnit)) return document;

            bool alreadyPresent = compilationUnit.Usings.Any(u =>
                u.Alias == null &&
                !u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) &&
                string.Equals(u.Name?.ToFullString().Trim(), "System.Linq", System.StringComparison.Ordinal)
            );
            if (alreadyPresent) return document;

            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName("System"),
                    SyntaxFactory.IdentifierName("Linq")
                ).WithLeadingTrivia(SyntaxFactory.Space)
            ).WithTrailingTrivia(SyntaxFactory.ElasticLineFeed);

            var newRoot = compilationUnit.AddUsings(usingDirective);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
