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

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var variable = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent?.AncestorsAndSelf()
                    .OfType<VariableDeclaratorSyntax>()
                    .FirstOrDefault();

                if (variable?.Initializer == null) continue;

                var method = MaterializationHelper.ChooseBestMethod(variable, semanticModel, context.CancellationToken);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: MaterializationHelper.GetFixTitle(method),
                        createChangedDocument: token => MaterializeAsync(context.Document, variable, method, token),
                        equivalenceKey: "EnumerateQueryableMultipleTimesCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private static async Task<Document> MaterializeAsync(
            Document document,
            VariableDeclaratorSyntax variable,
            MaterializationHelper.Method method,
            CancellationToken cancellationToken)
        {
            if (variable.Initializer == null) return document;

            var declaration = variable.Parent as VariableDeclarationSyntax;
            if (declaration == null) return document;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            var invocation = MaterializationHelper.BuildMaterializationInvocation(
                variable.Initializer.Value,
                MaterializationHelper.GetMethodName(method)
            );

            // Also change the declared type to var: List<T>/T[]/HashSet<T> don't implement IQueryable<T>.
            var varType = SyntaxFactory.IdentifierName("var")
                .WithTriviaFrom(declaration.Type);

            var newRoot = root.ReplaceNodes(
                new SyntaxNode[] { declaration.Type, variable.Initializer.Value },
                (original, _) =>
                {
                    if (original == declaration.Type) return varType;
                    return invocation;
                }
            );

            var newDocument = document.WithSyntaxRoot(newRoot);

            return await MaterializationHelper.EnsureUsingSystemLinqAsync(newDocument, cancellationToken).ConfigureAwait(false);
        }
    }
}
