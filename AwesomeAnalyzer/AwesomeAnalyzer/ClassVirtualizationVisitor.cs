using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    public sealed class ClassVirtualizationVisitor : CSharpSyntaxRewriter
    {
        private Compilation _compilation;

        public ClassVirtualizationVisitor()
        {
            this.Classes = new List<ClassInformation>();
        }

        public List<ClassInformation> Classes { get; }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            string nameSpaceName = null;
            if (node.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
            {
                nameSpaceName = namespaceDeclarationSyntax.Name.ToString();
            }

            this.Classes.Add(new ClassInformation
            {
                ClassName = node.Identifier.ValueText,
                NameSpaceName = nameSpaceName,
                BaseClasses = node.BaseList?.Types.Select(x => new ClassInformation
                {
                    ClassName = GetClassName(x),
                    NameSpaceName = GetNameSpaceName(x),
                }).ToList() ?? new List<ClassInformation>(),
            });

            return node;
        }

        private static string GetClassName(BaseTypeSyntax x)
        {
            if (!(x is SimpleBaseTypeSyntax simpleBaseTypeSyntax))
            {
                return x.ToString();
            }

            if (simpleBaseTypeSyntax.Type is QualifiedNameSyntax qualifiedNameSyntax)
            {
                return qualifiedNameSyntax.Right.ToString();
            }

            if (simpleBaseTypeSyntax.Type is GenericNameSyntax genericNameSyntax)
            {
                return genericNameSyntax.Identifier.ValueText;
            }

            return x.ToString();
        }

        private string GetNameSpaceName(BaseTypeSyntax x)
        {
            var symbolInfo = _compilation.GetSemanticModel(x.SyntaxTree)?.GetSymbolInfo(x.Type);
            return symbolInfo?.Symbol?.ContainingNamespace.ToDisplayString() ?? string.Empty;
        }

        public void SetCompilation(Compilation compilation)
        {
            _compilation = compilation;
        }
    }
}