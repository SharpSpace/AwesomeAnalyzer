using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    public sealed class SortVirtualizationVisitor : CSharpSyntaxRewriter
    {
        public enum Types
        {
            Field = 1,
            Constructor = 2,
            Delegate = 3,
            EventField = 4,
            Event = 5,
            Enum = 6,
            Property = 7,
            Methods = 8,
        }

        public enum ModifiersSort
        {
            PublicConst = 1,
            PrivateConst = 2,
            PublicStatic = 3,
            PublicReadOnly = 4,
            Public = 5,
            PrivateStatic = 6,
            PrivateReadOnly = 7,
            Private = 8
        }

        public Dictionary<Types, List<TypesInformation>> Members { get; }

        public SortVirtualizationVisitor()
        {
            this.Members = new Dictionary<Types, List<TypesInformation>>();
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var item = new TypesInformation
            {
                Name = node.Identifier.ValueText,
                FullSpan = node.FullSpan,
            };

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.Enum);

            return node;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var item = new TypesInformation
            {
                Name = node.Declaration.Variables[0].Identifier.ValueText,
                FullSpan = node.FullSpan,
            };

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.Field);

            return node;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var item = new TypesInformation
            {
                Name = node.Identifier.ValueText,
                FullSpan = node.FullSpan
            };

            AddToList(item, Types.Constructor);

            return node;
        }

        public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            var item = new TypesInformation
            {
                Name = node.Identifier.ValueText,
                FullSpan = node.FullSpan,
            };

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.Delegate);

            return node;
        }

        public override SyntaxNode VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            var item = new TypesInformation
            {
                Name = node.Declaration.Variables[0].Identifier.ValueText,
                FullSpan = node.FullSpan,
            };

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.EventField);

            return node;
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var item = new TypesInformation
            {
                Name = node.Identifier.ValueText,
                FullSpan = node.FullSpan
            };

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.Event);

            return node;
        }

        private static void SetModifiers(SyntaxTokenList modifiers, TypesInformation item)
        {
            var modifiersText = modifiers.Select(x => x.ValueText).ToList();
            var modifiersString = string.Join(string.Empty, modifiersText);

            item.Modifiers = string.Join(",", modifiersText);
            item.ModifiersOrder = string.IsNullOrEmpty(modifiersString) ? (int)ModifiersSort.Private : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var modifiers = node.Modifiers.Select(x => x.ValueText).ToList();
            var modifiersString = string.Join(string.Empty, modifiers);
            var item = new TypesInformation
            {
                Name = node.Identifier.ValueText,
                FullSpan = node.FullSpan,
                Modifiers = string.Join(",", modifiers),
                ModifiersOrder = string.IsNullOrEmpty(modifiersString) ? (int)ModifiersSort.Private : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true)
            };

            AddToList(item, Types.Property);

            return node;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var modifiers = node.Modifiers.Select(x => x.ValueText).ToList();
            var modifiersString = string.Join(string.Empty, modifiers);
            var item = new TypesInformation
            {
                Name = node.Identifier.ValueText,
                FullSpan = node.FullSpan,
                Modifiers = string.Join(",", modifiers),
                ModifiersOrder = string.IsNullOrEmpty(modifiersString) ? (int)ModifiersSort.Private : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true)
            };

            AddToList(item, Types.Methods);

            return node;
        }

        private void AddToList(TypesInformation item, Types constructor)
        {
            if (this.Members.ContainsKey(constructor))
            {
                this.Members[constructor].Add(item);
            }
            else
            {
                this.Members.Add(
                    constructor,
                    new List<TypesInformation> { item }
                );
            }
        }
    }

    public sealed class TypesInformation
    {
        public string Name { get; set; }

        public TextSpan FullSpan { get; set; }

        public string Modifiers { get; set; }

        public int ModifiersOrder { get; set; }
    }
}