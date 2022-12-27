namespace AwesomeAnalyzer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParseAnalyzer : DiagnosticAnalyzer
{
    private readonly Dictionary<ExpressionSyntax, TypeSyntax> _expectedTypesCache = new();
    private readonly Dictionary<ExpressionSyntax, ITypeSymbol> _variableTypesCache = new();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ParseStringRule0005);

    public static readonly ImmutableArray<dynamic> Types = ImmutableArray.CreateRange(
        new dynamic[]
            {
                new TryParseTypes<bool>("bool", true, false),
                new TryParseTypes<byte>("byte", 0, (byte)0),
                //new TryParseTypes<char>("char", 'c', char.MaxValue),
                new TryParseTypes<decimal>("decimal", 1m, 0m),
                new TryParseTypes<double>("double", 2d, 0d),
                new TryParseTypes<float>("float", 3f, 0f),
                new TryParseTypes<int>("int", 10, 0),
                new TryParseTypes<long>("long", 100, 0),
                new TryParseTypes<sbyte>("sbyte", 1, 0),
                new TryParseTypes<short>("short", 1, 0),
                new TryParseTypes<uint>("uint", 10, 0),
                new TryParseTypes<ulong>("ulong", 100, 0),
                new TryParseTypes<ushort>("ushort", 1, 0)
            }
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.EqualsValueClause);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ReturnStatement);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is EqualsValueClauseSyntax equalsValueClauseSyntax)
        {
            AnalyzeNode(context, equalsValueClauseSyntax.Value);
        }
        else if (context.Node is ReturnStatementSyntax returnStatementSyntax)
        {
            AnalyzeNode(context, returnStatementSyntax.Expression);
        }
    }

    private void AnalyzeNode(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax valueExpression
    )
    {
        if (valueExpression is not IdentifierNameSyntax and not LiteralExpressionSyntax)
        {
            return;
        }

        var variableType = GetVariableType(context, valueExpression);
        if (variableType is not { Name: "String" })
        {
            return;
        }

        var expectedType = GetExpectedType(context, valueExpression);
        if (expectedType is null or 
            IdentifierNameSyntax { Identifier.ValueText: "var" } ||
            (expectedType is PredefinedTypeSyntax predefinedTypeSyntax && Types.Any(x => x.TypeName == predefinedTypeSyntax.Keyword.ValueText) == false)
        )
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.ParseStringRule0005,
                valueExpression.GetLocation()
            )
        );
    }

    private TypeSyntax GetExpectedType(SyntaxNodeAnalysisContext context, ExpressionSyntax valueExpression)
    {
        if (_expectedTypesCache.TryGetValue(valueExpression, out var expectedType))
        {
            return expectedType;
        }

        if (valueExpression is IdentifierNameSyntax identifierNameSyntax)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(identifierNameSyntax, context.CancellationToken).Symbol;
            if (symbol == null)
            {
                return null;
            }

            var typeString = symbol.ContainingSymbol.ToDisplayString();
            expectedType = SyntaxFactory.ParseTypeName(typeString);
        }
        else
        {
            var parent = valueExpression.Parent;
            while (parent is not MethodDeclarationSyntax 
                and not PropertyDeclarationSyntax 
                and not FieldDeclarationSyntax 
                and not LocalDeclarationStatementSyntax
            )
            {
                parent = parent!.Parent;
            }

            expectedType = parent switch
            {
                MethodDeclarationSyntax methodDeclaration => methodDeclaration.ReturnType,
                PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Type,
                FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Declaration.Type,
                LocalDeclarationStatementSyntax localDeclaration => localDeclaration.Declaration.Type,
                _ => null
            };
        }

        _expectedTypesCache[valueExpression] = expectedType;
        return expectedType;
    }

    private ITypeSymbol GetVariableType(SyntaxNodeAnalysisContext context, ExpressionSyntax valueExpression)
    {
        if (_variableTypesCache.TryGetValue(valueExpression, out var variableType))
        {
            return variableType;
        }

        variableType = context.SemanticModel.GetTypeInfo(valueExpression, context.CancellationToken).Type;
        _variableTypesCache[valueExpression] = variableType;
        return variableType;
    }
}

public interface ITryParseTypes
{
    public string TypeName { get; }

    public string TestValueString { get; }

    public string DefaultValueString { get; }
}

public readonly record struct TryParseTypes<T>(string TypeName, T TestValue, T DefaultValue) // : ITryParseTypes
{
    //public string TestValueString => TypeName switch
    //{
    //    "decimal" => "\"" + TestValue.ToString().ToLower() + "\"",
    //    _ => TestValue.ToString().ToLower()
    //};

    public string TestValueString => $"\"{TestValue.ToString().ToLower()}\"";

    public string DefaultValueString => DefaultValue.ToString().ToLower();

    public string Cast => TypeName switch
    {
        "byte" => "(byte)",
        "sbyte" => "(sbyte)",
        "short" => "(short)",
        "ushort" => "(ushort)",
        _ => string.Empty
    };
}