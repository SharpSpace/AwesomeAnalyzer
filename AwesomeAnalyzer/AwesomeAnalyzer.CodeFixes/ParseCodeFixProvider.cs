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
using System;

namespace AwesomeAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
public sealed class ParseCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.ParseStringRule0005.Id);

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
        string oldSource
    )
    {
        return localDeclaration.Parent.Parent is VariableDeclarationSyntax variableDeclarationSyntax 
            ? Parse(document, oldSource, variableDeclarationSyntax.Type, localDeclaration.Value.Span, localDeclaration.Value)
            : document;
    }

    private static Document Parse(
        Document document,
        ReturnStatementSyntax localDeclaration,
        string oldSource
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
            if (nullableTypeSyntax.ElementType is not PredefinedTypeSyntax predefinedTypeSyntax) return document;
            if (ParseAnalyzer.Types.Any(x => x.TypeName == predefinedTypeSyntax.Keyword.ValueText) == false) return document;
            type = predefinedTypeSyntax.Keyword.ValueText;
        }
        else
        {
            if (typeSyntax is not PredefinedTypeSyntax predefinedTypeSyntax) return document;
            if (ParseAnalyzer.Types.Any(x => x.TypeName == predefinedTypeSyntax.Keyword.ValueText) == false) return document;
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
            oldSource.AsSpan(
                span.End
            )
        );

        return document.WithText(
            SourceText.From(
                newSource.ToString()
            )
        );
    }
}