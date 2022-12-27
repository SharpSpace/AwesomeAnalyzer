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

namespace AwesomeAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
public sealed class ParseCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.ParseIntRule2001.Id);

    public override FixAllProvider GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var oldSource = (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false))?.ToFullString();

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
                        _ => ParseAsync(context.Document, declaration, oldSource),
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
                        token => ParseAsync(context.Document, declaration, oldSource),
                        equivalenceKey: "ParseCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }
    }

    private Task<Document> ParseAsync(
        Document document,
        EqualsValueClauseSyntax localDeclaration,
        string oldSource
    )
    {
        if (localDeclaration.Parent.Parent is not VariableDeclarationSyntax variableDeclarationSyntax) return Task.FromResult(document);
        return ParseAsync(document, oldSource, variableDeclarationSyntax.Type, localDeclaration.Value.Span, localDeclaration.Value);
    }

    private Task<Document> ParseAsync(
        Document document,
        ReturnStatementSyntax localDeclaration,
        string oldSource
    )
    {
        if (localDeclaration.Parent?.Parent is not MethodDeclarationSyntax methodDeclarationSyntax) return Task.FromResult(document);
        return ParseAsync(
            document,
            oldSource,
            methodDeclarationSyntax.ReturnType,
            localDeclaration.Expression.Span,
            localDeclaration.Expression
        );
    }

    private static Task<Document> ParseAsync(
        Document document,
        string oldSource,
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
            if (nullableTypeSyntax.ElementType is not PredefinedTypeSyntax predefinedTypeSyntax) return Task.FromResult(document);
            if (ParseAnalyzer.Types.Any(x => x.TypeName == predefinedTypeSyntax.Keyword.ValueText) == false) return Task.FromResult(document);
            type = predefinedTypeSyntax.Keyword.ValueText;
        }
        else
        {
            if (typeSyntax is not PredefinedTypeSyntax predefinedTypeSyntax) return Task.FromResult(document);
            if (ParseAnalyzer.Types.Any(x => x.TypeName == predefinedTypeSyntax.Keyword.ValueText) == false) return Task.FromResult(document);
            type = predefinedTypeSyntax.Keyword.ValueText;
        }

        var newSource = new StringBuilder(
            oldSource[..span.Start]
        );

        var itemType = ParseAnalyzer.Types.Single(x => x.TypeName == type);

        newSource.Append(type);
        newSource.Append($".TryParse({expressionSyntax}, out var value) ? value : ");
        if (isNullable == false)
        {
            newSource.Append(itemType.Cast);
        }
        newSource.Append(isNullable ? "null" : itemType.DefaultValueString);

        newSource.Append(
            oldSource.Substring(
                span.End
            )
        );

        return Task.FromResult(
            document.WithText(
                SourceText.From(
                    newSource.ToString()
                )
            )
        );
    }
}