using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SortAnalyzer : DiagnosticAnalyzer
    {
        public const string FieldSortDiagnosticId = "JJ1001";
        public const string FieldOrderDiagnosticId = "JJ1002";
        public const string MethodSortDiagnosticId = "JJ1003";
        public const string MethodOrderDiagnosticId = "JJ1004";
        public const string ConstructorOrderDiagnosticId = "JJ1005";
        public const string PropertySortDiagnosticId = "JJ1006";
        public const string PropertyOrderDiagnosticId = "JJ1007";
        private const string Category = "Order";

        private static readonly DiagnosticDescriptor FieldSortRule1001 = new DiagnosticDescriptor(
            FieldSortDiagnosticId,
            "Field needs to be sorted alphabetically",
            "Sort field {0}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sorts fields alphabetically."
        );

        private static readonly DiagnosticDescriptor FieldOrderRule1002 = new DiagnosticDescriptor(
            FieldOrderDiagnosticId,
            "Field needs to be in correct order",
            "Order field {0}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order fields in correct order."
        );

        private static readonly DiagnosticDescriptor ConstructorOrderRule1005 = new DiagnosticDescriptor(
            ConstructorOrderDiagnosticId,
            "Constructor needs to be in correct order",
            "Order constructor {0}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order constructor in correct order."
        );

        private static readonly DiagnosticDescriptor PropertySortRule1006 = new DiagnosticDescriptor(
            PropertySortDiagnosticId,
            "Property needs to be sorted alphabetically",
            "Sort property {0}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sorts properties alphabetically."
        );

        private static readonly DiagnosticDescriptor PropertyOrderRule1007 = new DiagnosticDescriptor(
            PropertyOrderDiagnosticId,
            "Property needs to be in correct order",
            "Order property {0}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order properties in correct order."
        );

        private static readonly DiagnosticDescriptor MethodSortRule1003 = new DiagnosticDescriptor(
            MethodSortDiagnosticId,
            "Method needs to be sorted alphabetically",
            "Sort method {0}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sorts methods alphabetically."
        );

        private static readonly DiagnosticDescriptor MethodOrderRule1004 = new DiagnosticDescriptor(
            MethodOrderDiagnosticId,
            "Method needs to be in correct order",
            "Order method {0}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order methods in correct order."
        );

        private Dictionary<SortVirtualizationVisitor.Types, List<TypesInformation>> _members;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            FieldSortRule1001,
            FieldOrderRule1002,
            ConstructorOrderRule1005,
            PropertySortRule1006,
            PropertyOrderRule1007,
            MethodSortRule1003, 
            MethodOrderRule1004
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.EnumDeclaration);
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.PropertyDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            //if (_members == null)
            {
                var sortVirtualizationVisitor = new SortVirtualizationVisitor();
                sortVirtualizationVisitor.Visit(context.SemanticModel.SyntaxTree.GetRoot());
                _members = sortVirtualizationVisitor.Members;
            }

            switch (context.Node)
            {
                case EnumDeclarationSyntax enumDeclarationSyntax:
                    if (AnalyzeOrder(
                            this._members,
                            SortVirtualizationVisitor.Types.Enum,
                            enumDeclarationSyntax.FullSpan
                    ))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            MethodOrderRule1004,
                            enumDeclarationSyntax.Identifier.GetLocation(),
                            messageArgs: new[] { enumDeclarationSyntax.Identifier.ValueText }
                        ));
                    }

                    if (AnalyzeSort(
                            this._members[SortVirtualizationVisitor.Types.Enum],
                        enumDeclarationSyntax.FullSpan
                    ))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            MethodSortRule1003,
                            enumDeclarationSyntax.Identifier.GetLocation(),
                            messageArgs: new[] { enumDeclarationSyntax.Identifier.ValueText }
                        ));
                    }

                    break;
                case FieldDeclarationSyntax fieldDeclarationSyntax:
                    if (AnalyzeOrder(
                        this._members,
                        SortVirtualizationVisitor.Types.Field,
                        fieldDeclarationSyntax.FullSpan
                    ))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            FieldOrderRule1002,
                            fieldDeclarationSyntax.Declaration.Variables[0].Identifier.GetLocation(),
                            messageArgs: new[] { fieldDeclarationSyntax.Declaration.Variables[0].Identifier.ToFullString() }
                        ));
                    }

                    if (AnalyzeSort(
                            this._members[SortVirtualizationVisitor.Types.Field],
                        fieldDeclarationSyntax.FullSpan
                    ))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            FieldSortRule1001,
                            fieldDeclarationSyntax.Declaration.Variables[0].Identifier.GetLocation(),
                            messageArgs: new[] { fieldDeclarationSyntax.Declaration.Variables[0].Identifier.ToFullString() }
                        ));
                    }

                    return;
                case ConstructorDeclarationSyntax constructorDeclarationSyntax:
                    if (AnalyzeOrder(
                            this._members,
                            SortVirtualizationVisitor.Types.Constructor,
                            constructorDeclarationSyntax.FullSpan
                        ))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            ConstructorOrderRule1005,
                            constructorDeclarationSyntax.Identifier.GetLocation(),
                            messageArgs: new[] { constructorDeclarationSyntax.Identifier.ValueText }
                        ));
                    }

                    return;
                case PropertyDeclarationSyntax propertyDeclarationSyntax:
                    if (AnalyzeOrder(
                            this._members,
                            SortVirtualizationVisitor.Types.Property,
                            propertyDeclarationSyntax.FullSpan
                        ))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            PropertyOrderRule1007,
                            propertyDeclarationSyntax.Identifier.GetLocation(),
                            messageArgs: new[] { propertyDeclarationSyntax.Identifier.ValueText }
                        ));
                    }

                    if (AnalyzeSort(
                            this._members[SortVirtualizationVisitor.Types.Property],
                            propertyDeclarationSyntax.FullSpan
                        ))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            PropertySortRule1006,
                            propertyDeclarationSyntax.Identifier.GetLocation(), 
                            messageArgs: new[] { propertyDeclarationSyntax.Identifier.ValueText }
                        ));
                    }

                    return;
                case MethodDeclarationSyntax methodDeclarationSyntax:
                    if (AnalyzeOrder(
                            this._members,
                            SortVirtualizationVisitor.Types.Methods,
                            methodDeclarationSyntax.FullSpan
                        ))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            MethodOrderRule1004,
                            methodDeclarationSyntax.Identifier.GetLocation(),
                            messageArgs: new[] { methodDeclarationSyntax.Identifier.ValueText }
                        ));
                    }

                    if (AnalyzeSort(
                            this._members[SortVirtualizationVisitor.Types.Methods],
                        methodDeclarationSyntax.FullSpan
                    ))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            MethodSortRule1003,
                            methodDeclarationSyntax.Identifier.GetLocation(),
                            messageArgs: new[] { methodDeclarationSyntax.Identifier.ValueText }
                        ));
                    }

                    return;
            }
        }

        private static bool AnalyzeSort(
            IList<TypesInformation> members,
            TextSpan fullSpan
        )
        {
            var member = members.Single(x => x.FullSpan.Start == fullSpan.Start);
            var currentIndexOf = members.IndexOf(member);

            var sortedList = members.OrderBy(x => x.ModifiersOrder).ThenBy(x => x.Name).ToList();
            var sortedIndexOf = sortedList.IndexOf(member);
            return currentIndexOf != sortedIndexOf;
        }

        private static bool AnalyzeOrder(
            Dictionary<SortVirtualizationVisitor.Types, List<TypesInformation>> members,
            SortVirtualizationVisitor.Types type,
            TextSpan span
        )
        {
            var types = Enum.GetValues(typeof(SortVirtualizationVisitor.Types)).OfType<SortVirtualizationVisitor.Types>().ToList();

            var aboveTypes = types.Where(x => x < type);
            var belowTypes = types.Where(x => x > type);

            var aboveList = aboveTypes.Where(members.ContainsKey).SelectMany(x => members[x]).Select(x => x.FullSpan).ToList();
            var belowList = belowTypes.Where(members.ContainsKey).SelectMany(x => members[x]).Select(x => x.FullSpan).ToList();

            if (aboveList.Any() == false && belowList.Any() == false)
            {
                return false;
            }

            var above = aboveList.Any() ? (Min: aboveList.Min(y => y.Start), Max: aboveList.Max(y => y.End)) : (Min: int.MinValue, Max: int.MinValue);
            var below = belowList.Any() ? (Min: belowList.Min(y => y.Start), Max: belowList.Max(y => y.End)) : (Min: int.MaxValue, Max: int.MaxValue);

            if (above.Min < span.Start && above.Max <= span.Start && 
                below.Min >= span.End && below.Max > span.End
            )
            {
                return false;
            }

            return true;
        }
    }
}