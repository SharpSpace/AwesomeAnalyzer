using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseValueTaskAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0103UseValueTask
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            using (_ = new MeasureTime(true))
            {
                if (context.IsDisabledEditorConfig(DiagnosticDescriptors.Rule0103UseValueTask.Id))
                {
                    return;
                }

                var methodDeclaration = (MethodDeclarationSyntax)context.Node;

                // Skip if not async method
                if (!methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
                {
                    return;
                }

                // Get the return type
                var returnType = methodDeclaration.ReturnType;
                if (returnType == null)
                {
                    return;
                }

                var typeInfo = context.SemanticModel.GetTypeInfo(returnType);
                if (typeInfo.Type == null)
                {
                    return;
                }

                // Check if return type is Task or Task<T>
                var typeName = typeInfo.Type.ToDisplayString();
                if (!typeName.StartsWith("System.Threading.Tasks.Task"))
                {
                    return;
                }

                // Don't suggest ValueTask if already using ValueTask
                if (typeName.StartsWith("System.Threading.Tasks.ValueTask"))
                {
                    return;
                }

                // Skip if method is public interface implementation or override
                if (methodDeclaration.Modifiers.Any(m => 
                    m.IsKind(SyntaxKind.OverrideKeyword) || 
                    m.IsKind(SyntaxKind.VirtualKeyword)))
                {
                    return;
                }

                // Check if the method is implementing an interface
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
                if (methodSymbol == null)
                {
                    return;
                }

                // Skip if it's an interface implementation
                if (methodSymbol.IsInterfaceImplementation())
                {
                    return;
                }

                // Skip test methods
                if (methodDeclaration.AttributeLists.Any(x =>
                    x.Attributes.Any(y =>
                        y.Name.ToFullString() == "TestMethod" ||
                        y.Name.ToFullString() == "Fact" ||
                        y.Name.ToFullString() == "Theory"
                    )))
                {
                    return;
                }

                // Suggest ValueTask for async methods returning Task
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Rule0103UseValueTask,
                        methodDeclaration.Identifier.GetLocation(),
                        methodDeclaration.Identifier.ValueText
                    )
                );
            }
        }
    }

    internal static class SymbolExtensions
    {
        internal static bool IsInterfaceImplementation(this IMethodSymbol methodSymbol)
        {
            if (methodSymbol == null)
            {
                return false;
            }

            // Check if the method explicitly implements an interface
            if (methodSymbol.ExplicitInterfaceImplementations.Any())
            {
                return true;
            }

            // Check if the method implicitly implements an interface
            var containingType = methodSymbol.ContainingType;
            if (containingType == null)
            {
                return false;
            }

            foreach (var @interface in containingType.AllInterfaces)
            {
                foreach (var interfaceMember in @interface.GetMembers().OfType<IMethodSymbol>())
                {
                    var implementation = containingType.FindImplementationForInterfaceMember(interfaceMember);
                    if (SymbolEqualityComparer.Default.Equals(implementation, methodSymbol))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
