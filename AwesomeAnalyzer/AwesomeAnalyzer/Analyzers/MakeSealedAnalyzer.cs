namespace AwesomeAnalyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MakeSealedAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.MakeSealedRule0001);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        //context.RegisterSymbolStartAction(AnalyzeSymbolStart, SymbolKind.TypeParameter);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        if (classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return;
        }

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

        context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MakeSealedRule0001, classDeclarationSyntax.Identifier.GetLocation()));
    }
}