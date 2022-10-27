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

using Document = Microsoft.CodeAnalysis.Document;

namespace Analyzer1
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public class MakeAsyncCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MakeAsyncAnalyzer.DiagnosticId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Remove Async in name",
                        token => MakeAsyncAsync(context.Document, declaration, token),
                        equivalenceKey: "MakeAsyncCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Document> MakeAsyncAsync(
            Document document, 
            MethodDeclarationSyntax localDeclaration, 
            CancellationToken token
        )
        {
            var oldSource = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false)).ToFullString();

            var span = localDeclaration.Identifier.Span;
            var oldCode = oldSource.Substring(span.Start, span.Length);
            var newCode = oldCode.Replace("Async", string.Empty);

            var newSource = $"{oldSource.Substring(0, span.Start)}{newCode}{oldSource.Substring(span.End)}";

            return document.WithText(SourceText.From(newSource));
        }
    }
}