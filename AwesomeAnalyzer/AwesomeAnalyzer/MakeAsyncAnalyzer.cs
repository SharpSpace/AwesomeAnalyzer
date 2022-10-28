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
        public const string DiagnosticId = "MakeAsync";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Method contains Async prefix",
            "MessageFormat",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Removes Async prefix from method name"
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

            if (
                methodDeclarationSyntax.Identifier.ValueText.EndsWith("Async") &&
                methodDeclarationSyntax.Modifiers.Any(x => x.ValueText == "async") == false &&
                methodDeclarationSyntax.ReturnType is IdentifierNameSyntax identifierNameSyntax &&
                identifierNameSyntax.Identifier.ValueText != "Task"
            )
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
            }

            if (methodDeclarationSyntax.Identifier.ValueText.EndsWith("Async") &&
                methodDeclarationSyntax.Modifiers.Any(x => x.ValueText == "async") == false &&
                !(methodDeclarationSyntax.ReturnType is IdentifierNameSyntax)
            )
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
            }
        }
    }
}