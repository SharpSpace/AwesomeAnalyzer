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
        public const string DiagnosticId = "JJ0002";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Method contains Async prefix",
            "This method contains Async prefix and its not async",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Removes Async prefix from method name."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
            //context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodKeyword);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                bool hasAsyncModifier = false;
                foreach (var modifier in methodDeclarationSyntax.Modifiers)
                {
                    if (modifier.ValueText == "async")
                    {
                        hasAsyncModifier = true;
                        break;
                    }
                }

                if (
                    methodDeclarationSyntax.Identifier.ValueText.EndsWith("Async") &&
                    hasAsyncModifier == false &&
                    methodDeclarationSyntax.ReturnType is IdentifierNameSyntax identifierNameSyntax &&
                    identifierNameSyntax.Identifier.ValueText != "Task"
                )
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclarationSyntax.Identifier.GetLocation()));
                }

                if (methodDeclarationSyntax.Identifier.ValueText.EndsWith("Async") &&
                    hasAsyncModifier == false &&
                    !(methodDeclarationSyntax.ReturnType is IdentifierNameSyntax)
                   )
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclarationSyntax.Identifier.GetLocation()));
                }
            }
        }
    }
}