using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AwesomeAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ParseAnalyzer : DiagnosticAnalyzer
    {
        private const string TextString = "String";
        private const string TextVar = "var";

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

        private readonly Dictionary<ExpressionSyntax, TypeSyntax> _expectedTypesCache = new Dictionary<ExpressionSyntax, TypeSyntax>();

        private readonly Dictionary<ExpressionSyntax, ITypeSymbol> _variableTypesCache = new Dictionary<ExpressionSyntax, ITypeSymbol>();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.ParseStringRule0005);

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
                if (equalsValueClauseSyntax.Parent is ParameterSyntax) return;

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
            if (!(valueExpression is IdentifierNameSyntax) && !(valueExpression is LiteralExpressionSyntax))
            {
                return;
            }

            var variableType = GetVariableType(context, valueExpression);
            if (variableType?.Name != TextString)
            {
                return;
            }

            switch (GetExpectedType(context, valueExpression))
            {
                case null:
                case IdentifierNameSyntax identifierNameSyntax when identifierNameSyntax.Identifier.ValueText == TextVar:
                case PredefinedTypeSyntax predefinedTypeSyntax when Types.Any(x => x.TypeName == predefinedTypeSyntax.Keyword.ValueText) == false:
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
                    parent = parent.Parent;
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