using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedCodeFixProvider))]
    [Shared]
    public sealed class UnusedCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.Rule0012UnusedCode.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.First();
            var node = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;
            if (node == null)
            {
                return;
            }

            var declaration = node.AncestorsAndSelf().FirstOrDefault(IsSupportedDeclarationNode);
            if (declaration == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Remove unused code",
                    createChangedDocument: c => RemoveUnusedCodeAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)
                ),
                diagnostic
            );
        }

        private static bool IsSupportedDeclarationNode(SyntaxNode node) =>
            node is VariableDeclaratorSyntax
                || node is MethodDeclarationSyntax
                || node is PropertyDeclarationSyntax
                || node is EventDeclarationSyntax
                || node is EventFieldDeclarationSyntax
                || node is LocalFunctionStatementSyntax
                || node is ClassDeclarationSyntax
                || node is StructDeclarationSyntax
                || node is RecordDeclarationSyntax;

        private static async Task<Document> RemoveUnusedCodeAsync(
            Document document,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var nodeToRemove = GetNodeToRemove(declaration);
            if (nodeToRemove == null)
            {
                return document;
            }

            var newRoot = root.RemoveNode(nodeToRemove, SyntaxRemoveOptions.KeepExteriorTrivia);
            if (newRoot == null)
            {
                return document;
            }

            return document.WithSyntaxRoot(newRoot.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private static SyntaxNode GetNodeToRemove(SyntaxNode declaration)
        {
            var variableDeclarator = declaration as VariableDeclaratorSyntax;
            if (variableDeclarator == null)
            {
                return declaration;
            }

            var variableDeclaration = variableDeclarator.Parent as VariableDeclarationSyntax;
            if (variableDeclaration == null)
            {
                return declaration;
            }

            if (variableDeclaration.Variables.Count > 1)
            {
                return variableDeclarator;
            }

            return variableDeclaration.Parent;
        }
    }
}
