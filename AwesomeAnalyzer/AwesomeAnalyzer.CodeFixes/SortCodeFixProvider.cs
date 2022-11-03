﻿using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class SortCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            SortAnalyzer.FieldSortDiagnosticId,
            SortAnalyzer.FieldOrderDiagnosticId,
            SortAnalyzer.ConstructorOrderDiagnosticId,
            SortAnalyzer.PropertySortDiagnosticId,
            SortAnalyzer.PropertyOrderDiagnosticId,
            SortAnalyzer.MethodSortDiagnosticId,
            SortAnalyzer.MethodOrderDiagnosticId
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
                        "Sort document",
                        token => this.SortDocumentAsync(context.Document, root, token, oldSource),
                        equivalenceKey: "SortCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private Task<Document> SortDocumentAsync(
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
                token.ThrowIfCancellationRequested();
                newSource = newSource.Remove(item.FullSpan.Start, item.FullSpan.Length);

                if (item.FullSpan.Start < minStartIndex)
                {
                    minStartIndex = item.FullSpan.Start;
                }
            }

            var count = 0;
            foreach (var tuple in oldCode
                .OrderByDescending(x => x.Order)
                 .ThenByDescending(x => x.ModifiersOrder)
                 .ThenByDescending(x => x.Name))
            {
                token.ThrowIfCancellationRequested();
                count++;
                var code = count != 1 
                    ? $"    {tuple.Code.Trim()}{Environment.NewLine}{Environment.NewLine}" 
                    : $"    {tuple.Code.Trim()}{Environment.NewLine}";

                newSource = newSource.Insert(minStartIndex, code);
            }

            //return document.WithText(SourceText.From(newSource));
            return Formatter.FormatAsync(document.WithText(SourceText.From(newSource)), cancellationToken: token);
        }
    }
}