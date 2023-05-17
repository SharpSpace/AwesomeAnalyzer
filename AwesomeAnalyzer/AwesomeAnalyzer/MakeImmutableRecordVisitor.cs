using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    public sealed class MakeImmutableRecordVisitor : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;
        private readonly IPropertySymbol _propertySymbol;
        private readonly SymbolEqualityComparer _symbolEqualityComparer;

        public MakeImmutableRecordVisitor(SemanticModel semanticModel, IPropertySymbol propertySymbol)
        {
            _semanticModel = semanticModel;
            _propertySymbol = propertySymbol;
            _symbolEqualityComparer = SymbolEqualityComparer.Default;
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            IsFound = node.DescendantNodes()
                .OfType<ExpressionSyntax>()
                .Any(x => _symbolEqualityComparer.Equals(
                        _semanticModel.GetSymbolInfo(x).Symbol,
                        _propertySymbol
                    )
                );

            if (IsFound)
            {
                return node;
            }

            return base.VisitExpressionStatement(node);
        }

        public bool IsFound { get; set; }
    }
}