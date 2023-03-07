using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
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

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Enum,
                    ((EnumDeclarationSyntax)x.Node).Identifier,
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
                    DiagnosticDescriptors.MethodOrderRule1004,
                    DiagnosticDescriptors.MethodSortRule1003
                ),
                SyntaxKind.MethodDeclaration
            );

            context.RegisterSyntaxNodeAction(AnalyzeConstructorNode, SyntaxKind.ConstructorDeclaration);
        }

        private static bool AnalyzeSort(
            ImmutableHashSet<KeyValuePair<TextSpan, ClassInformation>> classes,
            IEnumerable<TypesInformation> members,
            TextSpan fullSpan
        )
        {
            var classMemberGroup = classes
                .ToDictionary(
                    x => x.Key,
                    y => members.Where(x => x.FullSpan.IntersectsWith(y.Key)).ToList()
                );

            foreach (var classMembers in classMemberGroup)
            {
                var member = classMembers.Value.SingleOrDefault(x => x.FullSpan.Start == fullSpan.Start);
                if (member == null) continue;
                if (string.IsNullOrWhiteSpace(member.ClassName)) continue;
                if (member.ClassName.Count(x => x == '.') >= 1) return false;

                var classMemberList = classMembers.Value.Where(x => x.ClassName == member.ClassName).ToList();
                var currentIndexOf = classMemberList.IndexOf(member);

                var sortedList = classMemberList
                    .OrderBy(x => x.Order)
                    .ThenBy(x => PadNumbers(x.Name))
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
            if (members.Contains(type) == false)
            {
                return false;
            }

            var member = members[type].Single(x => x.FullSpan == span);
            if (string.IsNullOrWhiteSpace(member.ClassName)) return false;
            if (member.ClassName.Count(z => z == '.') >= 1) return false;

            var memberIndex = (member.FullSpan, member.Order);
            var classMemberList = members.SelectMany(x => x).Where(x => x.ClassName == member.ClassName).ToList();
            var membersIndex = classMemberList.Select(y => (y.FullSpan, y.Order)).ToList();

            if (membersIndex.Any(x => x.Order > memberIndex.Order && x.FullSpan.Start < memberIndex.FullSpan.Start))
            {
                return true;
            }

            if (membersIndex.Any(x => x.Order < memberIndex.Order && x.FullSpan.Start > memberIndex.FullSpan.Start))
            {
                return true;
            }

            return false;
        }

        private void AnalyzeConstructorNode(SyntaxNodeAnalysisContext context)
        {
            var sortVirtualizationVisitor = new SortVirtualizationVisitor();
            sortVirtualizationVisitor.Visit(context.Node.Parent);

            var members = sortVirtualizationVisitor.Members
                .SelectMany(x =>
                    x.Value
                        .Where(y => string.IsNullOrWhiteSpace(y.ClassName) == false)
                        .Select(y => new { x.Key, y })
                )
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
                        messageArgs: new object[] { constructorDeclarationSyntax.Identifier.ValueText }
                    )
                );
            }
        }

        private void Analyze(
            SyntaxNodeAnalysisContext context,
            SortVirtualizationVisitor.Types types,
            SyntaxToken syntaxToken,
            DiagnosticDescriptor orderRule,
            DiagnosticDescriptor sortRule
        )
        {
            var sortVirtualizationVisitor = new SortVirtualizationVisitor();

            var parents = context.Node.FindAllParent(
                typeof(CompilationUnitSyntax),
                typeof(NamespaceDeclarationSyntax),
                typeof(FileScopedNamespaceDeclarationSyntax),
                typeof(RecordDeclarationSyntax),
                typeof(ClassDeclarationSyntax),
                typeof(InterfaceDeclarationSyntax),
                typeof(StructDeclarationSyntax)
            );

            var firstParent = parents.OrderBy(x => x.SpanStart).First();
            sortVirtualizationVisitor.Visit(firstParent);
            var members = sortVirtualizationVisitor.Members
                .SelectMany(x =>
                    x.Value
                        .Select(y => new { x.Key, y })
                )
                .ToLookup(x => x.Key, x => x.y);
            var classes = sortVirtualizationVisitor.Classes.ToImmutableHashSet();

            AnalyzeOrderAndSort(
                classes,
                members,
                context,
                types,
                syntaxToken,
                context.Node.FullSpan,
                orderRule,
                sortRule
            );
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
                )
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        orderRule,
                        syntaxIdentifier.GetLocation(),
                        messageArgs: new object[] { syntaxIdentifier.ValueText }
                    )
                );
            }
            else if (AnalyzeSort(
                     classInformations,
                     members[types],
                     fullSpan
                 )
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        sortRule,
                        syntaxIdentifier.GetLocation(),
                        messageArgs: new object[] { syntaxIdentifier.ValueText }
                    )
                );
            }
        }

        public static string PadNumbers(string input) => Regex.Replace(input, "[0-9]+", match => match.Value.PadLeft(10, '0'), RegexOptions.Compiled);
    }
}