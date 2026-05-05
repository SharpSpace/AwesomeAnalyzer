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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumerateQueryableMultipleTimesCodeFixProvider))]
    [Shared]
    public sealed class EnumerateQueryableMultipleTimesCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0011EnumerateQueryableMultipleTimes.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var variable = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent?.AncestorsAndSelf()
                    .OfType<VariableDeclaratorSyntax>()
                    .FirstOrDefault();

                if (variable?.Initializer == null) continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Convert to list",
                        createChangedDocument: token => ConvertToListAsync(context.Document, variable, token),
                        equivalenceKey: "EnumerateQueryableMultipleTimesCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private static async Task<Document> ConvertToListAsync(
            Document document,
            VariableDeclaratorSyntax variable,
            CancellationToken cancellationToken)
        {
            if (variable.Initializer == null) return document;

            var declaration = variable.Parent as VariableDeclarationSyntax;
            if (declaration == null) return document;

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var initializerSpan = variable.Initializer.Value.Span;
            var initializerText = sourceText.ToString(initializerSpan);

            var changes = new System.Collections.Generic.List<Microsoft.CodeAnalysis.Text.TextChange>();

            // Replace type with var (type comes before the initializer in source)
            if (!declaration.Type.IsVar)
            {
                changes.Add(new Microsoft.CodeAnalysis.Text.TextChange(declaration.Type.Span, "var"));
            }

            // Append .ToList() to the initializer expression
            changes.Add(new Microsoft.CodeAnalysis.Text.TextChange(
                new Microsoft.CodeAnalysis.Text.TextSpan(initializerSpan.End, 0),
                ".ToList()"
            ));

            return document.WithText(sourceText.WithChanges(changes));
        }
    }
}
