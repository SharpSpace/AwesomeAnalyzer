using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public sealed class AddAwaitCodeFixProvider : CodeFixProvider
    {
        private const string TextAsync = "async";
        private const string TextTask = "Task";
        private const string TextAwait = "await ";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.Rule0101AddAwait.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                    .Parent
                    ?.AncestorsAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .First();

                if (declaration == null) continue;
                if (declaration.Parent is AwaitExpressionSyntax) continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add Await",
                        token => AddAwaitAsync(context.Document, declaration, token),
                        equivalenceKey: "AddAwaitCodeFixTitle"
                    ),
                    diagnostic
                );
            }
        }

        private async Task<Document> AddAwaitAsync(
            Document document,
            InvocationExpressionSyntax declaration,
            CancellationToken token
        )
        {
            //Debug.WriteLine($"declaration: {declaration.GetType().Name} {declaration.ToFullString()}");

            var sourceText = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false)).GetText();
            if (sourceText == null)
            {
                return document;
            }

            //Debug.WriteLine($"sourceText: {sourceText}");

            var replaceList = new List<(TextSpan, string)>();

            foreach (var parenthesizedLambdaExpressionSyntax in declaration.DescendantNodesAndSelf().OfType<ParenthesizedLambdaExpressionSyntax>().OrderByDescending(x => x.SpanStart)) {
                Debug.WriteLine($"parenthesizedLambdaExpressionSyntax: {parenthesizedLambdaExpressionSyntax.ToFullString()}");
                replaceList.Add((
                    new TextSpan(
                        parenthesizedLambdaExpressionSyntax.Span.Start,
                        0
                    ),
                    "async "
                ));

                replaceList.Add((
                    new TextSpan(
                        parenthesizedLambdaExpressionSyntax.ExpressionBody.Span.Start,
                        parenthesizedLambdaExpressionSyntax.ExpressionBody.Span.Length
                    ),
                    "await " + parenthesizedLambdaExpressionSyntax.ExpressionBody.ToString()
                ));
            }

            var methodDeclarationSyntax = declaration.HasParent<MethodDeclarationSyntax>();
            var methodModifiers = methodDeclarationSyntax?.Modifiers.ToList();
            if ((methodModifiers?.Any(x => x.ValueText == TextAsync) ?? false) == false) {
                var newType = methodDeclarationSyntax.ReturnType.ToString() != "void"
                    ? $"Task<{methodDeclarationSyntax.ReturnType}>"
                    : TextTask;

                replaceList.Add((
                    methodDeclarationSyntax.ReturnType.Span,
                    $"async {newType}"
                ));
            }

            if (methodDeclarationSyntax.ExpressionBody != null)
            {
                replaceList.Add((
                    methodDeclarationSyntax.ExpressionBody.Expression.Span,
                    $"await {methodDeclarationSyntax.ExpressionBody.Expression}"
                ));
            }
            else if (methodDeclarationSyntax.Body != null)
            {
                var semanticModel = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
                foreach (var syntax in methodDeclarationSyntax.Body.DescendantNodes().OfType<ExpressionStatementSyntax>())
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(syntax.Expression).Symbol as IMethodSymbol;
                    if (symbolInfo?.ReturnType.Name.StartsWith("Task") ?? false)
                    {
                        replaceList.Add((
                            new TextSpan(
                                syntax.Expression.SpanStart,
                                0
                            ),
                            "await "
                        ));
                    }
                    //Debug.WriteLine($"{symbolInfo.ReturnType.Name} {invocationExpressionSyntax.GetType().Name}: {invocationExpressionSyntax.ToFullString()}");
                }
            }

            sourceText = replaceList
                .OrderByDescending(x => x.Item1.Start)
                .Aggregate(
                    sourceText,
                    (text, tuple) => {
                        var start = tuple.Item1.Start;
                        var length = tuple.Item1.Length;
                        //Debug.WriteLine("textOut: '" + text.GetSubText(new TextSpan(start, length)) + "' -> '" + tuple.Item2 + "'");
                        var result = text.Replace(
                            start,
                            length,
                            tuple.Item2
                        );
                        //Debug.WriteLine($"Result: {result}");
                        return result;
                    }
                );

            return document.WithText(sourceText);
        }
    }
}