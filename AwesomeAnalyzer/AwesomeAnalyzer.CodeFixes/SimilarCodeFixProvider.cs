using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Document = Microsoft.CodeAnalysis.Document;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public class SimilarCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.SimilarRule0008.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Move all similar code to new method.",
                        createChangedDocument: async token => await CodeFix(root, context, diagnostic),
                        equivalenceKey: nameof(SimilarCodeFixProvider)
                    ),
                    context.Diagnostics
                );
            }
        }

        private static async Task<Document> CodeFix(SyntaxNode root, CodeFixContext context, Diagnostic diagnostic)
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
            var parentMethod = declaration.HasParent<MethodDeclarationSyntax>();

            var codeIndent = declaration.GetLeadingTrivia();
            var methodName = SyntaxFactory.Identifier("NewMethod");

            var newClass = UpdateClass(
                parentMethod,
                parentClass,
                methodName,
                variables,
                declaration,
                codeIndent
            );

            var newCode = newClass?.ToFullString();

            var sourceText = await UpdateSourceTextAsync(
                context,
                diagnostic,
                methodName,
                variables,
                parentClass,
                newCode,
                declaration,
                codeIndent
            ).ConfigureAwait(false);
            return context.Document.WithText(sourceText);
        }

        private static async Task<SourceText> UpdateSourceTextAsync(
            CodeFixContext context,
            Diagnostic diagnostic,
            SyntaxToken methodName,
            List<ISymbol> variables,
            ClassDeclarationSyntax parentClass,
            string newCode,
            SyntaxNode declaration,
            SyntaxTriviaList codeIndent
        )
        {
            var sourceText = await context.Document.GetTextAsync().ConfigureAwait(false);
            var callMethod = $"{methodName.ValueText}({string.Join(", ", variables.Select(x => x.Name))});";
            sourceText = sourceText.Replace(
                    parentClass.FullSpan,
                    newCode ?? string.Empty
                )
                .Replace(diagnostic.AdditionalLocations[0].SourceSpan, callMethod)
                .Replace(declaration.FullSpan, codeIndent.ToFullString() + callMethod);

            return sourceText;
        }

        private static ClassDeclarationSyntax UpdateClass(
            MethodDeclarationSyntax parentMethod,
            ClassDeclarationSyntax parentClass,
            SyntaxToken methodName,
            List<ISymbol> variables,
            SyntaxNode declaration,
            SyntaxTriviaList codeIndent)
        {
            var methodIndent = parentMethod.GetLeadingTrivia();
            return parentClass.AddMembers(
                SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)
                            .WithTrailingTrivia(SyntaxFactory.Space)
                        ),
                        methodName
                    )
                    .WithModifiers(parentMethod.Modifiers.Any()
                        ? parentMethod.Modifiers
                        : SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxFactory.Space)
                        )
                    )
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
                                variables.Select(x =>
                                    SyntaxFactory.Parameter(
                                            SyntaxFactory.Identifier(x.Name)
                                        )
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
                                )
                            )
                        )
                    )
                    .WithBody(
                        SyntaxFactory.Block(
                                SyntaxFactory
                                    .ParseStatement(declaration.ToFullString())
                                    .WithLeadingTrivia(codeIndent.Insert(0, SyntaxFactory.CarriageReturnLineFeed))
                            )
                            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(methodIndent)
                            )
                            .WithLeadingTrivia(
                                methodIndent.Insert(0, SyntaxFactory.CarriageReturnLineFeed)
                            )
                            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                    )
                    .WithLeadingTrivia(
                        methodIndent.Insert(0, SyntaxFactory.CarriageReturnLineFeed)
                    )
            );
        }

        private static List<ISymbol> GetVariablesThatIntersects(SyntaxNode declaration, SemanticModel semanticModel)
        {
            var declaredSymbols = GetChildSymbols(declaration, semanticModel);

            var outerSymbols = GetOuterSymbols(semanticModel, declaration);

            return declaredSymbols.Intersect(outerSymbols).ToList();
        }

        private static IEnumerable<ISymbol> GetOuterSymbols(SemanticModel semanticModel, SyntaxNode declaration) =>
            semanticModel.AnalyzeDataFlow(declaration).ReadOutside
                .Where(symbol =>
                    symbol.Kind == SymbolKind.Local ||
                    symbol.Kind == SymbolKind.Parameter
                );

        private static IEnumerable<ISymbol> GetChildSymbols(SyntaxNode declaration, SemanticModel semanticModel) =>
            declaration.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Select(x => semanticModel.GetSymbolInfo(x).Symbol)
                .Where(x =>
                    x != null &&
                    (
                        x.Kind == SymbolKind.Local ||
                        x.Kind == SymbolKind.Parameter
                    )
                )
                .Distinct();
    }
}