namespace AwesomeAnalyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DisposedAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.DisposedRule0004
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax objectCreationExpressionSyntax) return;
        if (objectCreationExpressionSyntax.Parent is not EqualsValueClauseSyntax equalsValueClauseSyntax) return;
        if (equalsValueClauseSyntax.Parent is not VariableDeclaratorSyntax variableDeclaratorSyntax) return;
        if (variableDeclaratorSyntax.Parent is not VariableDeclarationSyntax variableDeclarationSyntax) return;

        var localDeclarationStatementSyntax = variableDeclarationSyntax.Parent as LocalDeclarationStatementSyntax;
        if (localDeclarationStatementSyntax is { UsingKeyword.ValueText: "using" }
        ) return;

        if (!(localDeclarationStatementSyntax?.Parent is BlockSyntax blockSyntax)) return;

        var expressionStatementSyntaxes = blockSyntax.Statements.OfType<ExpressionStatementSyntax>();
        var expression = $"{variableDeclaratorSyntax.Identifier.ValueText}.Dispose()";
        if (expressionStatementSyntaxes.Any(x => x.Expression.ToString() == expression)) return;

        var typeSymbol = context.SemanticModel.GetTypeInfo(objectCreationExpressionSyntax).Type;
        if (typeSymbol == null) return;

        var interfaces = typeSymbol.AllInterfaces;
        if (interfaces.Any(x => x.Name == "IDisposable") == false) return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DisposedRule0004,
            variableDeclarationSyntax.GetLocation(),
            messageArgs: variableDeclarationSyntax.ToString()
        ));
    }
}