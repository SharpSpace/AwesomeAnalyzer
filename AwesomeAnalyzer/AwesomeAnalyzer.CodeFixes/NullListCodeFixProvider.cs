using System.Collections.Generic;
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
    public sealed class NullListCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.NullListRule0002.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var oldSource = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))?.ToFullString();

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent
                    .AncestorsAndSelf()
                    .OfType<LiteralExpressionSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Remove Async in method name",
                        token => NullListFix(context.Document, declaration, oldSource, token),
                        equivalenceKey: "MakeAsyncCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private Task<Document> NullListFix(
            Document document,
            LiteralExpressionSyntax localDeclaration,
            string oldSource,
            CancellationToken token
        )
        {
            var newSource = oldSource.Remove(localDeclaration.SpanStart, localDeclaration.Span.Length);
            var methodDeclarationSyntax = localDeclaration.HasParent<MethodDeclarationSyntax>();
            if (methodDeclarationSyntax == null) return Task.FromResult(document);

            string genericType = null;
            string returnType = null;
            if (methodDeclarationSyntax.ReturnType is GenericNameSyntax genericNameSyntax)
            {
                genericType = genericNameSyntax.Identifier.ValueText;

                var predefinedTypeSyntax = genericNameSyntax.TypeArgumentList.Arguments.OfType<PredefinedTypeSyntax>().FirstOrDefault();
                if (predefinedTypeSyntax != null)
                {
                    returnType = predefinedTypeSyntax.Keyword.ValueText;
                }
            }
            else if (methodDeclarationSyntax.ReturnType is ArrayTypeSyntax arrayTypeSyntax)
            {
                genericType = "Array";
                if (arrayTypeSyntax.ElementType is PredefinedTypeSyntax predefinedTypeSyntax)
                {
                    returnType = predefinedTypeSyntax.Keyword.ValueText;
                }
            }

            if (returnType == null) return Task.FromResult(document);

            switch (genericType)
            {
                case "IEnumerable":
                {
                    newSource = newSource.Insert(localDeclaration.SpanStart, $"Enumerable.Empty<{returnType}>()");
                    break;
                }
                case "List":
                {
                    newSource = newSource.Insert(localDeclaration.SpanStart, $"new List<{returnType}>()");
                    break;
                }
                case "Array":
                {
                    newSource = newSource.Insert(localDeclaration.SpanStart, $"Array.Empty<{returnType}>()");
                    break;
                }
                default:
                    break;
            }

            return Task.FromResult(document.WithText(SourceText.From(newSource)));
        }
    }
}