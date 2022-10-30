using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    public class SortVirtualizationVisitor : CSharpSyntaxRewriter
    {
        public enum Types
        {
            Enum = 1,
            Field = 2,
            Constructor = 3,
            Methods = 4,
        }

        public enum ModifiersSort
        {
            PublicConst = 1,
            PrivateConst = 2,
            PublicStatic = 3,
            PublicReadOnly = 4,
            Public = 5,
            PrivateReadOnly = 6,
            Private = 7
        }

        public Dictionary<Types, List<MethodInformation>> Members { get; }

        public SortVirtualizationVisitor()
        {
            this.Members = new Dictionary<Types, List<MethodInformation>>();
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var modifiers = node.Modifiers.Select(x => x.ValueText).ToList();
            var modifiersString = string.Join(string.Empty, modifiers);
            var item = new MethodInformation
            {
                Name = node.Identifier.ValueText,
                FullSpan = node.FullSpan,
                Modifiers = string.Join(",", modifiers),
                ModifiersOrder = string.IsNullOrEmpty(modifiersString) ? (int)ModifiersSort.Private : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true)
            };

            if (this.Members.ContainsKey(Types.Enum))
            {
                this.Members[Types.Enum].Add(item);
            }
            else
            {
                this.Members.Add(
                    Types.Enum,
                    new List<MethodInformation> { item }
                );
            }

            return node;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var modifiers = node.Modifiers.Select(x => x.ValueText).ToList();
            var modifiersString = string.Join(string.Empty, modifiers);
            var item = new MethodInformation
            {
                Name = node.Declaration.Variables.ToFullString(),
                FullSpan = node.FullSpan,
                Modifiers = string.Join(",", modifiers),
                ModifiersOrder = string.IsNullOrEmpty(modifiersString) ? (int)ModifiersSort.Private : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true)
            };

            if (this.Members.ContainsKey(Types.Field))
            {
                this.Members[Types.Field].Add(item);
            }
            else
            {
                this.Members.Add(
                    Types.Field,
                    new List<MethodInformation> { item }
                );
            }

            return node;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var item = new MethodInformation
            {
                Name = node.Identifier.ValueText,
                FullSpan = node.FullSpan
            };

            if (this.Members.ContainsKey(Types.Constructor))
            {
                this.Members[Types.Constructor].Add(item);
            }
            else
            {
                this.Members.Add(
                    Types.Constructor,
                    new List<MethodInformation> { item }
                );
            }

            return node;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var modifiers = node.Modifiers.Select(x => x.ValueText).ToList();
            var modifiersString = string.Join(string.Empty, modifiers);
            var item = new MethodInformation
            {
                Name = node.Identifier.ValueText,
                FullSpan = node.FullSpan,
                Modifiers = string.Join(",", modifiers),
                ModifiersOrder = string.IsNullOrEmpty(modifiersString) ? (int)ModifiersSort.Private : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true)
            };

            if (this.Members.ContainsKey(Types.Methods))
            {
                this.Members[Types.Methods].Add(item);
            }
            else
            {
                this.Members.Add(
                    Types.Methods,
                    new List<MethodInformation> { item }
                );
            }

            return node;
        }
    }

    public class MethodInformation
    {
        public string Name { get; set; }

        public TextSpan FullSpan { get; set; }

        public string Modifiers { get; set; }

        public int ModifiersOrder { get; set; }
    }
}