using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class MakeSealedCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.MakeSealedRule0001.Id);

        public override FixAllProvider GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.MakeSealedCodeFixTitle,
                    createChangedDocument: c => MakeConstAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.MakeSealedCodeFixTitle)),
                diagnostic);
        }

        private static async Task<Document> MakeConstAsync(
            Document document,
            ClassDeclarationSyntax localDeclaration,
            CancellationToken cancellationToken
        )
        {
            var oldClassSource = localDeclaration.GetText().ToString();
            var newClassSource = oldClassSource.Replace("class", "sealed class");

            var oldSource = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).ToFullString();
            var newSource = $"{oldSource.Substring(0, localDeclaration.FullSpan.Start)}{newClassSource}{oldSource.Substring(localDeclaration.FullSpan.End)}";

            return document.WithText(SourceText.From(newSource));
        }
    }
}