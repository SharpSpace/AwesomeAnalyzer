using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class MakeSealedCodeFixProvider : CodeFixProvider
    {
        private const string TextSealed = "sealed ";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.MakeSealedRule0001.Id);

        public override FixAllProvider GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.MakeSealedCodeFixTitle,
                    createChangedDocument: async token =>
                    {
                        var text = await context.Document.GetTextAsync(token).ConfigureAwait(false);
                        return context.Document.WithText(
                            text.Replace(context.Diagnostics.First().Location.SourceSpan.Start - 6, 0, TextSealed)
                        );
                    },
                    equivalenceKey: nameof(CodeFixResources.MakeSealedCodeFixTitle)
                ),
                context.Diagnostics
            );
        }
    }
}