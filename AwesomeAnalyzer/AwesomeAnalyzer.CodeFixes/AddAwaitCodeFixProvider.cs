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
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class AddAwaitCodeFixProvider : CodeFixProvider
    {
        private const string TextAsync = "async";
        private const string TextTask = "Task";
        private const string TextAwait = "await ";

        public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.Rule0101AddAwait.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent
                ?.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .First();

                if (declaration == null) continue;
                if (declaration.Parent is AwaitExpressionSyntax) continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add Await",
                        token => AddAwaitAsync(context.Document, declaration, token),
                        equivalenceKey: "AddAwaitCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Document> AddAwaitAsync(
            Document document,
            InvocationExpressionSyntax declaration,
            CancellationToken token
        )
        {
            var methodDeclarationSyntax = declaration.HasParent<MethodDeclarationSyntax>();

            var oldSource = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false))?.ToFullString();
            if (oldSource == null)
            {
                return document;
            }

            string newSource;
            if (methodDeclarationSyntax != null &&
                methodDeclarationSyntax.Modifiers.Any(x => x.ValueText == TextAsync) == false
               )
            {
                var methodCode = methodDeclarationSyntax.ToString();

                var newType = SyntaxFactory.ParseTypeName(TextTask)
                .WithLeadingTrivia(SyntaxFactory.Space)
                .WithAdditionalAnnotations(Simplifier.Annotation)
                .WithTrailingTrivia(methodDeclarationSyntax.ReturnType.GetTrailingTrivia());

                var modifiers =
                methodDeclarationSyntax.Modifiers.Union(new[] { SyntaxFactory.Token(SyntaxKind.AsyncKeyword) });
                var newMethodCode = methodDeclarationSyntax
                .WithModifiers(SyntaxFactory.TokenList(modifiers))
                .WithReturnType(newType)
                .ToString();

                var oldDeclaration = declaration.ToString();
                newMethodCode = newMethodCode.Replace(oldDeclaration, $"{TextAwait}{oldDeclaration}");

                newSource = oldSource.Replace(methodCode, newMethodCode);
            }
            else
            {
                newSource = oldSource.Insert(declaration.SpanStart, TextAwait);
            }

            return document.WithText(SourceText.From(newSource));
        }
    }
}