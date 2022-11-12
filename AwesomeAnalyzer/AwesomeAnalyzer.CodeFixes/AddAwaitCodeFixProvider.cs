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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class AddAwaitCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.AddAwaitRule0101.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent
                    .AncestorsAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .First();

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
            if (declaration.Parent is AwaitExpressionSyntax) return document;

            var methodDeclarationSyntax = declaration.HasParent<MethodDeclarationSyntax>();

            var oldSource = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false)).ToFullString();
            string newSource;
            if (methodDeclarationSyntax != null && 
                methodDeclarationSyntax.Modifiers.Any(x => x.ValueText == "async") == false
            )
            {
                var methodCode = methodDeclarationSyntax.ToString();

                var newType = SyntaxFactory.ParseTypeName("Task")
                    .WithLeadingTrivia(SyntaxFactory.Space)
                    .WithAdditionalAnnotations(Simplifier.Annotation)
                    .WithTrailingTrivia(methodDeclarationSyntax.ReturnType.GetTrailingTrivia());

                var modifiers = methodDeclarationSyntax.Modifiers.Union(new []{ SyntaxFactory.Token(SyntaxKind.AsyncKeyword) });
                var newMethodCode = methodDeclarationSyntax
                    .WithModifiers(SyntaxFactory.TokenList(modifiers))
                    .WithReturnType(newType)
                    .ToString();

                var oldDeclaration = declaration.ToString();
                newMethodCode = newMethodCode.Replace(oldDeclaration, $"await {oldDeclaration}");

                newSource = oldSource.Replace(methodCode, newMethodCode);
            }
            else
            {
                newSource = oldSource.Insert(declaration.SpanStart, "await ");
            }


            return document.WithText(SourceText.From(newSource));
        }
    }
}