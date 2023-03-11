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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class AddAsyncPrefixCodeFixProvider : CodeFixProvider
    {
        private const string TextAsync = "Async";

        private static readonly SymbolRenameOptions _symbolRenameOptions = new SymbolRenameOptions();

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0102AddAsync.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.FirstOrDefault();
            var declaration = (IdentifierNameSyntax)root
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent;

            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Add Async Prefix",
                    token => AddAsyncPrefixAsync(document, declaration, semanticModel, token),
                    equivalenceKey: "AddAsyncCodeFixTitle"
                ),
                context.Diagnostics
            );
        }

        private static Task<Solution> AddAsyncPrefixAsync(
            TextDocument document,
            SimpleNameSyntax declaration,
            SemanticModel semanticModel,
            CancellationToken token
        )
        {
            var symbolInfo = semanticModel.GetSymbolInfo(declaration, token);

            if (symbolInfo.Symbol == null)
            {
                return Task.FromResult(document.Project.Solution);
            }

            return Renamer.RenameSymbolAsync(
                document.Project.Solution,
                symbolInfo.Symbol,
                _symbolRenameOptions,
                $"{declaration.Identifier.ValueText}{TextAsync}",
                token
            );
        }
    }
}