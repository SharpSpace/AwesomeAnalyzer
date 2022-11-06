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
using Document = Microsoft.CodeAnalysis.Document;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class MakeAsyncCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.MakeAsyncRule0002.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent
                    .AncestorsAndSelf()
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

        private async Task<Solution> MakeAsyncAsync(
            Document document,
            MethodDeclarationSyntax localDeclaration,
            CancellationToken token
        )
        {
            var semanticModel = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(localDeclaration, token);

            return await Renamer.RenameSymbolAsync(
                document.Project.Solution,
                symbol,
                new SymbolRenameOptions(),
                localDeclaration.Identifier.ValueText.Replace("Async", string.Empty),
                token
            ).ConfigureAwait(false);
        }
    }
}