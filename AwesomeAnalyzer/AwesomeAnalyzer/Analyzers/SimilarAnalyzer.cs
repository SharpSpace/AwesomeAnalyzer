using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            DiagnosticDescriptors.SimilarRule0008
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var tokens = methodDeclaration.DescendantTokens().Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)).ToArray();

            var blockSyntaxes = methodDeclaration
                .DescendantNodes()
                .OfType<BlockSyntax>()
                .Where(x => x.Parent is StatementSyntax)
                .Select(x => x.Parent)
                .ToList();

            var semanticModel = context.SemanticModel;
            var skipIndex = new List<int>();

            for (var i = 0; i < blockSyntaxes.Count; i++)
            {
                var firstBlock = blockSyntaxes[i];

                for (var j = 0; j < blockSyntaxes.Count; j++)
                {
                    if (i == j) continue;
                    if (!IsSimilarBlock(blockSyntaxes[j], firstBlock, semanticModel, tokens)) continue;

                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.SimilarRule0008,
                        firstBlock.GetLocation(),
                        new[] { blockSyntaxes[j].GetLocation() },
                        null,
                        null
                    ));
                    skipIndex.Add(j);
                }
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

        private static bool IsSimilarBlock(SyntaxNode node1, SyntaxNode node2, SemanticModel semanticModel, SyntaxToken[] tokens)
        {
            return node1.WithLeadingTrivia().WithTrailingTrivia().ToFullString() == node2.WithLeadingTrivia().WithTrailingTrivia().ToFullString();
        }

        private static bool IsSimilarMethod(MethodDeclarationSyntax node1, MethodDeclarationSyntax node2, SemanticModel semanticModel, SyntaxToken[] tokens)
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