using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RenameAsyncAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.MakeAsyncRule0100);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MethodDeclarationSyntax methodDeclarationSyntax)) return;
            
            if (methodDeclarationSyntax.Modifiers.Any(modifier => modifier.ValueText == "async")) return;
            if (!methodDeclarationSyntax.Identifier.ValueText.EndsWith("Async")) return;

            //if (methodDeclarationSyntax.HasParent<InterfaceDeclarationSyntax>() != null) return;

            if (methodDeclarationSyntax.ReturnType is IdentifierNameSyntax identifierNameSyntax &&
                identifierNameSyntax.Identifier.ValueText != "Task")
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MakeAsyncRule0100, methodDeclarationSyntax.Identifier.GetLocation()));
            }

            if (!(methodDeclarationSyntax.ReturnType is IdentifierNameSyntax)
               )
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MakeAsyncRule0100, methodDeclarationSyntax.Identifier.GetLocation()));
            }
        }
    }
}