using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AddAwaitAnalyzer : DiagnosticAnalyzer
    {
        private const string TextTask = "Task";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.AddAwaitRule0101,
            DiagnosticDescriptors.AddAsyncRule0102
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax invocationExpressionSyntax)) return;

            if (invocationExpressionSyntax.HasParent<AwaitExpressionSyntax>() != null) return;

            var typeSymbol = ModelExtensions.GetTypeInfo(context.SemanticModel, invocationExpressionSyntax);
            if (typeSymbol.Type?.Name != TextTask) return;

            if (invocationExpressionSyntax.HasParent<ConstructorDeclarationSyntax>() != null)
            {
                return;
            }

            var methodDeclarationSyntax = invocationExpressionSyntax.HasParent<MethodDeclarationSyntax>();
            if (methodDeclarationSyntax != null)
            {
                var typeInfo = ModelExtensions.GetTypeInfo(context.SemanticModel, methodDeclarationSyntax.ReturnType);
                if (typeInfo.Type?.Name == TextTask) return;
            }

            if (invocationExpressionSyntax.Parent is AssignmentExpressionSyntax) return;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AddAwaitRule0101,
                invocationExpressionSyntax.Expression.GetLocation(),
                messageArgs: invocationExpressionSyntax.Expression.ToString()
            ));
        }
    }
}