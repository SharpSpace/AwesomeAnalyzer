using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class SortAndOrderCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.EnumSortRule1008.Id,
            DiagnosticDescriptors.EnumOrderRule1009.Id,
            DiagnosticDescriptors.FieldSortRule1001.Id,
            DiagnosticDescriptors.FieldOrderRule1002.Id,
            DiagnosticDescriptors.ConstructorOrderRule1005.Id,
            DiagnosticDescriptors.DelegateSortRule1010.Id,
            DiagnosticDescriptors.DelegateOrderRule1011.Id,
            DiagnosticDescriptors.EventSortRule1012.Id,
            DiagnosticDescriptors.EnumOrderRule1009.Id,
            DiagnosticDescriptors.PropertySortRule1006.Id,
            DiagnosticDescriptors.PropertyOrderRule1007.Id,
            DiagnosticDescriptors.MethodSortRule1003.Id,
            DiagnosticDescriptors.MethodOrderRule1004.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var oldSource = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))?.GetText();

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Sort document",
                        token => SortDocumentAsync(
                            context.Document,
                            root,
                            oldSource,
                            token
                        ),
                        equivalenceKey: "SortCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private static Task<Document> SortDocumentAsync(
            Document document,
            SyntaxNode root,
            SourceText oldSource,
            CancellationToken token
        )
        {
            var sortVirtualizationVisitor = new SortVirtualizationVisitor();
            sortVirtualizationVisitor.Visit(root);

            var classList = sortVirtualizationVisitor.Members
                .SelectMany(
                    x => x.Value,
                    (x, item) => (
                        item.ClassName,
                        item.FullSpan,
                        item.Name,
                        item.Order,
                        Type: x.Key
                    )
                )
                .GroupBy(x => x.ClassName)
                .Where(x => x.Count() > 1)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.FullSpan.Start).ToList()
                );

            var newSource = oldSource;
            var stringBuilder = new StringBuilder();
            foreach (var item in classList)
            {
                token.ThrowIfCancellationRequested();

                stringBuilder.Clear();
                foreach (var codeItem in item.Value.OrderBy(x => x.Order).ThenBy(x => x.Name))
                {
                    stringBuilder.AppendLine(
                        oldSource.GetSubText(TextSpan.FromBounds(codeItem.FullSpan.Start, codeItem.FullSpan.End))
                            .ToString()
                            .TrimStart('\r', '\n')
                    );
                }

                var textSpan = TextSpan.FromBounds(
                    item.Value.Min(y => y.FullSpan.Start),
                    item.Value.Max(y => y.FullSpan.End)
                );

                newSource = newSource.Replace(textSpan, stringBuilder.ToString().TrimEnd('\r', '\n') + Environment.NewLine);
            }

            return Task.FromResult(document.WithText(newSource));

            // return Formatter.FormatAsync(document.WithText(newSource), cancellationToken: token);
        }
    }
}