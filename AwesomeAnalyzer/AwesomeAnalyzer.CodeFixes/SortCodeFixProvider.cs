using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class SortCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            SortAnalyzer.FieldSortDiagnosticId,
            SortAnalyzer.FieldOrderDiagnosticId,
            SortAnalyzer.MethodSortDiagnosticId,
            SortAnalyzer.MethodOrderDiagnosticId,
            SortAnalyzer.ConstructorOrderDiagnosticId
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var oldSource = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))?.ToFullString();

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Sort",
                        token => Task.FromResult(this.MakeAsyncPrefix(context.Document, root, token, oldSource)),
                        equivalenceKey: "SortCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private Document MakeAsyncPrefix(
            Document document,
            SyntaxNode root,
            CancellationToken token,
            string oldSource
        )
        {
            var sortVirtualizationVisitor = new SortVirtualizationVisitor();
            sortVirtualizationVisitor.Visit(root);

            var oldCode = (
                from keyValuePair in sortVirtualizationVisitor.Members 
                from item in keyValuePair.Value 
                select (
                    Order: keyValuePair.Key, 
                    Code: oldSource.Substring(item.FullSpan.Start, item.FullSpan.Length), 
                    item.FullSpan, 
                    item.Name,
                    item.ModifiersOrder
                )
            ).ToList();

            var minStartIndex = int.MaxValue;
            var newSource = oldSource;
            foreach (var item in
                     oldCode.OrderByDescending(x => x.FullSpan.Start))
            {
                newSource = newSource.Remove(item.FullSpan.Start, item.FullSpan.Length);

                if (item.FullSpan.Start < minStartIndex)
                {
                    minStartIndex = item.FullSpan.Start;
                }
            }

            newSource = oldCode
                .OrderByDescending(x => x.Order)
                .ThenByDescending(x => x.ModifiersOrder)
                .ThenByDescending(x => x.Name)
                .Aggregate(newSource, (current, code) => current.Insert(minStartIndex, code.Code));

            return document.WithText(SourceText.From(newSource));
        }
    }
}