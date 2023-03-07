using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    public static class Helper
    {
        public static IEnumerable<SyntaxNode> FindAllParent(this SyntaxNode syntaxNode, params Type[] types)
        {
            var method = syntaxNode.Parent;
            while (true)
            {
                if (method == null)
                {
                    yield break;
                }

                if (types.Contains(method.GetType()))
                {
                    yield return method;
                }

                method = method.Parent;
            }
        }

        public static SyntaxNode FindFirstParent(this SyntaxNode syntaxNode, params Type[] types)
        {
            var method = syntaxNode.Parent;
            while (true)
            {
                if (method == null || types.Contains(method.GetType()))
                {
                    break;
                }

                method = method.Parent;
            }

            return method;
        }

        public static string GetIdentifier(this BaseTypeSyntax baseTypeSyntax)
        {
            var s = new StringBuilder(baseTypeSyntax.ToString());
            s.Append(".");

            if (baseTypeSyntax.Parent?.Parent?.Parent is NamespaceDeclarationSyntax item)
            {
                s.Append(item.Name.ToString());
            }

            if (baseTypeSyntax.Parent?.Parent?.Parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax)
            {
                s.Append(fileScopedNamespaceDeclarationSyntax.Name.ToString());
            }

            return s.ToString();
        }

        public static IEnumerable<ISymbol> GetMembers(this ClassDeclarationSyntax classDeclaration, Compilation compilation)
        {
            var declaredSymbol = compilation
                .GetSemanticModel(classDeclaration.SyntaxTree)
                .GetDeclaredSymbol(classDeclaration);

            foreach (var member in ((ITypeSymbol)declaredSymbol).GetMembers())
            {
                yield return member;
            }
        }

        public static T HasParent<T>(this SyntaxToken syntaxNode)
        {
            var method = syntaxNode.Parent;
            while (true)
            {
                if (method is T || method == null)
                {
                    break;
                }

                method = method.Parent;
            }

            return method is T node ? node : default;
        }

        public static T HasParent<T>(this SyntaxNode syntaxNode)
        {
            var method = syntaxNode.Parent;
            while (true)
            {
                if (method is T || method == null)
                {
                    break;
                }

                method = method.Parent;
            }

            return method is T node ? node : default;
        }
    }
}