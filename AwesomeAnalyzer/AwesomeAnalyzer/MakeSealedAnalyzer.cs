using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MakeSealedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "JJ0001";
        private const string Category = "Naming";
        
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Class should have modifier sealed",
            "Class should contain modifier sealed",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Class should have modifier sealed."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //context.RegisterSymbolStartAction(AnalyzeSymbolStart, SymbolKind.TypeParameter);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

            if (classDeclarationSyntax.Modifiers.Any(SyntaxKind.SealedKeyword))
            {
                return;
            }

            var identifier = classDeclarationSyntax.Identifier.ValueText;
            if (classDeclarationSyntax.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
            {
                identifier = $"{namespaceDeclarationSyntax.Name}.{classDeclarationSyntax.Identifier.ValueText}";
            }

            var classVirtualizationVisitor = new ClassVirtualizationVisitor();
            classVirtualizationVisitor.Visit(context.SemanticModel.SyntaxTree.GetRoot());

            if (classVirtualizationVisitor.Classes
                .Where(x => x.BaseClasses != null)
                .Any(x => x.BaseClasses.Any(y => y.IdentifierName == identifier))
            )
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, classDeclarationSyntax.Identifier.GetLocation()));
        }

        //private static void AnalyzeSymbolStart(SymbolStartAnalysisContext context)
        //{
        //    var symbol = (ITypeParameterSymbol)context.Symbol;

        //    symbol.
        //}
        
        private bool InheritsFrom(INamedTypeSymbol baseClassSymbol, INamedTypeSymbol symbol)
        {
            while (true)
            {
                if (symbol.ToString() == baseClassSymbol.ToString())
                {
                    return true;
                }
                if (symbol.BaseType != null)
                {
                    symbol = symbol.BaseType;
                    continue;
                }
                break;
            }

            return false;
        }
    }
}