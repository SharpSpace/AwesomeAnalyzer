using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class DisposedCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.DisposedRule0004.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // context.Document.Project.CompilationOptions
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var languageVersion =
                ((CSharpParseOptions)(await context.Document.GetSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false))?.Options)
                ?.LanguageVersion ??
                LanguageVersion.CSharp6;

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent
                    ?.AncestorsAndSelf()
                    .OfType<LocalDeclarationStatementSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add using to statement",
                        token => AddUsingAsync(context.Document, declaration, languageVersion, token),
                        equivalenceKey: "DisposedCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Document> AddUsingAsync(
            Document document,
            LocalDeclarationStatementSyntax declaration,
            LanguageVersion languageVersion,
            CancellationToken token
        )
        {
            if (declaration.UsingKeyword.ValueText == "using") return document;

            if (languageVersion <= LanguageVersion.CSharp8)
            {
                var children = declaration.Parent?.ChildNodes()
                    .Where(x => x.FullSpan.Start >= declaration.FullSpan.End)
                    .ToList() ?? new List<SyntaxNode>();

                var oldSource = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false))?.ToFullString();
                if (oldSource == null) return document;

                var newSource = oldSource;
                var blockCode = string.Empty;

                if (children.Any())
                {
                    var startIndex = children.Min(x => x.SpanStart);
                    var endIndex = children.Max(x => x.FullSpan.End);
                    blockCode = oldSource.Substring(startIndex, endIndex - startIndex);
                    newSource = oldSource.Remove(startIndex, endIndex - startIndex);
                }

                newSource = newSource.Remove(declaration.FullSpan.End - 3, 3);
                newSource = newSource.Insert(
                    declaration.FullSpan.End - 3,
                    $@")
        {{
            {blockCode}}}"
                );

                newSource = newSource.Insert(declaration.SpanStart, "using (");

                return await Formatter.FormatAsync(
                    document.WithText(SourceText.From(newSource)),
                    cancellationToken: token
                ).ConfigureAwait(false);
            }
            else
            {
                var oldSource = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false))?.ToFullString();
                if (oldSource == null) return document;
                var newSource = oldSource.Insert(declaration.SpanStart, "using ");

                return document.WithText(SourceText.From(newSource));
            }
        }
    }
}