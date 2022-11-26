using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    public class ClassVirtualizationVisitor : CSharpSyntaxRewriter
    {
        public List<ClassInformation> Classes { get; }

        public ClassVirtualizationVisitor()
        {
            this.Classes = new List<ClassInformation>();
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            //node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

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
                    NameSpaceName = ((NamespaceDeclarationSyntax)x.Parent.Parent.Parent).Name.ToString()
                }).ToList()
            });

            return node;
        }
    }

    public class ClassInformation
    {
        public string ClassName { get; set; }

        public string NameSpaceName { get; set; }

        public List<ClassInformation> BaseClasses { get; set; }

        public string IdentifierName => $"{this.NameSpaceName}.{this.ClassName}";
        
        public TextSpan FullSpan { get; set; }
    }
}