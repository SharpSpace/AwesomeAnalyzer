using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NullListAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.NullListRule0002
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MethodDeclarationSyntax methodDeclarationSyntax)) return;

            var typeName = string.Empty;
            if (methodDeclarationSyntax.ReturnType is GenericNameSyntax genericNameSyntax)
            {
                typeName = genericNameSyntax.Identifier.ValueText;
            }
            else if (methodDeclarationSyntax.ReturnType is ArrayTypeSyntax)
            {
                typeName = "Array";
            }

            if (typeName != nameof(IEnumerable) && typeName != "List" && typeName != "Array") return;

            var children = (methodDeclarationSyntax
                        .FindToken(methodDeclarationSyntax.SpanStart)
                        .Parent?
                        .DescendantNodesAndSelf() ?? Enumerable.Empty<SyntaxNode>()
                ).ToList();
            var literalExpressionSyntaxes = children.OfType<LiteralExpressionSyntax>();
            var invocationExpressionSyntaxes = children.OfType<InvocationExpressionSyntax>();

            if (methodDeclarationSyntax.ExpressionBody != null)
            {
                //var returnType = context.SemanticModel.GetTypeInfo(methodDeclarationSyntax.ExpressionBody);


                if (methodDeclarationSyntax.ExpressionBody.Expression.ParentTrivia.Token.Value != null)
                {
                    var typeSymbol = context.SemanticModel.GetTypeInfo(methodDeclarationSyntax.ExpressionBody.Expression);
                    if (typeName == typeSymbol.Type?.Name) return;
                }

                if (methodDeclarationSyntax.ExpressionBody.Expression is LiteralExpressionSyntax literalExpressionSyntax)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.NullListRule0002,
                        literalExpressionSyntax.GetLocation()
                    ));
                }

                return;
            }
            else if (methodDeclarationSyntax.Body != null)
            {
                foreach (var returnStatementSyntax in methodDeclarationSyntax.Body.Statements.OfType<ReturnStatementSyntax>())
                {
                    if (returnStatementSyntax.Expression == null) continue;

                    var typeSymbol = context.SemanticModel.GetTypeInfo(returnStatementSyntax);
                    if (typeName == typeSymbol.Type?.Name) return;

                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.NullListRule0002,
                        returnStatementSyntax.Expression.GetLocation()
                    ));
                }
            }
            else
            {
                
            }

            //context.ReportDiagnostic(Diagnostic.Create(
            //    DiagnosticDescriptors.NullListRule0002,
            //    methodDeclarationSyntax.GetLocation()
            //));
        }
    }
}