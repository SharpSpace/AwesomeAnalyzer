using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SortAnalyzer : DiagnosticAnalyzer
    {
        private readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = ImmutableArray.Create(
            DiagnosticDescriptors.EnumSortRule1008,
            DiagnosticDescriptors.EnumOrderRule1009,
            DiagnosticDescriptors.FieldSortRule1001,
            DiagnosticDescriptors.FieldOrderRule1002,
            DiagnosticDescriptors.ConstructorOrderRule1005,
            DiagnosticDescriptors.DelegateSortRule1010,
            DiagnosticDescriptors.DelegateOrderRule1011,
            DiagnosticDescriptors.EventSortRule1012,
            DiagnosticDescriptors.EventOrderRule1013,
            DiagnosticDescriptors.PropertySortRule1006,
            DiagnosticDescriptors.PropertyOrderRule1007,
            DiagnosticDescriptors.MethodSortRule1003,
            DiagnosticDescriptors.MethodOrderRule1004
        );

        private static ImmutableHashSet<SortVirtualizationVisitor.Types> _types;

        public SortAnalyzer()
        {
            _types = Enum.GetValues(typeof(SortVirtualizationVisitor.Types)).OfType<SortVirtualizationVisitor.Types>().ToImmutableHashSet();
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Enum,
                    ((EnumDeclarationSyntax)x.Node).Identifier,
                    x.Node.FullSpan,
                    DiagnosticDescriptors.EnumOrderRule1009,
                    DiagnosticDescriptors.EnumSortRule1008
                ),
                SyntaxKind.EnumDeclaration
            );
            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Field,
                    ((FieldDeclarationSyntax)x.Node).Declaration.Variables[0].Identifier,
                    x.Node.FullSpan,
                    DiagnosticDescriptors.FieldOrderRule1002,
                    DiagnosticDescriptors.FieldSortRule1001
                ), 
                SyntaxKind.FieldDeclaration
            );

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Delegate,
                    ((DelegateDeclarationSyntax)x.Node).Identifier,
                    x.Node.FullSpan,
                    DiagnosticDescriptors.DelegateOrderRule1011,
                    DiagnosticDescriptors.DelegateSortRule1010
                ),
                SyntaxKind.DelegateDeclaration
            );

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.EventField,
                    ((EventFieldDeclarationSyntax)x.Node).Declaration.Variables[0].Identifier,
                    x.Node.FullSpan,
                    DiagnosticDescriptors.EventOrderRule1013,
                    DiagnosticDescriptors.EventSortRule1012
                ),
                SyntaxKind.EventFieldDeclaration
            );

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Property,
                    ((PropertyDeclarationSyntax)x.Node).Identifier,
                    x.Node.FullSpan,
                    DiagnosticDescriptors.PropertyOrderRule1007,
                    DiagnosticDescriptors.PropertySortRule1006
                ),
                SyntaxKind.PropertyDeclaration
            );

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Methods,
                    ((MethodDeclarationSyntax)x.Node).Identifier,
                    x.Node.FullSpan,
                    DiagnosticDescriptors.MethodOrderRule1004,
                    DiagnosticDescriptors.MethodSortRule1003
                ),
                SyntaxKind.MethodDeclaration
            );

            context.RegisterSyntaxNodeAction(AnalyzeConstructorNode, SyntaxKind.ConstructorDeclaration);
        }

        private void AnalyzeConstructorNode(SyntaxNodeAnalysisContext context)
        {
            var sortVirtualizationVisitor = new SortVirtualizationVisitor();
            sortVirtualizationVisitor.Visit(context.Node.Parent);
            var members = sortVirtualizationVisitor.Members
                .SelectMany(x => x.Value.Select(y => new { x.Key, y }))
                .ToLookup(x => x.Key, x => x.y);

            var constructorDeclarationSyntax = (ConstructorDeclarationSyntax)context.Node;

            if (AnalyzeOrder(
                    members,
                    SortVirtualizationVisitor.Types.Constructor,
                    constructorDeclarationSyntax.FullSpan
                ))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.ConstructorOrderRule1005,
                        constructorDeclarationSyntax.Identifier.GetLocation(),
                        messageArgs: new[] { constructorDeclarationSyntax.Identifier.ValueText }
                    )
                );
            }
        }

        private void Analyze(SyntaxNodeAnalysisContext context, SortVirtualizationVisitor.Types types, SyntaxToken syntaxToken, TextSpan fullSpan, DiagnosticDescriptor orderRule, DiagnosticDescriptor sortRule)
        {
            var sortVirtualizationVisitor = new SortVirtualizationVisitor();
            sortVirtualizationVisitor.Visit(context.Node.Parent);
            var members = sortVirtualizationVisitor.Members
                .SelectMany(x => x.Value.Select(y => new { x.Key, y }))
                .ToLookup(x => x.Key, x => x.y);
            var classes = sortVirtualizationVisitor.Classes.ToImmutableHashSet();

            AnalyzeOrderAndSort(
                classes,
                members,
                context,
                types,
                syntaxToken,
                fullSpan,
                orderRule,
                sortRule
            );
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var sortVirtualizationVisitor = new SortVirtualizationVisitor();
            sortVirtualizationVisitor.Visit(context.SemanticModel.SyntaxTree.GetRoot());
            var members = sortVirtualizationVisitor.Members
                .SelectMany(x => x.Value.Select(y => new { x.Key, y }))
                .ToLookup(x => x.Key, x => x.y);
            var classes = sortVirtualizationVisitor.Classes.ToImmutableHashSet();
            //context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree).

            switch (context.Node)
            {
                case EnumDeclarationSyntax enumDeclarationSyntax:
                    AnalyzeOrderAndSort(
                        classes,
                        members,
                        context,
                        SortVirtualizationVisitor.Types.Enum,
                        enumDeclarationSyntax.Identifier,
                        enumDeclarationSyntax.FullSpan,
                        DiagnosticDescriptors.EnumOrderRule1009,
                        DiagnosticDescriptors.EnumSortRule1008
                    );

                    break;
                case FieldDeclarationSyntax fieldDeclarationSyntax:
                    AnalyzeOrderAndSort(
                        classes,
                        members,
                        context,
                        SortVirtualizationVisitor.Types.Field,
                        fieldDeclarationSyntax.Declaration.Variables[0].Identifier,
                        fieldDeclarationSyntax.FullSpan,
                        DiagnosticDescriptors.FieldOrderRule1002,
                        DiagnosticDescriptors.FieldSortRule1001
                    );

                    return;
                case ConstructorDeclarationSyntax constructorDeclarationSyntax:
                    if (AnalyzeOrder(
                        members,
                        SortVirtualizationVisitor.Types.Constructor,
                        constructorDeclarationSyntax.FullSpan
                    ))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.ConstructorOrderRule1005,
                                constructorDeclarationSyntax.Identifier.GetLocation(),
                                messageArgs: new[] { constructorDeclarationSyntax.Identifier.ValueText }
                            )
                        );
                    }

                    return;
                case DelegateDeclarationSyntax delegateDeclarationSyntax:
                    AnalyzeOrderAndSort(
                        classes,
                        members,
                        context,
                        SortVirtualizationVisitor.Types.Delegate,
                        delegateDeclarationSyntax.Identifier,
                        delegateDeclarationSyntax.FullSpan,
                        DiagnosticDescriptors.DelegateOrderRule1011,
                        DiagnosticDescriptors.DelegateSortRule1010
                    );

                    return;
                case EventFieldDeclarationSyntax eventFieldDeclarationSyntax:
                    AnalyzeOrderAndSort(
                        classes,
                        members,
                        context,
                        SortVirtualizationVisitor.Types.EventField,
                        eventFieldDeclarationSyntax.Declaration.Variables[0].Identifier,
                        eventFieldDeclarationSyntax.FullSpan,
                        DiagnosticDescriptors.EventOrderRule1013,
                        DiagnosticDescriptors.EventSortRule1012
                    );

                    return;
                case EventDeclarationSyntax eventDeclarationSyntax:
                    AnalyzeOrderAndSort(
                        classes,
                        members,
                        context,
                        SortVirtualizationVisitor.Types.Event,
                        eventDeclarationSyntax.Identifier,
                        eventDeclarationSyntax.FullSpan,
                        DiagnosticDescriptors.EventOrderRule1013,
                        DiagnosticDescriptors.EventSortRule1012
                    );

                    return;
                case PropertyDeclarationSyntax propertyDeclarationSyntax:
                    AnalyzeOrderAndSort(
                        classes,
                        members,
                        context,
                        SortVirtualizationVisitor.Types.Property,
                        propertyDeclarationSyntax.Identifier,
                        propertyDeclarationSyntax.FullSpan,
                        DiagnosticDescriptors.PropertyOrderRule1007,
                        DiagnosticDescriptors.PropertySortRule1006
                    );

                    return;
                case MethodDeclarationSyntax methodDeclarationSyntax:
                    AnalyzeOrderAndSort(
                        classes,
                        members,
                        context,
                        SortVirtualizationVisitor.Types.Methods,
                        methodDeclarationSyntax.Identifier,
                        methodDeclarationSyntax.FullSpan,
                        DiagnosticDescriptors.MethodOrderRule1004,
                        DiagnosticDescriptors.MethodSortRule1003
                    );

                    return;
            }
        }

        private void AnalyzeOrderAndSort(
            ImmutableHashSet<KeyValuePair<TextSpan, ClassInformation>> classInformations,
            ILookup<SortVirtualizationVisitor.Types, TypesInformation> members,
            SyntaxNodeAnalysisContext context,
            SortVirtualizationVisitor.Types types,
            SyntaxToken syntaxIdentifier,
            TextSpan fullSpan,
            DiagnosticDescriptor orderRule,
            DiagnosticDescriptor sortRule
        )
        {
            if (AnalyzeOrder(
                members,
                types,
                fullSpan
            ))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        orderRule,
                        syntaxIdentifier.GetLocation(),
                        messageArgs: new[] { syntaxIdentifier.ValueText }
                    )
                );
            }

            if (AnalyzeSort(
                classInformations,
                members[types],
                fullSpan
            ))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        sortRule,
                        syntaxIdentifier.GetLocation(),
                        messageArgs: new[] { syntaxIdentifier.ValueText }
                    )
                );
            }
        }

        private static bool AnalyzeSort(
            ImmutableHashSet<KeyValuePair<TextSpan, ClassInformation>> classes,
            IEnumerable<TypesInformation> members,
            TextSpan fullSpan
        )
        {
            var classMemberGroup = classes.ToDictionary(
                x => x.Key,
                y => members.Where(x => x.FullSpan.IntersectsWith(y.Key)).ToList()
            );

            foreach (var classMembers in classMemberGroup)
            {
                var member = classMembers.Value.SingleOrDefault(x => x.FullSpan.Start == fullSpan.Start);
                if (member == null) continue;

                var currentIndexOf = classMembers.Value.IndexOf(member);

                var sortedList = classMembers.Value
                    .OrderBy(x => x.ClassName)
                    .ThenBy(x => x.ModifiersOrder)
                    .ThenBy(x => x.Name)
                    .ToList();
                var sortedIndexOf = sortedList.IndexOf(member);

                if (currentIndexOf != sortedIndexOf)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnalyzeOrder(
            ILookup<SortVirtualizationVisitor.Types, TypesInformation> members,
            SortVirtualizationVisitor.Types type,
            TextSpan span
        )
        {
            var aboveTypes = _types.Where(x => x < type);
            var belowTypes = _types.Where(x => x > type);

            var aboveList = aboveTypes.Where(members.Contains).SelectMany(x => members[x]).Select(x => x.FullSpan).ToImmutableHashSet();
            var belowList = belowTypes.Where(members.Contains).SelectMany(x => members[x]).Select(x => x.FullSpan).ToImmutableHashSet();

            if (aboveList.Any() == false && belowList.Any() == false)
            {
                return false;
            }

            var above = aboveList.Any()
                ? (Min: aboveList.Min(y => y.Start), Max: aboveList.Max(y => y.End))
                : (Min: int.MinValue, Max: int.MinValue);
            var below = belowList.Any()
                ? (Min: belowList.Min(y => y.Start), Max: belowList.Max(y => y.End))
                : (Min: int.MaxValue, Max: int.MaxValue);

            return above.Min >= span.Start || above.Max > span.Start ||
                   below.Min < span.End || below.Max <= span.End;
        }
    }
}