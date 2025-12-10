using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAnalyzer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class SortAndOrderCodeFixProvider : CodeFixProvider
    {
        private static readonly char[] NewLine = new[] { '\r', '\n' };

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.Rule1008EnumSort.Id,
            DiagnosticDescriptors.Rule1009EnumOrder.Id,
            DiagnosticDescriptors.Rule1001FieldSort.Id,
            DiagnosticDescriptors.Rule1002FieldOrder.Id,
            DiagnosticDescriptors.Rule1005ConstructorOrder.Id,
            DiagnosticDescriptors.Rule1010DelegateSort.Id,
            DiagnosticDescriptors.Rule1011DelegateOrder.Id,
            DiagnosticDescriptors.Rule1012EventSort.Id,
            DiagnosticDescriptors.Rule1009EnumOrder.Id,
            DiagnosticDescriptors.Rule1006PropertySort.Id,
            DiagnosticDescriptors.Rule1007PropertyOrder.Id,
            DiagnosticDescriptors.Rule1003MethodSort.Id,
            DiagnosticDescriptors.Rule1004MethodOrder.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var oldSource = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))
            ?.GetText();

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

        private static async Task<Document> SortDocumentAsync(
            Document document,
            SyntaxNode root,
            SourceText oldSource,
            CancellationToken token
        )
        {
            var sortVirtualizationVisitor = new SortVirtualizationVisitor(token);
            sortVirtualizationVisitor.Visit(root);

            var classList = sortVirtualizationVisitor.Members
            .SelectMany(
                x => x.Value.Where(y => string.IsNullOrWhiteSpace(y.ClassName) == false),
                (x, item) =>
                (
                    item.ClassName,
                    item.FullSpan,
                    item.Name,
                    item.Order,
                    item.RegionName,
                    Type: x.Key
                )
            )
            .GroupBy(x => new { x.ClassName, x.RegionName })
            .Where(x => x.Count() > 1)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(y => y.FullSpan.Start).ToList()
            )
            .Reverse();

            var newSource = oldSource;
            var stringBuilder = new StringBuilder();
            foreach (var item in classList)
            {
                token.ThrowIfCancellationRequested();

                stringBuilder.Clear();
                foreach (var codeItem in item.Value.OrderBy(x => x.Order).ThenBy(x => SortAnalyzer.PadNumbers(x.Name)))
                {
                    stringBuilder.AppendLine(
                        oldSource.GetSubText(TextSpan.FromBounds(codeItem.FullSpan.Start, codeItem.FullSpan.End))
                        .ToString()
                        .TrimStart(NewLine)
                    );
                }

                var textSpan = TextSpan.FromBounds(
                    item.Value.Min(y => y.FullSpan.Start),
                    item.Value.Max(y => y.FullSpan.End)
                );

                newSource = newSource.Replace(
                    textSpan,
                    $"{stringBuilder.ToString().TrimEnd(NewLine)}{Environment.NewLine}"
                );
            }

            return document.WithText(newSource);

            // return Formatter.FormatAsync(document.WithText(newSource), cancellationToken: token);
        }
    }
}