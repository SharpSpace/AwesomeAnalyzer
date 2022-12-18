using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        var isNullable = false;
        if (variableDeclarationSyntax.Type is NullableTypeSyntax nullableTypeSyntax)
        {
            isNullable = true;
            if (nullableTypeSyntax.ElementType is not PredefinedTypeSyntax predefinedTypeSyntax) return Task.FromResult(document);
            if (predefinedTypeSyntax.Keyword.ValueText != "int") return Task.FromResult(document);
        }
        else
        {
            if (variableDeclarationSyntax.Type is not PredefinedTypeSyntax predefinedTypeSyntax) return Task.FromResult(document);
            if (predefinedTypeSyntax.Keyword.ValueText != "int") return Task.FromResult(document);
        }

        var newSource = new StringBuilder(
            oldSource.Substring(
                0,
                localDeclaration.Value.SpanStart
            )
        );

        newSource.Append($"int.TryParse({localDeclaration.Value}, out var value) ? value : ");
        newSource.Append(isNullable ? "null" : "0");

        newSource.Append(
            oldSource.Substring(
                localDeclaration.Value.Span.End
            )
        );

        return Task.FromResult(document.WithText(
            SourceText.From(
                newSource.ToString()
            )
        ));
    }

    private Task<Document> ParseAsync(
        Document document,
        ReturnStatementSyntax localDeclaration,
        string oldSource
    )
    {
        if (localDeclaration.Parent?.Parent is not MethodDeclarationSyntax methodDeclarationSyntax) return Task.FromResult(document);
        var isNullable = false;
        if (methodDeclarationSyntax.ReturnType is NullableTypeSyntax nullableTypeSyntax)
        {
            isNullable = true;
            if (nullableTypeSyntax.ElementType is not PredefinedTypeSyntax predefinedTypeSyntax) return Task.FromResult(document);
            if (predefinedTypeSyntax.Keyword.ValueText != "int") return Task.FromResult(document);
        }
        else
        {
            if (methodDeclarationSyntax.ReturnType is not PredefinedTypeSyntax predefinedTypeSyntax) return Task.FromResult(document);
            if (predefinedTypeSyntax.Keyword.ValueText != "int") return Task.FromResult(document);
        }

        var newSource = new StringBuilder(
            oldSource[..localDeclaration.Expression.SpanStart]
        );

        newSource.Append($"int.TryParse({localDeclaration.Expression}, out var value) ? value : ");
        newSource.Append(isNullable ? "null" : "0");

        newSource.Append(
            oldSource[localDeclaration.Expression.Span.End..]
        );

        return Task.FromResult(document.WithText(SourceText.From(newSource.ToString())));
    }
}