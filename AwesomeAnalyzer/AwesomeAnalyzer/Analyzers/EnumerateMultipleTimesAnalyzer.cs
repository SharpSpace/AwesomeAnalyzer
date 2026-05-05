using System.Collections.Generic;
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
                var containingBlock = localDeclaration.Parent;
                if (containingBlock == null) return;

                // Collect candidate variables: IEnumerable<T> locals with non-materialized initializers.
                var candidates = new Dictionary<ISymbol, VariableDeclaratorSyntax>(SymbolEqualityComparer.Default);
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

                    candidates[symbol] = variable;
                }

                if (candidates.Count == 0) return;

                // Single pass over the containing block: count enumeration usages per candidate symbol.
                var counts = new Dictionary<ISymbol, int>(SymbolEqualityComparer.Default);
                foreach (var sym in candidates.Keys) counts[sym] = 0;

                foreach (var node in containingBlock.DescendantNodes())
                {
                    if (!(node is IdentifierNameSyntax identifier)) continue;

                    var resolvedSymbol = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol;
                    if (resolvedSymbol == null || !counts.ContainsKey(resolvedSymbol)) continue;

                    if (IsEnumerationUsage(identifier, context.SemanticModel, context.CancellationToken))
                    {
                        counts[resolvedSymbol]++;
                    }
                }

                foreach (var pair in candidates)
                {
                    if (counts.TryGetValue(pair.Key, out var count) && count >= 2)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.Rule0010EnumerateMultipleTimes,
                                pair.Value.GetLocation(),
                                pair.Value.Identifier.ValueText
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

            // Check if the type itself is one of the materialized interfaces (e.g. cast to IList<T>).
            if (IsMatchingMaterializedInterface(namedType, iListType, iReadOnlyListType, iCollectionType, iReadOnlyCollectionType))
            {
                return true;
            }

            return namedType.AllInterfaces.Any(iface =>
                IsMatchingMaterializedInterface(iface, iListType, iReadOnlyListType, iCollectionType, iReadOnlyCollectionType)
            );
        }

        private static bool IsMatchingMaterializedInterface(
            INamedTypeSymbol type,
            INamedTypeSymbol iListType,
            INamedTypeSymbol iReadOnlyListType,
            INamedTypeSymbol iCollectionType,
            INamedTypeSymbol iReadOnlyCollectionType)
        {
            return
                (iListType != null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, iListType)) ||
                (iReadOnlyListType != null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, iReadOnlyListType)) ||
                (iCollectionType != null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, iCollectionType)) ||
                (iReadOnlyCollectionType != null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, iReadOnlyCollectionType));
        }

        private static bool IsEnumerationUsage(
            IdentifierNameSyntax identifier,
            SemanticModel model,
            CancellationToken cancellationToken)
        {
            // foreach (... in [wrapped] variable): walk up through parentheses and null-forgiving operators.
            SyntaxNode current = identifier;
            while (current.Parent is ParenthesizedExpressionSyntax ||
                   (current.Parent is PostfixUnaryExpressionSyntax postfix &&
                    postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression)))
            {
                current = current.Parent;
            }

            if (current.Parent is ForEachStatementSyntax forEach && forEach.Expression == current)
            {
                return true;
            }

            // variable.Method(...): only count invocations of methods in the System.Linq namespace.
            if (identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression == identifier &&
                memberAccess.Parent is InvocationExpressionSyntax invocation)
            {
                var methodSymbol = model.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
                if (methodSymbol != null)
                {
                    var containingType = methodSymbol.ContainingType;
                    return string.Equals(
                        containingType?.ContainingNamespace?.ToDisplayString(),
                        "System.Linq",
                        System.StringComparison.Ordinal
                    );
                }
            }

            return false;
        }
    }
}
