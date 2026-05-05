using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumerateMultipleTimesCodeFixProvider))]
    [Shared]
    public sealed class EnumerateMultipleTimesCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0010EnumerateMultipleTimes.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent?.AncestorsAndSelf()
                    .OfType<LocalDeclarationStatementSyntax>()
                    .FirstOrDefault();

                if (declaration == null) continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Convert to list",
                        createChangedDocument: token => ConvertToListAsync(context.Document, declaration, token),
                        equivalenceKey: "EnumerateMultipleTimesCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private static async Task<Document> ConvertToListAsync(
            Document document,
            LocalDeclarationStatementSyntax localDeclaration,
            CancellationToken cancellationToken)
        {
            var declaration = localDeclaration.Declaration;
            var variable = declaration.Variables[0];
            if (variable.Initializer == null) return document;

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var initializerSpan = variable.Initializer.Value.Span;
            var initializerText = sourceText.ToString(initializerSpan);
            return document.WithText(sourceText.Replace(initializerSpan, initializerText + ".ToList()"));
        }
    }
}
