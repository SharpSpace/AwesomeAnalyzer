using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class SimilarCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.Rule0008Similar.Id);

        public override async Task RegisterCodeFixesAsync(
            CodeFixContext context
        )
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Move all similar code to new method.",
                        createChangedDocument: token => CodeFixAsync(root, context, diagnostic),
                        equivalenceKey: nameof(SimilarCodeFixProvider)
                    ),
                    context.Diagnostics
                );
            }
        }

        private static async Task<Document> CodeFixAsync(
            SyntaxNode root,
            CodeFixContext context,
            Diagnostic diagnostic
        )
        {
            var semanticModel = await context.Document.GetSemanticModelAsync();
            if (semanticModel == null) return context.Document;

            var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent
                ?.AncestorsAndSelf()
                ?.OfType<SyntaxNode>()
                .FirstOrDefault();

            if (declaration == null) return context.Document;

            var variables = GetVariablesThatIntersects(declaration, semanticModel);

            var parentClass = declaration.HasParent<ClassDeclarationSyntax>();
            var sourceBlock = GetParentMethod(declaration);

            var codeIndent = declaration.GetLeadingTrivia();
            var methodName = SyntaxFactory.Identifier("NewMethod");

            var newClass = UpdateMethod(
                sourceBlock,
                parentClass,
                methodName,
                variables,
                declaration,
                codeIndent
            );

            var sourceText = await UpdateSourceTextAsync(
                    context,
                    diagnostic,
                    methodName.ValueText,
                    variables,
                    parentClass,
                    newClass?.ToFullString(),
                    declaration,
                    codeIndent.ToFullString()
                )
                .ConfigureAwait(false);
            return context.Document.WithText(sourceText);
        }

        private static (SyntaxTokenList Modifiers, SyntaxTriviaList LeadingTrivia) GetParentMethod(SyntaxNode declaration)
        {
            var d = declaration.HasParent<MethodDeclarationSyntax>();
            if (d != null)
            {
                return (
                    Modifiers: d.Modifiers, 
                    LeadingTrivia: d.GetLeadingTrivia()
                );
            }
            else
            {
                var constructorDeclarationSyntax = declaration.HasParent<ConstructorDeclarationSyntax>();
                return (
                    Modifiers: constructorDeclarationSyntax.Modifiers,
                    LeadingTrivia: constructorDeclarationSyntax.GetLeadingTrivia()
                );
            }
        }

        private static async Task<SourceText> UpdateSourceTextAsync(
            CodeFixContext context,
            Diagnostic diagnostic,
            string methodName,
            List<ISymbol> variables,
            ClassDeclarationSyntax parentClass,
            string newCode,
            SyntaxNode declaration,
            string codeIndent
        )
        {
            var sourceText = await context.Document.GetTextAsync().ConfigureAwait(false);

            var stringLiteralExpressionSyntax = GetChildLiteralExpressionSyntax(declaration.Parent).Select(x => x.SyntaxNode.ToFullString());

            sourceText = diagnostic.AdditionalLocations
                .OrderByDescending(x => x.SourceSpan.Start)
                .Aggregate(
                    sourceText.Replace(
                        parentClass.FullSpan,
                        newCode ?? string.Empty
                    ),
                    (
                        current,
                        location
                    ) =>
                    {
                        var spanStart = location.SourceSpan.Start - codeIndent.Length;
                        var spanLength = location.SourceSpan.Length + codeIndent.Length;

                        Debug.WriteLine("sourceText out: '" + current.GetSubText(new TextSpan(spanStart, spanLength)) + "'");

                        var text = current.Replace(
                            spanStart, 
                            spanLength, 
                            GetMethodCallString(
                                stringLiteralExpressionSyntax
                                    .Skip(diagnostic.AdditionalLocations.ToList().IndexOf(location) + 1)
                                    .Take(1)
                            )
                        );
                        //Debug.WriteLine("sourceText in:" + text);
                        return text;
                    }
                );

            Debug.WriteLine("Final sourceText out: '" + sourceText.GetSubText(new TextSpan(declaration.FullSpan.Start, declaration.FullSpan.Length - 2)) + "'");
            sourceText = sourceText
                .Replace(
                    declaration.FullSpan.Start, 
                    declaration.FullSpan.Length - 2, 
                    GetMethodCallString(stringLiteralExpressionSyntax.Take(1))
                );

            //Debug.WriteLine("sourceText:" + sourceText);

            return sourceText;

            string GetMethodCallString(IEnumerable<string> stringParameters)
            {
                var stringBuilder = new StringBuilder(codeIndent);
                stringBuilder.Append(methodName);
                stringBuilder.Append('(');
                stringBuilder.Append(string.Join(", ", variables.Select(x => x.Name).Union(stringParameters)));
                stringBuilder.Append(");");

                Debug.WriteLine("stringBuilder: '" + stringBuilder.ToString() + "'");

                return stringBuilder.ToString();
            }
        }

        private static ClassDeclarationSyntax UpdateMethod(
            (SyntaxTokenList Modifiers, SyntaxTriviaList LeadingTrivia) sourceBlock,
            ClassDeclarationSyntax parentClass,
            SyntaxToken methodName,
            List<ISymbol> variables,
            SyntaxNode declaration,
            SyntaxTriviaList codeIndent
        )
        {
            var methodIndent = sourceBlock.LeadingTrivia;
            return parentClass.AddMembers(
                SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.VoidKeyword)
                                .WithTrailingTrivia(SyntaxFactory.Space)
                        ),
                        methodName
                    )
                    .WithModifiers(GetMethodModifiers(sourceBlock))
                    .WithParameterList(GetMethodParameters(declaration, variables))
                    .WithBody(GetMethodBody(declaration, codeIndent, methodIndent))
                    .WithLeadingTrivia(methodIndent.Insert(0, SyntaxFactory.CarriageReturnLineFeed))
            );
        }

        private static SyntaxTokenList GetMethodModifiers(
            (SyntaxTokenList Modifiers, SyntaxTriviaList LeadingTrivia) sourceBlock
        ) => sourceBlock.Modifiers.Any()
            ? sourceBlock.Modifiers
            : SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxFactory.Space)
            );

        private static ParameterListSyntax GetMethodParameters(SyntaxNode declaration, List<ISymbol> variables)
        {
            var parameterSyntaxes = variables.Select(x =>
                SyntaxFactory
                    .Parameter(SyntaxFactory.Identifier(x.Name))
                    .WithType(
                        SyntaxFactory
                            .ParseTypeName(
                                ((x as ILocalSymbol)?.Type ?? (x as IParameterSymbol)?.Type)
                                .ToDisplayString(
                                    SymbolDisplayFormat.MinimallyQualifiedFormat
                                )
                            )
                            .WithTrailingTrivia(SyntaxFactory.Space)
                    )
            ).ToList();
            parameterSyntaxes.AddRange(
                GetChildLiteralExpressionSyntax(declaration)
                    .Select(x =>
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.Name))
                            .WithType(SyntaxFactory
                                .ParseTypeName(x.Type)
                                .WithTrailingTrivia(SyntaxFactory.Space)
                            ).WithLeadingTrivia(SyntaxFactory.Space)
                    )
            );

            return SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    parameterSyntaxes
                )
            );
        }

        private static BlockSyntax GetMethodBody(SyntaxNode declaration, SyntaxTriviaList codeIndent, SyntaxTriviaList methodIndent) => 
            SyntaxFactory.Block(SyntaxFactory
                .ParseStatement(FixBody(declaration))
                .WithLeadingTrivia(codeIndent.Insert(0, SyntaxFactory.CarriageReturnLineFeed))
            )
            .WithCloseBraceToken(
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(methodIndent)
            )
            .WithLeadingTrivia(
                methodIndent.Insert(0, SyntaxFactory.CarriageReturnLineFeed)
            )
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        private static string FixBody(SyntaxNode declaration)
        {
            var body = declaration.GetText();
            //Debug.WriteLine("body:" + body);

            var decendants = GetChildLiteralExpressionSyntax(declaration).ToImmutableList();
            if (decendants.Count > 0)
            {
                //Debug.WriteLine("Match:" + node1Decendants.Count);

                foreach (var tuple in decendants)
                {
                    //Debug.WriteLine("node1Code:" + node1.FullSpan + " " + node1Code.Length + " Textspan:" + (textSpan.Start - node1.FullSpan.Start) + " - " + textSpan.Length);
                    body = body.Replace(tuple.SyntaxNode.Span.Start - declaration.FullSpan.Start, tuple.SyntaxNode.Span.Length, tuple.Name);
                }
            }

            //Debug.WriteLine("body:" + body);

            return body.ToString();
        }

        private static List<ISymbol> GetVariablesThatIntersects(
            SyntaxNode declaration,
            SemanticModel semanticModel
        )
        {
            var declaredSymbols = GetChildIdentifierNameSyntax(declaration, semanticModel);

            var outerSymbols = GetOuterSymbols(semanticModel, declaration);

            return declaredSymbols
                .Intersect(outerSymbols)
                .ToList();
        }

        private static IEnumerable<ISymbol> GetOuterSymbols(
            SemanticModel semanticModel,
            SyntaxNode declaration
        ) =>
            semanticModel.AnalyzeDataFlow(declaration)
                .ReadOutside
                .Where(
                    symbol =>
                        symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Parameter
                );

        private static IEnumerable<ISymbol> GetChildIdentifierNameSyntax(
            SyntaxNode declaration,
            SemanticModel semanticModel
        ) =>
            declaration.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Select(x => semanticModel.GetSymbolInfo(x).Symbol)
                .Where(x =>
                    x != null &&
                    (
                        x.Kind == SymbolKind.Local || x.Kind == SymbolKind.Parameter
                    )
                )
                .Distinct();

        private static IEnumerable<(string Name, string Type, LiteralExpressionSyntax SyntaxNode)> GetChildLiteralExpressionSyntax(
            SyntaxNode declaration
        )
        {
            var literalExpressionSyntaxes = declaration.DescendantNodesAndSelf()
                .OfType<LiteralExpressionSyntax>()
                .Where(x => x.IsKind(SyntaxKind.StringLiteralExpression))
                .Select((x, i) => (
                    Name: $"s{i}", 
                    Type: "string",
                    SyntaxNode: x
                ));

            Debug.WriteLine("literalExpressionSyntax " + string.Join(" | ", literalExpressionSyntaxes.Select(x => x.ToString())));

            return literalExpressionSyntaxes;

            //var symbols = literalExpressionSyntaxes
            //    .Select(x => semanticModel.GetSymbolInfo(x));

            
            //Debug.WriteLine("symbols " + string.Join(" | ", symbols.Select(x => x.ToString())));

            //return symbols
            //    .Select(x => x.Symbol)
            //    .Where(x => x != null)
            //    .Distinct();
        }
    }
}