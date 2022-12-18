namespace AwesomeAnalyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AddAwaitAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.AddAwaitRule0101,
        DiagnosticDescriptors.AddAsyncRule0102
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocationExpressionSyntax) return;

        if (invocationExpressionSyntax.HasParent<AwaitExpressionSyntax>() != null) return;

        var typeSymbol = context.SemanticModel.GetTypeInfo(invocationExpressionSyntax)!;
        if (typeSymbol.Type!.Name != "Task") return;

        var methodDeclarationSyntax = invocationExpressionSyntax.HasParent<MethodDeclarationSyntax>();
        if (methodDeclarationSyntax != null)
        {
            var typeInfo = context.SemanticModel.GetTypeInfo(methodDeclarationSyntax.ReturnType)!;
            if (typeInfo.Type!.Name == "Task") return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.AddAwaitRule0101,
            invocationExpressionSyntax.GetLocation(),
            messageArgs: invocationExpressionSyntax.ToString()
        ));
    }
}