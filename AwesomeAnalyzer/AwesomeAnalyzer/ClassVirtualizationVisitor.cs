using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    public sealed class ClassVirtualizationVisitor : CSharpSyntaxRewriter
    {
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
                    ClassName = x.ToString(),
                    NameSpaceName = x.Parent?.Parent?.Parent is NamespaceDeclarationSyntax item ? item.Name.ToString() : ((FileScopedNamespaceDeclarationSyntax)x.Parent?.Parent?.Parent)?.Name.ToString(),
                }).ToList(),
            });

            return node;
        }
    }
}