using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DontReturnNullAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.DontReturnNullRule0007);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ReturnStatement);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var returnStatementSyntax = (ReturnStatementSyntax)context.Node;

            if (!(returnStatementSyntax.Expression is LiteralExpressionSyntax literalExpressionSyntax))
            {
                return;
            }

            if (literalExpressionSyntax.Token.ValueText != "null")
            {
                return;
            }

            var methodDeclarationSyntax = returnStatementSyntax.HasParent<MethodDeclarationSyntax>();
            if (!(methodDeclarationSyntax.ReturnType is ArrayTypeSyntax))
            {
                if (methodDeclarationSyntax.ReturnType is GenericNameSyntax genericNameSyntax)
                {
                    if (genericNameSyntax.Identifier.ValueText == "Task")
                    {
                        return;
                    }

                    if (genericNameSyntax.Identifier.ValueText == "ValueTask")
                    {
                        return;
                    }
                }
                else
                {
                    if (!(methodDeclarationSyntax.ReturnType is IdentifierNameSyntax identifierNameSyntax))
                    {
                        return;
                    }

                    if (identifierNameSyntax.Identifier.ValueText != "ArrayList")
                    {
                        return;
                    }
                }
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.DontReturnNullRule0007,
                    returnStatementSyntax.GetLocation(),
                    methodDeclarationSyntax.Identifier.ValueText
                )
            );
        }
    }
}