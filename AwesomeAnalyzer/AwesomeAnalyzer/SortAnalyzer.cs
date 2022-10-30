using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor FieldSortRule = new DiagnosticDescriptor(
            FieldSortDiagnosticId,
            "Field needs to be sorted alphabetically",
            "{0}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Sorts fields alphabetically."
        );

        private static readonly DiagnosticDescriptor FieldOrderRule = new DiagnosticDescriptor(
            FieldOrderDiagnosticId,
            "Field needs to be in correct order",
            "{0}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Sorts fields in correct order."
        );

        private static readonly DiagnosticDescriptor MethodSortRule = new DiagnosticDescriptor(
            MethodSortDiagnosticId,
            "Method needs to be sorted alphabetically",
            "{0}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Sorts methods alphabetically."
        );

        private static readonly DiagnosticDescriptor MethodOrderRule = new DiagnosticDescriptor(
            MethodOrderDiagnosticId,
            "Method needs to be in correct order",
            "{0}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Sorts methods in correct order."
        );

        private static readonly DiagnosticDescriptor ConstructorOrderRule = new DiagnosticDescriptor(
            ConstructorOrderDiagnosticId,
            "Constructor needs to be in correct order",
            "{0}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Sorts constructor in correct order."
        );

        private Dictionary<SortVirtualizationVisitor.Types, List<MethodInformation>> _members;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            FieldSortRule,
            FieldOrderRule,
            MethodSortRule, 
            MethodOrderRule,
            ConstructorOrderRule
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(this.AnalyzeNode, SyntaxKind.EnumDeclaration);
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
                case MethodDeclarationSyntax methodDeclarationSyntax:
                    AnalyzeOrder(
                        context,
                        this._members,
                        (enumMax, fieldMax, constructorMax, methodMin) =>
                            (enumMax != null && methodDeclarationSyntax.SpanStart <= enumMax) ||
                            (fieldMax != null && methodDeclarationSyntax.SpanStart <= fieldMax) ||
                            (methodMin != null && methodDeclarationSyntax.SpanStart <= constructorMax),
                        MethodOrderRule,
                        new[] { methodDeclarationSyntax.Identifier.ValueText },
                        methodDeclarationSyntax.WithoutTrailingTrivia().GetLocation()
                    );

                    AnalyzeSort(
                        context,
                        this._members[SortVirtualizationVisitor.Types.Methods],
                        methodDeclarationSyntax.Identifier.ValueText,
                        MethodSortRule,
                        new[] { methodDeclarationSyntax.Identifier.ValueText },
                        Location.Create(
                            context.SemanticModel.SyntaxTree,
                            new Microsoft.CodeAnalysis.Text.TextSpan(
                                methodDeclarationSyntax.Span.Start,
                                methodDeclarationSyntax.Identifier.Span.End - methodDeclarationSyntax.Span.Start
                            )
                        )
                    );

                    return;
                case FieldDeclarationSyntax fieldDeclarationSyntax:
                    AnalyzeOrder(
                        context,
                        this._members,
                        (enumMax, fieldMax, constructorMax, methodMin) =>
                            (enumMax != null && fieldDeclarationSyntax.SpanStart <= enumMax) ||
                            (constructorMax != null && fieldDeclarationSyntax.SpanStart >= constructorMax) ||
                            (methodMin != null && fieldDeclarationSyntax.SpanStart >= methodMin),
                        FieldOrderRule,
                        new[] { fieldDeclarationSyntax.Declaration.ToFullString() },
                        fieldDeclarationSyntax.GetLocation()
                    );

                    AnalyzeSort(
                        context,
                        this._members[SortVirtualizationVisitor.Types.Field],
                        fieldDeclarationSyntax.Declaration.Variables.ToFullString(),
                        FieldSortRule,
                        new[] { fieldDeclarationSyntax.Declaration.ToFullString() },
                        fieldDeclarationSyntax.GetLocation()
                    );

                    return;
                case ConstructorDeclarationSyntax constructorDeclarationSyntax:
                    AnalyzeOrder(
                        context,
                        this._members,
                        (enumMax, fieldMax, constructorMax, methodMin) =>
                            (enumMax != null && constructorDeclarationSyntax.SpanStart <= enumMax) ||
                            (fieldMax != null && constructorDeclarationSyntax.SpanStart <= fieldMax) ||
                            (constructorMax != null && constructorDeclarationSyntax.SpanStart >= methodMin),
                        ConstructorOrderRule,
                        new[] { constructorDeclarationSyntax.Identifier.ValueText },
                        context.Node.GetLocation());

                    return;
                case EnumDeclarationSyntax enumDeclarationSyntax:
                    AnalyzeOrder(
                        context,
                        this._members,
                        (enumMax, fieldMax, constructorMax, methodMin) =>
                            (fieldMax != null && enumDeclarationSyntax.SpanStart >= fieldMax) ||
                            (constructorMax != null && enumDeclarationSyntax.SpanStart >= constructorMax) ||
                            (methodMin != null && enumDeclarationSyntax.SpanStart >= methodMin),
                        MethodOrderRule,
                        new []{ enumDeclarationSyntax.Identifier.ValueText },
                        enumDeclarationSyntax.Identifier.GetLocation());

                    AnalyzeSort(
                        context,
                        this._members[SortVirtualizationVisitor.Types.Enum],
                        enumDeclarationSyntax.Identifier.ValueText,
                        MethodSortRule,
                        new[] { enumDeclarationSyntax.Identifier.ValueText },
                        enumDeclarationSyntax.Identifier.GetLocation());
                    break;
            }
        }

        private static void AnalyzeSort(
            SyntaxNodeAnalysisContext context,
            IList<MethodInformation> members,
            string name,
            DiagnosticDescriptor rule,
            object[] messageArgs,
            Location location
        )
        {
            var member = members.Single(x => x.Name == name);
            var currentIndexOf = members.IndexOf(member);

            var sortedList = members.OrderBy(x => x.ModifiersOrder).ThenBy(x => x.Name).ToList();
            var sortedIndexOf = sortedList.IndexOf(member);
            if (currentIndexOf != sortedIndexOf)
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, location, messageArgs: messageArgs));
            }
        }

        private static void AnalyzeOrder(
            SyntaxNodeAnalysisContext context,
            Dictionary<SortVirtualizationVisitor.Types, List<MethodInformation>> members,
            Func<int?, int?, int?, int?, bool> func,
            DiagnosticDescriptor rule,
            object[] messageArgs,
            Location location
        )
        {
            var enumMax = members.ContainsKey(SortVirtualizationVisitor.Types.Enum) ? members[SortVirtualizationVisitor.Types.Enum].Max(x => x.FullSpan.End) : (int?)null;
            var fieldMax = members.ContainsKey(SortVirtualizationVisitor.Types.Field) ? members[SortVirtualizationVisitor.Types.Field].Max(x => x.FullSpan.End) : (int?)null;
            var constructorMax = members.ContainsKey(SortVirtualizationVisitor.Types.Constructor) ? members[SortVirtualizationVisitor.Types.Constructor].Max(x => x.FullSpan.End) : (int?)null;
            var methodMin = members.ContainsKey(SortVirtualizationVisitor.Types.Methods) ? members[SortVirtualizationVisitor.Types.Methods].Min(x => x.FullSpan.Start) : (int?)null;

            if (func(enumMax, fieldMax, constructorMax, methodMin))
            {
                context.ReportDiagnostic(Diagnostic.Create(rule, location, messageArgs: messageArgs));
            }
        }
    }
}