﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    public sealed class SortVirtualizationVisitor : CSharpSyntaxRewriter
    {
        public enum ModifiersSort
        {
            PublicConst = 1,
            PublicStatic = 2,
            PublicStaticReadOnly = 3,
            PublicReadOnly = 4,
            PublicNew = 5,
            Public = 6,
            InternalConst = 7,
            InternalStatic = 8,
            InternalStaticReadOnly = 9,
            InternalReadOnly = 10,
            InternalNew = 11,
            Internal = 12,
            PrivateConst = 13,
            PrivateStatic = 14,
            PrivateStaticReadOnly = 15,
            PrivateReadOnly = 16,
            PrivateNew = 17,
            Private = 18,
        }

        public enum Types
        {
            Other = 10,
            Field = 1,
            Constructor = 2,
            Delegate = 3,
            EventField = 4,
            Event = 5,
            Enum = 6,
            Property = 7,
            Methods = 8,
        }

        private const string TextAsync = "async";
        private const string TextComma = ",";
        private readonly CancellationToken _contextCancellationToken;

        public SortVirtualizationVisitor(CancellationToken contextCancellationToken)
        {
            _contextCancellationToken = contextCancellationToken;
            Members = new ConcurrentDictionary<Types, List<TypesInformation>>();
            Classes = new ConcurrentDictionary<TextSpan, ClassInformation>();
        }

        public ConcurrentDictionary<Types, List<TypesInformation>> Members { get; }

        public ConcurrentDictionary<TextSpan, ClassInformation> Classes { get; }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            AddClass(node);

            return base.VisitInterfaceDeclaration(node);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            AddClass(node);

            return base.VisitStructDeclaration(node);
        }

        public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            AddClass(node);

            return base.VisitRecordDeclaration(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            AddClass(node);

            return base.VisitClassDeclaration(node);
        }

        private void AddClass(TypeDeclarationSyntax node)
        {
            var parentClass = GetClassName(node);
            if (parentClass == string.Empty)
            {
                var item = new ClassInformation
                {
                    ClassName = $"{node.Identifier.ValueText}{node.TypeParameterList}",
                    FullSpan = node.FullSpan,
                };

                Classes.AddOrUpdate(
                    item.FullSpan,
                    _ => item,
                    (_, information) => information
                );
            }
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            var item = new TypesInformation(
                Types.Enum,
                node.Identifier.ValueText,
                node.FullSpan,
                //default,
                default,
                GetClassName(node)
            );

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.Enum);

            return base.VisitEnumDeclaration(node);
        }

        private static string GetClassName(SyntaxNode node)
        {
            if (node.Parent is NamespaceDeclarationSyntax || node.Parent is FileScopedNamespaceDeclarationSyntax)
            {
                return string.Empty;
            }

            var parents = node.FindAllParent(
                typeof(RecordDeclarationSyntax),
                typeof(ClassDeclarationSyntax),
                typeof(InterfaceDeclarationSyntax),
                typeof(StructDeclarationSyntax)
            )
            .OfType<TypeDeclarationSyntax>()
            .Reverse();

            return string.Join(".", parents.Select(x => $"{x.Identifier.ValueText}{x.TypeParameterList}"));
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            var item = new TypesInformation(
                Types.Field,
                node.Declaration.Variables[0].Identifier.ValueText,
                node.FullSpan,
                //default,
                default,
                GetClassName(node)
            );

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.Field);

            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            var item = new TypesInformation(
                Types.Constructor,
                node.Identifier.ValueText,
                node.FullSpan,
                //default,
                default,
                GetClassName(node)
            );

            AddToList(item, Types.Constructor);

            return base.VisitConstructorDeclaration(node);
        }

        public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            var item = new TypesInformation(
                Types.Delegate,
                node.Identifier.ValueText,
                node.FullSpan,
                //default,
                default,
                GetClassName(node)
            );

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.Delegate);

            return base.VisitDelegateDeclaration(node);
        }

        public override SyntaxNode VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            var item = new TypesInformation(
                Types.EventField,
                node.Declaration.Variables[0].Identifier.ValueText,
                node.FullSpan,
                //default,
                default,
                GetClassName(node)
            );

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.EventField);

            return base.VisitEventFieldDeclaration(node);
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            var item = new TypesInformation(
                Types.Event,
                node.Identifier.ValueText,
                node.FullSpan,
                //default,
                default,
                GetClassName(node)
            );

            SetModifiers(node.Modifiers, item);

            AddToList(item, Types.Event);

            return base.VisitEventDeclaration(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            var modifiers = Enumerable.Range(0, node.Modifiers.Count).Select(x => node.Modifiers[x].ValueText);
            var modifiersString = string.Join(string.Empty, modifiers.Where(x => x != TextAsync));
            var item = new TypesInformation(
                Types.Property,
                node.Identifier.ValueText,
                node.FullSpan,
                //string.Join(TextComma, modifiers),
                string.IsNullOrEmpty(modifiersString)
                    ? (int)ModifiersSort.Private
                    : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true),
                GetClassName(node)
            );

            AddToList(item, Types.Property);

            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (_contextCancellationToken.IsCancellationRequested) return null;
            var modifiers = Enumerable.Range(0, node.Modifiers.Count).Select(x => node.Modifiers[x].ValueText);
            var modifiersString = string.Join(string.Empty, modifiers.Where(x => x != TextAsync));
            var item = new TypesInformation(
                Types.Methods,
                node.Identifier.ValueText,
                node.FullSpan,
                //string.Join(TextComma, modifiers),
                string.IsNullOrEmpty(modifiersString)
                ? (int)ModifiersSort.Private
                : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true),
                GetClassName(node)
            );

            AddToList(item, Types.Methods);

            return base.VisitMethodDeclaration(node);
        }

        private static void SetModifiers(SyntaxTokenList modifiers, TypesInformation item)
        {
            var modifiersText = Enumerable.Range(0, modifiers.Count).Select(x => modifiers[x].ValueText);
            var modifiersString = string.Join(string.Empty, modifiersText.Where(x => x != TextAsync));

            //item.Modifiers = string.Join(TextComma, modifiersText);
            item.ModifiersOrder = string.IsNullOrEmpty(modifiersString)
            ? (int)ModifiersSort.Private
            : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true);
        }

        private void AddToList(TypesInformation item, Types constructor)
        {
            Members.AddOrUpdate(
                constructor,
                _ => new List<TypesInformation> { item },
                (_, list) =>
                {
                    lock (list)
                    {
                        if (list.Any(x => x.FullSpan == item.FullSpan))
                        {
                            return list;
                        }

                        list.Add(item);
                        return list;
                    }
                }
            );
        }
    }
}