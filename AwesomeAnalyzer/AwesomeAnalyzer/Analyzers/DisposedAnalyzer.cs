using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DisposedAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.DisposedRule0004
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ObjectCreationExpressionSyntax objectCreationExpressionSyntax)) return;
            if (!(objectCreationExpressionSyntax.Parent is EqualsValueClauseSyntax equalsValueClauseSyntax)) return;
            if (!(equalsValueClauseSyntax.Parent is VariableDeclaratorSyntax variableDeclaratorSyntax)) return;
            if (!(variableDeclaratorSyntax.Parent is VariableDeclarationSyntax variableDeclarationSyntax)) return;

            var localDeclarationStatementSyntax = variableDeclarationSyntax.Parent as LocalDeclarationStatementSyntax;
            if (localDeclarationStatementSyntax != null &&
                localDeclarationStatementSyntax.UsingKeyword.ValueText == "using"
            ) return;

            if (!(localDeclarationStatementSyntax?.Parent is BlockSyntax blockSyntax)) return;

            var expressionStatementSyntaxes = blockSyntax.Statements.OfType<ExpressionStatementSyntax>();
            var expression = $"{variableDeclaratorSyntax.Identifier.ValueText}.Dispose()";
            if (expressionStatementSyntaxes.Any(x => x.Expression.ToString() == expression)) return;

            var typeSymbol = context.SemanticModel.GetTypeInfo(objectCreationExpressionSyntax).Type;
            if (typeSymbol == null) return;

            var interfaces = typeSymbol.AllInterfaces;
            if (interfaces.Any(x => x.Name == "IDisposable") == false) return;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DisposedRule0004,
                variableDeclarationSyntax.GetLocation(),
                messageArgs: variableDeclarationSyntax.ToString()
            ));
        }
    }
}