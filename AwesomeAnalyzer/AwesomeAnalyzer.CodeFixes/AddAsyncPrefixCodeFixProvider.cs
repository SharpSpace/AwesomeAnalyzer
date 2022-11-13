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
using Microsoft.CodeAnalysis.Rename;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class AddAsyncPrefixCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.AddAsyncRule0102.Id
        );

        public override FixAllProvider GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent
                    .AncestorsAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add Async Prefix",
                        token => AddAsyncPrefixAsync(context.Document, declaration, token),
                        equivalenceKey: "AddAsyncCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Solution> AddAsyncPrefixAsync(
            Document document,
            SimpleNameSyntax declaration,
            CancellationToken token
        )
        {
            var semanticModel = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
            if (semanticModel == null) return document.Project.Solution;

            var symbolInfo = semanticModel.GetSymbolInfo(declaration);

            return await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                symbolInfo.Symbol,
                new SymbolRenameOptions(),
                $"{declaration.Identifier.ValueText}Async",
                token
            ).ConfigureAwait(false);
        }
    }
}