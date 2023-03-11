using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class MakeSealedCodeFixProvider : CodeFixProvider
    {
        private const string TextSealed = "sealed ";
        private const string TextPartial = "partial ";
        private const string TextClass = "class ";

        public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.Rule0001MakeSealed.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.MakeSealedCodeFixTitle,
                    createChangedDocument: async token =>
                    {
                        var text = await context.Document.GetTextAsync(token).ConfigureAwait(false);
                        var spanStart = context.Diagnostics.First().Location.SourceSpan.Start - TextClass.Length;
                        if (spanStart > 7)
                        {
                            var prevStatement = text.GetSubText(
                                TextSpan.FromBounds(spanStart - TextPartial.Length, spanStart)
                            );
                            if (prevStatement.ToString() == TextPartial)
                            {
                                spanStart -= TextPartial.Length;
                            }
                        }

                        return context.Document.WithText(
                            text.Replace(spanStart, 0, TextSealed)
                        );
                    },
                    equivalenceKey: nameof(CodeFixResources.MakeSealedCodeFixTitle)
                ),
                context.Diagnostics
            );

            return Task.CompletedTask;
        }
    }
}