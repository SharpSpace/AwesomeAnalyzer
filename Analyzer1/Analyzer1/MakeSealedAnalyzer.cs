using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MakeSealedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MakeSealed";
        private const string Category = "Naming";
        private static readonly LocalizableString Description = new LocalizableResourceString(
            nameof(Resources.MakeSealedAnalyzerDescription),
            Resources.ResourceManager,
            typeof(Resources));
        
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
            nameof(Resources.MakeSealedAnalyzerMessageFormat),
            Resources.ResourceManager,
            typeof(Resources));

        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.MakeSealedAnalyzerTitle),
            Resources.ResourceManager,
            typeof(Resources));
        
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description
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

            //var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            //var fullyQualifiedMetadataName = symbol.GetFullMetadataName();
            //var typeByMetadataName = context.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);

            ////var findReferences = await SymbolFinder.FindReferencesAsync(symbol, );

            //var a = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax).BaseType;

            //var onlyClass = classDeclarationSyntax.WithoutLeadingTrivia().WithoutTrailingTrivia();
            //var syntaxReference = classDeclarationSyntax.SyntaxTree.GetReference(onlyClass);
            //if (syntaxReference != null)
            //{
            //    return;
            //}

            //if (classDeclarationSyntax.BaseList != null)
            //{
            //    return;
            //}

            //// Perform data flow analysis on the local declaration.
            //var dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(classDeclarationSyntax);

            //// Retrieve the local symbol for each variable in the local declaration
            //// and ensure that it is not written outside of the data flow analysis region.
            //var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, context.CancellationToken);
            //if (dataFlowAnalysis.WrittenOutside.Contains(classSymbol))
            //{
            //    return;
            //}

            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
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

    class ClassVirtualizationVisitor : CSharpSyntaxRewriter
    {
        public List<ClassInformation> Classes { get; }

        public ClassVirtualizationVisitor()
        {
            this.Classes = new List<ClassInformation>();
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            //node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);

            string nameSpaceName = null;
            if (node.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
            {
                nameSpaceName = namespaceDeclarationSyntax.Name.ToString();
            }
            
            this.Classes.Add(new ClassInformation
            {
                ClassName = node.Identifier.ValueText,
                NameSpaceName = nameSpaceName,
                BaseClasses = node.BaseList?.Types.Select(x => new ClassInformation
                {
                    ClassName = x.ToString(),
                    NameSpaceName = ((NamespaceDeclarationSyntax)x.Parent.Parent.Parent).Name.ToString()
                }).ToList()
            });

            return node;
        }
    }

    public class ClassInformation
    {
        public string ClassName { get; set; }

        public string NameSpaceName { get; set; }

        public List<ClassInformation> BaseClasses { get; set; }

        public string IdentifierName => $"{this.NameSpaceName}.{this.ClassName}";
    }
}