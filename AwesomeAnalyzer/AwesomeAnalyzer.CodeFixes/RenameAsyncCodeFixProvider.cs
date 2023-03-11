using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class RenameAsyncCodeFixProvider : CodeFixProvider
    {
        private const string TextAsync = "Async";

        private static readonly SymbolRenameOptions SymbolRenameOptions = new SymbolRenameOptions();

        public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.Rule0100RenameAsync.Id);

        public override FixAllProvider GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent
                ?.AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Remove Async in method name",
                        token => MakeAsyncAsync(context.Document, declaration, token),
                        equivalenceKey: "MakeAsyncCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private static async Task<Solution> MakeAsyncAsync(
            Document document,
            MethodDeclarationSyntax localDeclaration,
            CancellationToken token
        )
        {
            var semanticModel = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
            var symbol = semanticModel?.GetDeclaredSymbol(localDeclaration, token);
            if (symbol == null) return document.Project.Solution;

            return await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                symbol,
                SymbolRenameOptions,
                localDeclaration.Identifier.ValueText.Replace(TextAsync, string.Empty),
                token
            )
            .ConfigureAwait(false);
        }
    }
}