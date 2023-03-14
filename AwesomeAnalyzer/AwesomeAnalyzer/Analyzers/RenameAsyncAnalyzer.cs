using System.Collections.Immutable;
using System.Linq;
using FleetManagement.Service;
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
        private const string TextValueTask = "ValueTask";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0100RenameAsync
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            using (var _ = new MeasureTime())
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0100RenameAsync.Id))
                {
                    return;
                }

                var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;
                if (!methodDeclarationSyntax.Identifier.ValueText.EndsWith(TextAsync)) return;
                if ((
                        methodDeclarationSyntax.ReturnType is GenericNameSyntax genericNameSyntax
                        && (
                            genericNameSyntax.Identifier.ValueText == TextTask || genericNameSyntax.Identifier.ValueText == TextValueTask
                        )
                    )
                    || methodDeclarationSyntax.Modifiers.Any(x => x.ValueText == Textasync)
                   ) return;

                if (methodDeclarationSyntax.ReturnType is IdentifierNameSyntax)
                {
                    return;
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Rule0100RenameAsync,
                        methodDeclarationSyntax.Identifier.GetLocation(),
                        methodDeclarationSyntax.Identifier.ValueText
                    )
                );
            }
        }
    }
}