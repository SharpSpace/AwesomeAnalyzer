using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RenameAsyncAnalyzer : DiagnosticAnalyzer
    {
        private const string TextAsync = "Async";
        private const string Textasync = "async";
        private const string TextTask = "Task";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.RenameAsyncRule0100
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MethodDeclarationSyntax methodDeclarationSyntax)) return;
            if (!methodDeclarationSyntax.Identifier.ValueText.EndsWith(TextAsync)) return;
            if ((methodDeclarationSyntax.ReturnType is GenericNameSyntax genericNameSyntax &&
                genericNameSyntax.Identifier.ValueText == TextTask) ||
                methodDeclarationSyntax.Modifiers.Any(modifier => modifier.ValueText == Textasync)
                ) return;

            if (methodDeclarationSyntax.ReturnType is IdentifierNameSyntax)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.RenameAsyncRule0100,
                methodDeclarationSyntax.Identifier.GetLocation(),
                methodDeclarationSyntax.Identifier.ValueText
            ));
        }
    }
}