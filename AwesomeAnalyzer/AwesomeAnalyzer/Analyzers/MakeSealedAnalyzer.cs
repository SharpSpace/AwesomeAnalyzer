using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MakeSealedAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.MakeSealedRule0001);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(c => AnalyzeNodeAsync(c), SyntaxKind.ClassDeclaration);
        }

        private static async Task AnalyzeNodeAsync(SyntaxNodeAnalysisContext context)
        {
            var classVirtualizationVisitor = new ClassVirtualizationVisitor();
            classVirtualizationVisitor.SetCompilation(context.Compilation);
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                classVirtualizationVisitor.Visit(
                    await syntaxTree.GetRootAsync(context.CancellationToken).ConfigureAwait(false)
                );
            }

            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
            if (classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)) return;
            if (classDeclarationSyntax.Modifiers.Any(SyntaxKind.SealedKeyword)) return;
            if (classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword)) return;

            var symbolInfo = context.Compilation
                .GetSemanticModel(classDeclarationSyntax.SyntaxTree)
                .GetDeclaredSymbol(classDeclarationSyntax);
            var identifier = $"{symbolInfo?.ContainingNamespace.ToDisplayString()}.{classDeclarationSyntax.Identifier.ValueText}".Trim('.');

            if (classVirtualizationVisitor.Classes.Any(x => x.BaseClasses.Any(y => y.IdentifierName == identifier))) return;
            if (classDeclarationSyntax.GetMembers(context.Compilation).Any(x => x.IsVirtual)) return;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MakeSealedRule0001,
                classDeclarationSyntax.Identifier.GetLocation(),
                classDeclarationSyntax.Identifier.ValueText
            ));
        }
    }
}