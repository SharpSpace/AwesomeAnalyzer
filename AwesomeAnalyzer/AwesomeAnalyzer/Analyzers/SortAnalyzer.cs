using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
            DiagnosticDescriptors.Rule1008EnumSort,
            DiagnosticDescriptors.Rule1009EnumOrder,
            DiagnosticDescriptors.Rule1001FieldSort,
            DiagnosticDescriptors.Rule1002FieldOrder,
            DiagnosticDescriptors.Rule1005ConstructorOrder,
            DiagnosticDescriptors.Rule1010DelegateSort,
            DiagnosticDescriptors.Rule1011DelegateOrder,
            DiagnosticDescriptors.Rule1012EventSort,
            DiagnosticDescriptors.Rule1013EventOrder,
            DiagnosticDescriptors.Rule1006PropertySort,
            DiagnosticDescriptors.Rule1007PropertyOrder,
            DiagnosticDescriptors.Rule1003MethodSort,
            DiagnosticDescriptors.Rule1004MethodOrder
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
                    DiagnosticDescriptors.Rule1009EnumOrder,
                    DiagnosticDescriptors.Rule1008EnumSort
                ),
                SyntaxKind.EnumDeclaration
            );
            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Field,
                    ((FieldDeclarationSyntax)x.Node).Declaration.Variables[0].Identifier,
                    DiagnosticDescriptors.Rule1002FieldOrder,
                    DiagnosticDescriptors.Rule1001FieldSort
                ),
                SyntaxKind.FieldDeclaration
            );

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Delegate,
                    ((DelegateDeclarationSyntax)x.Node).Identifier,
                    DiagnosticDescriptors.Rule1011DelegateOrder,
                    DiagnosticDescriptors.Rule1010DelegateSort
                ),
                SyntaxKind.DelegateDeclaration
            );

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.EventField,
                    ((EventFieldDeclarationSyntax)x.Node).Declaration.Variables[0].Identifier,
                    DiagnosticDescriptors.Rule1013EventOrder,
                    DiagnosticDescriptors.Rule1012EventSort
                ),
                SyntaxKind.EventFieldDeclaration
            );

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Property,
                    ((PropertyDeclarationSyntax)x.Node).Identifier,
                    DiagnosticDescriptors.Rule1007PropertyOrder,
                    DiagnosticDescriptors.Rule1006PropertySort
                ),
                SyntaxKind.PropertyDeclaration
            );

            context.RegisterSyntaxNodeAction(
                x => Analyze(
                    x,
                    SortVirtualizationVisitor.Types.Methods,
                    ((MethodDeclarationSyntax)x.Node).Identifier,
                    DiagnosticDescriptors.Rule1004MethodOrder,
                    DiagnosticDescriptors.Rule1003MethodSort
                ),
                SyntaxKind.MethodDeclaration
            );

            context.RegisterSyntaxNodeAction(AnalyzeConstructorNode, SyntaxKind.ConstructorDeclaration);
        }

        private static bool AnalyzeSort(
            ImmutableHashSet<KeyValuePair<TextSpan, ClassInformation>> classes,
            IReadOnlyCollection<TypesInformation> members,
            TextSpan fullSpan,
            CancellationToken token
        )
        {
            using (var _ = new MeasureTime())
            {
                if (token.IsCancellationRequested) return false;
                var member = members.SingleOrDefault(x => x.FullSpan.Start == fullSpan.Start);
                if (member == null) return false;
                if (string.IsNullOrWhiteSpace(member.ClassName)) return false;
                if (member.ClassName.Count(x => x == '.') >= 1) return false;

                var classMembers = members.Where(x => x.ClassName == member.ClassName).ToList();

                foreach (var item in classes)
                {
                    using (var __ = new MeasureTime(true, $"{nameof(AnalyzeSort)}->foreach"))
                    {
                        if (token.IsCancellationRequested) return false;

                        var classMemberList = classMembers
                            .Where(x => 
                                x.FullSpan.IntersectsWith(item.Value.FullSpan)
                            )
                            .ToList();
                        var currentIndexOf = classMemberList.IndexOf(member);

                        var sortedList = classMemberList
                            .OrderBy(x => x.Order)
                            .ThenBy(x => PadNumbers(x.Name))
                            .ToImmutableArray();
                        var sortedIndexOf = sortedList.IndexOf(member);

                        if (currentIndexOf != sortedIndexOf) return true;
                    }
                }

                return false;
            }
        }

        private static bool AnalyzeOrder(
            ILookup<SortVirtualizationVisitor.Types, TypesInformation> members,
            SortVirtualizationVisitor.Types type,
            TextSpan span,
            CancellationToken token
        )
        {
            using (var _ = new MeasureTime())
            {
                if (token.IsCancellationRequested) return false;
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
        }

        private void AnalyzeConstructorNode(SyntaxNodeAnalysisContext context)
        {
            var sortVirtualizationVisitor = new SortVirtualizationVisitor(context.CancellationToken);
            sortVirtualizationVisitor.Visit(context.Node.Parent);

            var members = sortVirtualizationVisitor.Members
                .SelectMany(
                    x =>
                        x.Value
                            .Where(y => string.IsNullOrWhiteSpace(y.ClassName) == false)
                            .Select(y => new { x.Key, y })
                )
                .ToLookup(x => x.Key, x => x.y);

            var constructorDeclarationSyntax = (ConstructorDeclarationSyntax)context.Node;

            if (AnalyzeOrder(
                    members,
                    SortVirtualizationVisitor.Types.Constructor,
                    constructorDeclarationSyntax.FullSpan,
                    context.CancellationToken
                ))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.Rule1005ConstructorOrder,
                    constructorDeclarationSyntax.Identifier.GetLocation(),
                    messageArgs: new object[] { constructorDeclarationSyntax.Identifier.ValueText }
                );
                context.ReportDiagnostic(
                    diagnostic
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
            using (var _ = new MeasureTime())
            {
                var sortVirtualizationVisitor = new SortVirtualizationVisitor(context.CancellationToken);

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
                    .SelectMany(
                        x =>
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
            if (context.IsDisabledEditorConfig(orderRule.Id) == false &&
                AnalyzeOrder(
                    members,
                    types,
                    fullSpan,
                    context.CancellationToken
                )
               )
            {
                var diagnostic = Diagnostic.Create(
                    orderRule,
                    syntaxIdentifier.GetLocation(),
                    messageArgs: new object[] { syntaxIdentifier.ValueText }
                );
                context.ReportDiagnostic(
                    diagnostic
                );
            }
            else if (
                context.IsDisabledEditorConfig(sortRule.Id) == false &&
                AnalyzeSort(
                    classInformations,
                    members[types].ToList(),
                    fullSpan,
                    context.CancellationToken
                )
            )
            {
                var diagnostic = Diagnostic.Create(
                    sortRule,
                    syntaxIdentifier.GetLocation(),
                    messageArgs: new object[] { syntaxIdentifier.ValueText }
                );
                context.ReportDiagnostic(diagnostic);
            }
        }

        public static string PadNumbers(string input) => Regex.Replace(
            input,
            "[0-9]+",
            match => match.Value.PadLeft(10, '0'),
            RegexOptions.Compiled
        );
    }
}