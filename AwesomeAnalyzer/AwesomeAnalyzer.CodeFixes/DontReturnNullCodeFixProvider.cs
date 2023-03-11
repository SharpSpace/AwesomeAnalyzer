using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class DontReturnNullCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.Rule0007DontReturnNull.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = (ReturnStatementSyntax)root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;
                if (declaration?.Expression == null) continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Don't return null",
                        token => DoCodeFix(context.Document, declaration, token),
                        equivalenceKey: "DontReturnNullCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Document> DoCodeFix(
            Document document,
            ReturnStatementSyntax declaration,
            CancellationToken token)
        {
            var methodDeclarationSyntax = declaration.HasParent<MethodDeclarationSyntax>();
            string type = null;
            string genericType = null;
            switch (methodDeclarationSyntax.ReturnType)
            {
                case GenericNameSyntax genericNameSyntax:
                {
                    genericType = genericNameSyntax.Identifier.ToFullString();

                    if (genericNameSyntax.TypeArgumentList.Arguments[0] is PredefinedTypeSyntax predefinedTypeSyntax)
                    {
                        type = predefinedTypeSyntax.Keyword.ValueText;
                    }

                    break;
                }

                case ArrayTypeSyntax arrayTypeSyntax:
                    genericType = "Array";
                    type = (arrayTypeSyntax.ElementType as PredefinedTypeSyntax).Keyword.ValueText;
                    break;

                case IdentifierNameSyntax identifierNameSyntax:
                    genericType = identifierNameSyntax.ToFullString().Trim();
                    break;
            }

            var sourceText = await document.GetTextAsync(token).ConfigureAwait(false);
            var targetTextSpan = declaration.Expression.FullSpan;
            switch (genericType)
            {
                case "Array":
                    return document.WithText(sourceText.Replace(targetTextSpan, $"Array.Empty<{type}>()"));

                case "ArrayList":
                    return document.WithText(sourceText.Replace(targetTextSpan, "new ArrayList()"));

                case "IEnumerable":
                    return document.WithText(sourceText.Replace(targetTextSpan, $"Enumerable.Empty<{type}>()"));

                case "IList":
                    return document.WithText(sourceText.Replace(targetTextSpan, $"new List<{type}>()"));

                default:
                    return document.WithText(sourceText.Replace(targetTextSpan, $"new {genericType}<{type}>()"));
            }
        }

        private IList<string> Test()
        {
            return new List<string>();
        }
    }
}