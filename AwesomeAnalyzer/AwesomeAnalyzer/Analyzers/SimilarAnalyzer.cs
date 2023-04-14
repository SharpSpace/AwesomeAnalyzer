using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SimilarAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0008Similar
        );

        public override void Initialize(
            AnalysisContext context
        )
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                AnalyzeNode,
                SyntaxKind.NamespaceDeclaration,
                SyntaxKind.FileScopedNamespaceDeclaration,
                SyntaxKind.ClassDeclaration
            );
        }

        private static void AnalyzeNode(
            SyntaxNodeAnalysisContext context
        )
        {
            using (var _ = new MeasureTime(true))
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0008Similar.Id))
                {
                    return;
                }

                var groups = context.Node
                    .DescendantNodes()
                    .OfType<BlockSyntax>()
                    .Where(x => x.Parent is StatementSyntax)
                    .Select(x => x.Parent)
                    .GroupBy(x => GetString(x, context.SemanticModel))
                    .Where(x => x.Count() > 1);

                foreach (var group in groups)
                {
                    foreach (var item in group)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.Rule0008Similar,
                                item.GetLocation(),
                                group.Except(new []{ item }).Select(x => x.GetLocation()),
                                null,
                                null
                            )
                        );
                    }
                }
            }
        }

        private static string GetString(SyntaxNode node, SemanticModel semanticModel)
        {
            var nodeCode = node.GetText();
            var replaceList = node.DescendantNodesAndSelf()
                .OfType<PredefinedTypeSyntax>()
                .Where(x => x.IsVar == false)
                .Select(
                    predefinedTypeSyntax => new TextSpan(
                        predefinedTypeSyntax.Span.Start - node.FullSpan.Start,
                        predefinedTypeSyntax.Span.Length
                    )
                )
                .Select(textSpan => (textSpan, "var"))
                .ToList();

            var variables = node.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Select(x => new { SyntaxNode = x, semanticModel.GetSymbolInfo(x).Symbol })
                .Where(x =>
                    x.Symbol != null
                    &&
                    (
                        x.Symbol.Kind == SymbolKind.Local ||
                        x.Symbol.Kind == SymbolKind.Parameter
                    )
                );

            var variableDeclaratorSyntaxes = node.DescendantNodesAndSelf().OfType<VariableDeclaratorSyntax>();

            var variableNames = new Dictionary<string, string>();
            var count = 1;

            foreach (var variableDeclaratorSyntax in variableDeclaratorSyntaxes)
            {
                var syntax = (variableDeclaratorSyntax.Parent as VariableDeclarationSyntax).Type;

                var symbol = semanticModel.GetSymbolInfo(syntax).Symbol;
                var name = $"{symbol.MetadataName}_{count++}";
                if (variableNames.ContainsKey(variableDeclaratorSyntax.Identifier.ValueText))
                {
                    name = variableNames[variableDeclaratorSyntax.Identifier.ValueText];
                }
                else
                {
                    variableNames.Add(variableDeclaratorSyntax.Identifier.ValueText, name);
                }

                replaceList.Add((
                    new TextSpan(
                        variableDeclaratorSyntax.Identifier.Span.Start - node.FullSpan.Start,
                        variableDeclaratorSyntax.Identifier.Span.Length
                    ),
                    name
                ));
            }

            foreach (var variable in variables.OrderByDescending(x => x.SyntaxNode.SpanStart))
            {
                var typeName = variable.Symbol is ILocalSymbol localSymbol
                    ? localSymbol.Type.Name
                    : variable.Symbol is IParameterSymbol parameterSymbol
                        ? parameterSymbol.Type.Name
                        : ((INamedTypeSymbol)variable.Symbol).Name;
                var name = $"{typeName}_{count++}";
                if (variableNames.ContainsKey(variable.Symbol.Name))
                {
                    name = variableNames[variable.Symbol.Name];
                }
                else
                {
                    variableNames.Add(variable.Symbol.Name, name);
                }

                replaceList.Add((
                    new TextSpan(
                        variable.SyntaxNode.Span.Start - node.FullSpan.Start,
                        variable.SyntaxNode.Span.Length
                    ),
                    name
                ));
            }

            var nodeDecendants = node.DescendantNodes()
                .OfType<LiteralExpressionSyntax>()
                .ToImmutableList();

            if (nodeDecendants.Count <= 0)
            {
                return FinalFix();
            }

            replaceList.AddRange(nodeDecendants
                .Select(x => x.Span)
                .OrderByDescending(x => x.Start)
                .Select(
                    textSpan => (
                        new TextSpan(
                            textSpan.Start - node.FullSpan.Start,
                            textSpan.Length
                        ),
                        string.Empty
                    )
                )
            );

            return FinalFix();

            string FinalFix()
            {
                nodeCode = replaceList
                    .OrderByDescending(x => x.Item1.Start)
                    .Aggregate(
                        nodeCode,
                        (text, tuple) =>
                        {
                            var start = tuple.Item1.Start;
                            var length = tuple.Item1.Length;

                            // Debug.WriteLine("textOut: '" + text.GetSubText(new TextSpan(start, length)) + "' -> '" + tuple.Item2 + "'");
                            var result = text.Replace(
                                start,
                                length,
                                tuple.Item2
                            );
                            return result;
                        }
                    );

                var fixString = FixString2(nodeCode).ToString();
                return fixString;
            }
        }

        private static SourceText FixString2(SourceText code)
        {
            var prevChars = new StringBuilder();

            for (var i = code.Length - 1; i > 0; i--)
            {
                var c = code[i];

                if (c == '\r' || c == '\n')
                {
                    code = code.Replace(i, 1, string.Empty);
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    prevChars.Append(c);
                }
                else if (prevChars.Length > 1)
                {
                    code = code.Replace(i + 1, prevChars.Length, string.Empty);
                    prevChars.Clear();
                }
                else
                {
                    prevChars.Clear();
                }
            }

            if (prevChars.Length > 1)
            {
                code = code.Replace(0, prevChars.Length + 1, string.Empty);
            }

            return code;
        }

        private static string FixString(string code)
        {
            return code.Trim()
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("    ", " ")
                .Replace("   ", " ")
                .Replace("  ", " ")
                .Replace("  ", " ")
                ;
        }

        private static bool IsSimilarMethod(
            MethodDeclarationSyntax node1,
            MethodDeclarationSyntax node2,
            SemanticModel semanticModel
        )
        {
            var tokens1 = node1.DescendantTokens().Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)).ToArray();
            var tokens2 = node2.DescendantTokens().Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)).ToArray();

            if (tokens1.Length != tokens2.Length) return false;

            for (var i = 0; i < tokens1.Length; i++)
            {
                if (tokens1[i].IsKind(SyntaxKind.IdentifierToken) && tokens2[i].IsKind(SyntaxKind.IdentifierToken))
                {
                    var symbol1 = semanticModel.GetSymbolInfo(tokens1[i].Parent).Symbol;
                    var symbol2 = semanticModel.GetSymbolInfo(tokens2[i].Parent).Symbol;
                    if (symbol1 != null && symbol2 != null && symbol1.Equals(symbol2)) continue;
                }

                if (!tokens1[i].IsEquivalentTo(tokens2[i])) return false;
            }

            return true;
        }
    }
}