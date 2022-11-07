using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class DisposedCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.DisposedRule0004.Id
        );

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent
                    .AncestorsAndSelf()
                    .OfType<LocalDeclarationStatementSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add using to statement",
                        token => AddUsingAsync(context.Document, declaration, token),
                        equivalenceKey: "DisposedCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Document> AddUsingAsync(Document document, LocalDeclarationStatementSyntax declaration, CancellationToken token)
        {
            if (declaration.UsingKeyword.ValueText == "using") return document;

            var oldSource = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false)).ToFullString();
            var newSource = oldSource.Insert(declaration.SpanStart, "using ");

            return document.WithText(SourceText.From(newSource));
        }
    }
}