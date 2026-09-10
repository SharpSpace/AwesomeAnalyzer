using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnusedCodeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.Rule0012UnusedCode);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(
                AnalyzeSymbol,
                SymbolKind.Field,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Event,
                SymbolKind.NamedType
            );
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclarator, SyntaxKind.VariableDeclarator);
            context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            using (_ = new MeasureTime())
            {
                if (IsDisabledEditorConfig(
                    context,
                    DiagnosticDescriptors.Rule0012UnusedCode.Id,
                    context.Symbol.Locations.FirstOrDefault()?.SourceTree
                ))
                {
                    return;
                }

                if (!IsCandidateSymbol(context.Symbol))
                {
                    return;
                }

                if (IsUsedInContainingType(context.Symbol, context.Compilation, context.CancellationToken))
                {
                    return;
                }

                var location = context.Symbol.Locations.FirstOrDefault();
                if (location == null)
                {
                    return;
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Rule0012UnusedCode,
                        location,
                        context.Symbol.Name
                    )
                );
            }
        }

        private static void AnalyzeVariableDeclarator(SyntaxNodeAnalysisContext context)
        {
            using (_ = new MeasureTime())
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0012UnusedCode.Id))
                {
                    return;
                }

                var variableDeclarator = (VariableDeclaratorSyntax)context.Node;
                var declaration = variableDeclarator.Parent as VariableDeclarationSyntax;
                if (declaration == null)
                {
                    return;
                }

                if (!(declaration.Parent is LocalDeclarationStatementSyntax)
                    && !(declaration.Parent is ForStatementSyntax))
                {
                    return;
                }

                var symbol = context.SemanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken);
                if (symbol == null)
                {
                    return;
                }

                var scope = GetScopeForLocalVariable(variableDeclarator, declaration.Parent);
                if (scope == null)
                {
                    return;
                }

                if (HasUsageInNode(scope, context.SemanticModel, symbol, context.CancellationToken))
                {
                    return;
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Rule0012UnusedCode,
                        variableDeclarator.Identifier.GetLocation(),
                        variableDeclarator.Identifier.ValueText
                    )
                );
            }
        }

        private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
        {
            using (_ = new MeasureTime())
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0012UnusedCode.Id))
                {
                    return;
                }

                var localFunction = (LocalFunctionStatementSyntax)context.Node;
                var symbol = context.SemanticModel.GetDeclaredSymbol(localFunction, context.CancellationToken);
                if (symbol == null)
                {
                    return;
                }

                var scope = localFunction.Parent;
                if (scope == null)
                {
                    return;
                }

                if (HasUsageInNode(scope, context.SemanticModel, symbol, context.CancellationToken))
                {
                    return;
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Rule0012UnusedCode,
                        localFunction.Identifier.GetLocation(),
                        localFunction.Identifier.ValueText
                    )
                );
            }
        }

        private static bool IsCandidateSymbol(ISymbol symbol)
        {
            if (symbol.IsImplicitlyDeclared)
            {
                return false;
            }

            if (symbol.DeclaredAccessibility != Accessibility.Private)
            {
                return false;
            }

            if (symbol is IFieldSymbol fieldSymbol)
            {
                return fieldSymbol.AssociatedSymbol == null && !fieldSymbol.IsConst;
            }

            if (symbol is IMethodSymbol methodSymbol)
            {
                return methodSymbol.MethodKind == MethodKind.Ordinary
                    && methodSymbol.DeclaringSyntaxReferences.Any(x => x.GetSyntax() is MethodDeclarationSyntax)
                    && !methodSymbol.IsOverride
                    && !methodSymbol.IsAbstract;
            }

            if (symbol is IPropertySymbol propertySymbol)
            {
                return !propertySymbol.IsOverride && !propertySymbol.IsAbstract;
            }

            if (symbol is IEventSymbol eventSymbol)
            {
                return !eventSymbol.IsOverride && !eventSymbol.IsAbstract;
            }

            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol.ContainingType != null
                    && namedTypeSymbol.DeclaringSyntaxReferences.Any(
                        x => x.GetSyntax() is ClassDeclarationSyntax
                            || x.GetSyntax() is StructDeclarationSyntax
                            || x.GetSyntax() is RecordDeclarationSyntax
                    );
            }

            return false;
        }

        private static SyntaxNode GetScopeForLocalVariable(
            VariableDeclaratorSyntax variableDeclarator,
            SyntaxNode declarationParent)
        {
            if (declarationParent is ForStatementSyntax forStatement)
            {
                return forStatement;
            }

            var block = variableDeclarator.FirstAncestorOrSelf<BlockSyntax>();
            if (block != null)
            {
                return block;
            }

            return declarationParent;
        }

        private static bool IsUsedInContainingType(
            ISymbol symbol,
            Compilation compilation,
            System.Threading.CancellationToken cancellationToken)
        {
            if (symbol.ContainingType == null)
            {
                return true;
            }

            foreach (var declaration in symbol.ContainingType.DeclaringSyntaxReferences)
            {
                var syntax = declaration.GetSyntax(cancellationToken);
                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                if (HasUsageInNode(syntax, semanticModel, symbol, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUsageInNode(
            SyntaxNode scope,
            SemanticModel semanticModel,
            ISymbol symbol,
            System.Threading.CancellationToken cancellationToken)
        {
            var declarations = symbol.DeclaringSyntaxReferences
                .Select(x => x.GetSyntax(cancellationToken).Span)
                .ToImmutableArray();

            foreach (var syntaxNode in scope.DescendantNodes().OfType<SyntaxNode>())
            {
                if (!(syntaxNode is SimpleNameSyntax)
                    && !(syntaxNode is MemberAccessExpressionSyntax)
                    && !(syntaxNode is MemberBindingExpressionSyntax))
                {
                    continue;
                }

                var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode, cancellationToken);
                if (IsSymbolMatch(symbolInfo.Symbol, symbol)
                    && !declarations.Any(x => x.Contains(syntaxNode.SpanStart)))
                {
                    return true;
                }

                if (symbolInfo.CandidateSymbols.Any(x => IsSymbolMatch(x, symbol))
                    && !declarations.Any(x => x.Contains(syntaxNode.SpanStart)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDisabledEditorConfig(
            SymbolAnalysisContext context,
            string rule,
            SyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                return false;
            }

            var config = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            if (config.TryGetValue($"dotnet_diagnostic.{rule}.enabled", out var enabled))
            {
                if (enabled.Equals("true", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (config.TryGetValue($"dotnet_diagnostic.{rule}.severity", out var severity))
            {
                if (severity.Equals("none", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool IsSymbolMatch(ISymbol candidate, ISymbol target)
        {
            if (candidate == null)
            {
                return false;
            }

            return SymbolEqualityComparer.Default.Equals(
                candidate.OriginalDefinition,
                target.OriginalDefinition
            );
        }
    }
}
