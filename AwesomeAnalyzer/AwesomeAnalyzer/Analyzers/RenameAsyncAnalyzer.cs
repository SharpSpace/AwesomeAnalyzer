namespace AwesomeAnalyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RenameAsyncAnalyzer : DiagnosticAnalyzer
{
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
        if (context.Node is not MethodDeclarationSyntax methodDeclarationSyntax) return;

        if (methodDeclarationSyntax.Modifiers.Any(modifier => modifier.ValueText == "async")) return;
        if (!methodDeclarationSyntax.Identifier.ValueText.EndsWith("Async")) return;

        //if (methodDeclarationSyntax.HasParent<InterfaceDeclarationSyntax>() != null) return;

        if (methodDeclarationSyntax.ReturnType is IdentifierNameSyntax identifierNameSyntax &&
            identifierNameSyntax.Identifier.ValueText != "Task")
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RenameAsyncRule0100, methodDeclarationSyntax.Identifier.GetLocation()));
        }

        if (!(methodDeclarationSyntax.ReturnType is IdentifierNameSyntax)
        )
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RenameAsyncRule0100, methodDeclarationSyntax.Identifier.GetLocation()));
        }
    }
}