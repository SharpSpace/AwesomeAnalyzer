using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MakeAsyncAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.MakeAsyncRule0002);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
            //context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodKeyword);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MethodDeclarationSyntax methodDeclarationSyntax)) return;
            
            var hasAsyncModifier = methodDeclarationSyntax.Modifiers.Any(modifier => modifier.ValueText == "async");

            if (
                methodDeclarationSyntax.Identifier.ValueText.EndsWith("Async") &&
                hasAsyncModifier == false &&
                methodDeclarationSyntax.ReturnType is IdentifierNameSyntax identifierNameSyntax &&
                identifierNameSyntax.Identifier.ValueText != "Task"
            )
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MakeAsyncRule0002, methodDeclarationSyntax.Identifier.GetLocation()));
            }

            if (methodDeclarationSyntax.Identifier.ValueText.EndsWith("Async") &&
                hasAsyncModifier == false &&
                !(methodDeclarationSyntax.ReturnType is IdentifierNameSyntax)
               )
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MakeAsyncRule0002, methodDeclarationSyntax.Identifier.GetLocation()));
            }
        }
    }
}