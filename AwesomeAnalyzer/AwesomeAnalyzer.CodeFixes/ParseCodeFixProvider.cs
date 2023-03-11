using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeAnalyzer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class ParseCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.Rule0005ParseString.Id);

        public override FixAllProvider GetFixAllProvider() => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var oldSource = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var parent = root
                .FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent
                ?.AncestorsAndSelf()
                .ToList();
                if (parent == null) continue;

                foreach (var declaration in parent.OfType<EqualsValueClauseSyntax>())
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add Parse",
                            _ => Task.FromResult(Parse(context.Document, declaration, oldSource)),
                            equivalenceKey: "ParseCodeFixTitle"
                        ),
                        diagnostic
                    );
                }

                foreach (var declaration in parent.OfType<ReturnStatementSyntax>())
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add Parse",
                            _ => Task.FromResult(Parse(context.Document, declaration, oldSource)),
                            equivalenceKey: "ParseCodeFixTitle"
                        ),
                        diagnostic
                    );
                }
            }
        }

        private static Document Parse(
            Document document,
            EqualsValueClauseSyntax localDeclaration,
            SyntaxNode oldSource
        )
        {
            return localDeclaration.Parent?.Parent is VariableDeclarationSyntax variableDeclarationSyntax
            ? Parse(
                document,
                oldSource,
                variableDeclarationSyntax.Type,
                localDeclaration.Value.Span,
                localDeclaration.Value
            )
            : document;
        }

        private static Document Parse(
            Document document,
            ReturnStatementSyntax localDeclaration,
            SyntaxNode oldSource
        )
        {
            return localDeclaration.Parent?.Parent is MethodDeclarationSyntax methodDeclarationSyntax
            ? Parse(
                document,
                oldSource,
                methodDeclarationSyntax.ReturnType,
                localDeclaration.Expression.Span,
                localDeclaration.Expression
            )
            : document;
        }

        private static Document Parse(
            Document document,
            SyntaxNode oldSource,
            TypeSyntax typeSyntax,
            TextSpan span,
            ExpressionSyntax expressionSyntax
        )
        {
            var isNullable = false;
            string type;
            if (typeSyntax is NullableTypeSyntax nullableTypeSyntax)
            {
                isNullable = true;
                if (!(nullableTypeSyntax.ElementType is PredefinedTypeSyntax predefinedTypeSyntax)) return document;
                if (ParseAnalyzer.Types.Any(x => x.TypeName == predefinedTypeSyntax.Keyword.ValueText) == false)
                    return document;
                type = predefinedTypeSyntax.Keyword.ValueText;
            }
            else
            {
                if (!(typeSyntax is PredefinedTypeSyntax predefinedTypeSyntax)) return document;
                if (ParseAnalyzer.Types.Any(x => x.TypeName == predefinedTypeSyntax.Keyword.ValueText) == false)
                    return document;
                type = predefinedTypeSyntax.Keyword.ValueText;
            }

            var code = new StringBuilder(type);
            code.Append($".TryParse({expressionSyntax}, out var value) ? value : ");

            var itemType = ParseAnalyzer.Types.Single(x => x.TypeName == type);

            if (isNullable == false)
            {
                code.Append(itemType.Cast);
            }

            code.Append(isNullable ? "null" : itemType.DefaultValueString);

            return document.WithText(
                oldSource.GetText().Replace(span.Start, span.Length, code.ToString())
            );
        }
    }
}