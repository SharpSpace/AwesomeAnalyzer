using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ParseAnalyzer : DiagnosticAnalyzer
    {
        public static readonly ImmutableArray<dynamic> Types = ImmutableArray.CreateRange(
            new dynamic[]
            {
                new TryParseTypes<bool>("bool", true, false),
                new TryParseTypes<byte>("byte", 0, 0),
                new TryParseTypes<decimal>("decimal", 1m, 0m),
                new TryParseTypes<double>("double", 2d, 0d),
                new TryParseTypes<float>("float", 3f, 0f),
                new TryParseTypes<int>("int", 10, 0),
                new TryParseTypes<long>("long", 100, 0),
                new TryParseTypes<sbyte>("sbyte", 1, 0),
                new TryParseTypes<short>("short", 1, 0),
                new TryParseTypes<uint>("uint", 10, 0),
                new TryParseTypes<ulong>("ulong", 100, 0),
                new TryParseTypes<ushort>("ushort", 1, 0),
            }
        );

        private const string TextString = "String";

        private const string TextVar = "var";

        private readonly Dictionary<ExpressionSyntax, TypeSyntax> _expectedTypesCache = new Dictionary<ExpressionSyntax, TypeSyntax>();

        private readonly Dictionary<ExpressionSyntax, ITypeSymbol> _variableTypesCache = new Dictionary<ExpressionSyntax, ITypeSymbol>();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.ParseStringRule0005);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeEqualsValueClause, SyntaxKind.EqualsValueClause);
            context.RegisterSyntaxNodeAction(AnalyzeNodeReturnStatement, SyntaxKind.ReturnStatement);
        }

        private void AnalyzeEqualsValueClause(SyntaxNodeAnalysisContext context)
        {
            var equalsValueClauseSyntax = (EqualsValueClauseSyntax)context.Node;
            if (equalsValueClauseSyntax.Parent is ParameterSyntax) return;
            if (equalsValueClauseSyntax.Value == null) return;

            AnalyzeNode(context, equalsValueClauseSyntax.Value);
        }

        private void AnalyzeNodeReturnStatement(SyntaxNodeAnalysisContext context)
        {
            var returnStatementSyntax = (ReturnStatementSyntax)context.Node;
            AnalyzeNode(context, returnStatementSyntax.Expression);
        }

        private void AnalyzeNode(
            SyntaxNodeAnalysisContext context,
            ExpressionSyntax sourceValueExpression
        )
        {
            if (!(sourceValueExpression is IdentifierNameSyntax) && !(sourceValueExpression is LiteralExpressionSyntax))
            {
                return;
            }

            string targetType;

            var variableDeclarationSyntax = sourceValueExpression.Parent?.Parent?.Parent as VariableDeclarationSyntax;
            if (variableDeclarationSyntax?.Type is IdentifierNameSyntax identifierNameSyntax)
            {
                targetType = identifierNameSyntax.Identifier.ValueText;
                if (targetType == TextVar)
                {
                    return;
                }
            }
            else
            {
                targetType = (variableDeclarationSyntax?.Type as PredefinedTypeSyntax)?.Keyword.ValueText;
            }

            if (targetType == null && sourceValueExpression.HasParent<ReturnStatementSyntax>() != null)
            {
                var methodDeclarationSyntax = sourceValueExpression.HasParent<MethodDeclarationSyntax>();
                targetType = GetVariableType(context, methodDeclarationSyntax.ReturnType).ToString();
                if (targetType.Contains("Task"))
                {
                    targetType = targetType.Replace("System.Threading.Tasks.Task<", string.Empty);
                    targetType = targetType.Substring(0, targetType.Length - 1);
                }
            }

            // Debug.WriteLine("T:" + targetType);
            if (targetType == "void") return;
            if (targetType == "dynamic") return;
            if (targetType == "object") return;

            var sourceType = GetVariableType(context, sourceValueExpression);
            if (sourceType == null) return;
            if (sourceType.ToString() != "string") return;

            // Debug.WriteLine("S:" + sourceType.ToString());
            if (string.Equals(sourceType.ToString(), targetType, System.StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.ParseStringRule0005,
                    sourceValueExpression.GetLocation()
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
                var symbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, identifierNameSyntax, context.CancellationToken).Symbol;
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
                while (!(parent is MethodDeclarationSyntax)
                    && !(parent is PropertyDeclarationSyntax)
                    && !(parent is FieldDeclarationSyntax)
                    && !(parent is LocalDeclarationStatementSyntax)
                )
                {
                    parent = parent?.Parent;
                }

                switch (parent)
                {
                    case MethodDeclarationSyntax methodDeclaration: expectedType = methodDeclaration.ReturnType; break;
                    case PropertyDeclarationSyntax propertyDeclaration: expectedType = propertyDeclaration.Type; break;
                    case FieldDeclarationSyntax fieldDeclaration: expectedType = fieldDeclaration.Declaration.Type; break;
                    case LocalDeclarationStatementSyntax localDeclaration: expectedType = localDeclaration.Declaration.Type; break;
                }
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

            variableType = ModelExtensions.GetTypeInfo(context.SemanticModel, valueExpression, context.CancellationToken).Type;
            _variableTypesCache[valueExpression] = variableType;
            return variableType;
        }
    }
}