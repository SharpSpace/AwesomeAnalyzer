using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EnumerateMultipleTimesAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0010EnumerateMultipleTimes
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            using (_ = new MeasureTime())
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0010EnumerateMultipleTimes.Id))
                {
                    return;
                }

                var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;

                foreach (var variable in localDeclaration.Declaration.Variables)
                {
                    if (variable.Initializer == null) continue;

                    var symbol = ModelExtensions.GetDeclaredSymbol(
                        context.SemanticModel,
                        variable,
                        context.CancellationToken
                    ) as ILocalSymbol;
                    if (symbol == null) continue;

                    if (!IsEnumerableType(symbol.Type, context.SemanticModel.Compilation)) continue;

                    var initializerTypeInfo = context.SemanticModel.GetTypeInfo(
                        variable.Initializer.Value,
                        context.CancellationToken
                    );
                    if (IsMaterializedCollection(initializerTypeInfo.Type, context.SemanticModel.Compilation)) continue;

                    var containingBlock = localDeclaration.Parent;
                    if (containingBlock == null) continue;

                    var enumerationCount = CountEnumerationUsages(
                        containingBlock,
                        symbol,
                        context.SemanticModel,
                        context.CancellationToken
                    );

                    if (enumerationCount >= 2)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.Rule0010EnumerateMultipleTimes,
                                variable.GetLocation(),
                                variable.Identifier.ValueText
                            )
                        );
                    }
                }
            }
        }

        private static bool IsEnumerableType(ITypeSymbol type, Compilation compilation)
        {
            if (!(type is INamedTypeSymbol namedType)) return false;

            var genericEnumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
            if (genericEnumerable == null) return false;

            return SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, genericEnumerable);
        }

        private static bool IsMaterializedCollection(ITypeSymbol type, Compilation compilation)
        {
            if (type == null) return false;
            if (type is IArrayTypeSymbol) return true;
            if (!(type is INamedTypeSymbol namedType)) return false;

            var iListType = compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1");
            var iReadOnlyListType = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1");
            var iCollectionType = compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
            var iReadOnlyCollectionType = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1");

            return namedType.AllInterfaces.Any(iface =>
                (iListType != null && SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iListType)) ||
                (iReadOnlyListType != null && SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iReadOnlyListType)) ||
                (iCollectionType != null && SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iCollectionType)) ||
                (iReadOnlyCollectionType != null && SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iReadOnlyCollectionType))
            );
        }

        private static int CountEnumerationUsages(
            SyntaxNode containingBlock,
            ISymbol variableSymbol,
            SemanticModel model,
            CancellationToken cancellationToken)
        {
            var count = 0;

            foreach (var node in containingBlock.DescendantNodes())
            {
                if (!(node is IdentifierNameSyntax identifier)) continue;

                var resolvedSymbol = model.GetSymbolInfo(identifier, cancellationToken).Symbol;
                if (!SymbolEqualityComparer.Default.Equals(resolvedSymbol, variableSymbol)) continue;

                if (IsEnumerationUsage(identifier))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsEnumerationUsage(IdentifierNameSyntax identifier)
        {
            // foreach (... in variable)
            if (identifier.Parent is ForEachStatementSyntax forEach && forEach.Expression == identifier)
            {
                return true;
            }

            // variable.Method(...)
            if (identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression == identifier &&
                memberAccess.Parent is InvocationExpressionSyntax)
            {
                return true;
            }

            return false;
        }
    }
}
