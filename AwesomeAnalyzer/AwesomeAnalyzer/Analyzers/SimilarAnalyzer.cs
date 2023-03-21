using System.Collections.Immutable;
using System.Linq;
using FleetManagement.Service;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ConstructorDeclaration
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

                var blockSyntaxes = context.Node
                    .DescendantNodes()
                    .OfType<BlockSyntax>()
                    .Where(x => x.Parent is StatementSyntax)
                    .Select(x => x.Parent)
                    .ToList();

                var groups = blockSyntaxes.GroupBy(GetString).Where(x => x.Count() > 1);

                foreach (var group in groups)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.Rule0008Similar,
                            group.First().GetLocation(),
                            group.Skip(1).Select(x => x.GetLocation()),
                            null,
                            null
                        )
                    );
                }

                //var similarNodes = root.DescendantNodes()
                //    .OfType<MethodDeclarationSyntax>()
                //    .Where(md => md != methodDeclaration)
                //    .Where(md => IsSimilarMethod(md, methodDeclaration, semanticModel, tokens));

                //foreach (var similarNode in similarNodes)
                //{
                //    Debug.WriteLine("SIMILAR NODE");
                //    context.ReportDiagnostic(Diagnostic.Create(
                //        DiagnosticDescriptors.SimilarRule0008,
                //        similarNode.GetLocation()
                //    ));
                //}
            }
        }

        private static bool IsSimilarBlock(
            SyntaxNode node1,
            SyntaxNode node2
        )
        {
            var node1Code = node1.GetText();
            var node2Code = node2.GetText();

            var node1Decendants = node1.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(x => x.IsKind(SyntaxKind.StringLiteralExpression)).ToImmutableList();
            var node2Decendants = node2.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(x => x.IsKind(SyntaxKind.StringLiteralExpression)).ToImmutableList();
            if (node1Decendants.Count > 0 && node1Decendants.Count() == node2Decendants.Count())
            {
                foreach (var textSpan in node1Decendants.Select(x => x.Span))
                {
                    node1Code = node1Code.Replace(textSpan.Start - node1.FullSpan.Start, textSpan.Length, string.Empty);
                }

                foreach (var textSpan in node2Decendants.Select(x => x.Span)) {
                    node2Code = node2Code.Replace(textSpan.Start - node2.FullSpan.Start, textSpan.Length, string.Empty);
                }
            }

            var isSimilarBlock = node1Code.ToString().Trim() == node2Code.ToString().Trim();
            //Debug.WriteLine("node1Code:" + node1Code.ToString().Trim());
            //Debug.WriteLine("isSimilarBlock:" + isSimilarBlock);
            //Debug.WriteLine("node2Code:" + node2Code.ToString().Trim());

            return isSimilarBlock;
        }

        private static string GetString(SyntaxNode node)
        {
            var nodeCode = node.GetText();

            var nodeDecendants = node.DescendantNodes()
                .OfType<LiteralExpressionSyntax>()
                .Where(x => x.IsKind(SyntaxKind.StringLiteralExpression))
                .ToImmutableList();

            if (nodeDecendants.Count <= 0) return nodeCode.ToString().Trim();
            
            foreach (var textSpan in nodeDecendants.Select(x => x.Span))
            {
                nodeCode = nodeCode.Replace(
                    textSpan.Start - node.FullSpan.Start, 
                    textSpan.Length, 
                    string.Empty
                );
            }

            return nodeCode.ToString().Trim();
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