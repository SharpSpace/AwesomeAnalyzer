using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Document = Microsoft.CodeAnalysis.Document;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class SimilarCodeFixProvider : CodeFixProvider
    {
        private static Dictionary<Type, string> _typeAlias = new Dictionary<Type, string>
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(object), "object" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(string), "string" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(void), "void" },
        };

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.Rule0008Similar.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(
            CodeFixContext context
        )
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Move all similar code to new method.",
                        createChangedDocument: token => CodeFixAsync(root, context, diagnostic),
                        equivalenceKey: nameof(SimilarCodeFixProvider)
                    ),
                    context.Diagnostics
                );
            }
        }

        private static MethodDeclarationSyntax CreateMethod(
            Diagnostic diagnostic,
            MethodItem sourceBlock,
            SyntaxToken methodName,
            List<ISymbol> variables,
            SyntaxNode declaration
        ) => SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.VoidKeyword)
                            .WithTrailingTrivia(SyntaxFactory.Space)
                    ),
                    methodName
                )
                .WithModifiers(GetMethodModifiers(sourceBlock))
                .WithParameterList(GetMethodParameters(diagnostic, declaration, variables))
                .WithBody(GetMethodBody(
                    diagnostic,
                    declaration
                ))
                .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        private static string FixBody(Diagnostic diagnostic, SyntaxNode declaration)
        {
            var body = declaration.GetText();

            var decendants = GetChildLiteralExpressionSyntax(declaration).ToList();
            var excludeParameters = diagnostic.AdditionalLocations.Select(
                    x =>
                    {
                        var node = declaration.HasParent<ClassDeclarationSyntax>().FindToken(x.SourceSpan.Start).Parent;
                        var additionalParameters = GetChildLiteralExpressionSyntax(node).Select(y => y.SyntaxNode).ToList();
                        return decendants.Select(y => y.SyntaxNode).Intersect(additionalParameters);
                    }
                )
                .SelectMany(x => x)
                .ToList();
            decendants = decendants.Where(x => excludeParameters.Contains(x.SyntaxNode) == false).ToList();

            if (decendants.Any())
            {
                var i = 0;
                body = decendants.Aggregate(
                    body,
                    (current, tuple) => current.Replace(
                        tuple.Span.Start - declaration.FullSpan.Start,
                        tuple.Span.Length,
                        $"{tuple.Name}{i++}"
                    )
                );
            }

            body = declaration.DescendantNodesAndSelf()
                .OfType<PredefinedTypeSyntax>()
                .Where(x => x.IsVar == false)
                .Select(
                    predefinedTypeSyntax => new TextSpan(
                        predefinedTypeSyntax.Span.Start - declaration.FullSpan.Start,
                        predefinedTypeSyntax.Span.Length
                    )
                )
                .Aggregate(body, (current, textSpan) => current.Replace(textSpan, "var"));

            return body.ToString();
        }

        private static IEnumerable<ISymbol> GetChildIdentifierNameSyntax(
            SemanticModel semanticModel,
            SyntaxNode declaration
        ) =>
        declaration.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .Select(x => semanticModel.GetSymbolInfo(x).Symbol)
            .Where(x =>
                x != null &&
                (
                    x.Kind == SymbolKind.Local ||
                    x.Kind == SymbolKind.Parameter
                )
            )
            .Distinct();

        private static IEnumerable<(int Index, string Name, string Type, string SyntaxNode, TextSpan Span)> GetChildLiteralExpressionSyntax(
            SyntaxNode declaration
        ) => declaration.DescendantNodesAndSelf()
                .OfType<LiteralExpressionSyntax>()
                .Where(x => x.IsKind(SyntaxKind.NullLiteralExpression) == false && x.Token.Value != null)
                .Select((x, i) => (
                    Index: i,
                    Name: "s",
                    Type: GetTypeName(x.Token.Value.GetType()),
                    SyntaxNode: x.ToString(),
                    Span: new TextSpan(x.Span.Start, x.Span.Length)
                ));

        private static BlockSyntax GetMethodBody(
            Diagnostic diagnostic,
            SyntaxNode declaration
        )
            => SyntaxFactory.Block(
                    SyntaxFactory.ParseStatement(FixBody(diagnostic, declaration))
                        .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                )
                .WithCloseBraceToken(
                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                )
                .WithLeadingTrivia(
                    SyntaxFactory.CarriageReturnLineFeed
                )
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        private static SyntaxTokenList GetMethodModifiers(
            MethodItem sourceBlock
        ) => sourceBlock.Modifiers.Any()
            ? sourceBlock.Modifiers
            : SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxFactory.Space)
            );

        private static ParameterListSyntax GetMethodParameters(
            Diagnostic diagnostic,
            SyntaxNode declaration,
            List<ISymbol> variables
        )
        {
            var parameterSyntaxes = variables.Select(x =>
                SyntaxFactory
                    .Parameter(SyntaxFactory.Identifier(x.Name))
                    .WithType(
                        SyntaxFactory
                            .ParseTypeName(
                                ((x as ILocalSymbol)?.Type ?? (x as IParameterSymbol)?.Type)
                                .ToDisplayString(
                                    SymbolDisplayFormat.MinimallyQualifiedFormat
                                )
                            )
                            .WithTrailingTrivia(SyntaxFactory.Space)
                    )
            ).ToList();

            var firstParameters = GetChildLiteralExpressionSyntax(declaration);
            var excludeParameters = diagnostic.AdditionalLocations.Select(
                    x =>
                    {
                        var node = declaration.HasParent<ClassDeclarationSyntax>().FindToken(x.SourceSpan.Start).Parent;
                        var additionalParameters = GetChildLiteralExpressionSyntax(node).Select(y => y.SyntaxNode).ToList();
                        return firstParameters.Select(y => y.SyntaxNode).Intersect(additionalParameters);
                    }
                )
                .SelectMany(x => x)
                .ToList();

            parameterSyntaxes.AddRange(
                firstParameters
                    .Where(x => excludeParameters.Contains(x.SyntaxNode) == false)
                    .Select((x, i) =>
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier($"{x.Name}{i}"))
                            .WithType(SyntaxFactory
                                .ParseTypeName(x.Type)
                                .WithTrailingTrivia(SyntaxFactory.Space)
                            ).WithLeadingTrivia(SyntaxFactory.Space)
                    )
            );

            return SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    parameterSyntaxes
                )
            );
        }

        private static MethodItem GetParentMethod(SyntaxNode declaration)
        {
            var methodDeclarationSyntax = declaration.HasParent<MethodDeclarationSyntax>();
            if (methodDeclarationSyntax != null)
            {
                return new MethodItem(
                    methodDeclarationSyntax.Modifiers,
                    methodDeclarationSyntax.GetLeadingTrivia()
                );
            }

            var constructorDeclarationSyntax = declaration.HasParent<ConstructorDeclarationSyntax>();
            if (constructorDeclarationSyntax != null)
            {
                return new MethodItem(
                    constructorDeclarationSyntax.Modifiers,
                    constructorDeclarationSyntax.GetLeadingTrivia()
                );
            }

            var propertyDeclarationSyntax = declaration.HasParent<PropertyDeclarationSyntax>();
            if (propertyDeclarationSyntax != null)
            {
                return new MethodItem(
                    propertyDeclarationSyntax.Modifiers,
                    propertyDeclarationSyntax.GetLeadingTrivia()
                );
            }

            var delegateDeclarationSyntax = declaration.HasParent<DelegateDeclarationSyntax>();
            {
                return new MethodItem(
                    delegateDeclarationSyntax.Modifiers,
                    delegateDeclarationSyntax.GetLeadingTrivia()
                );
            }
        }

        private static string GetTypeName(Type type)
        {
            var name = TypeNameOrAlias(type);
            return type.DeclaringType is Type dec
                ? $"{GetTypeName(dec)}.{name}"
                : name;
        }

        private static List<ISymbol> GetVariablesThatIntersects(
            SyntaxNode declaration,
            SemanticModel semanticModel
        )
            => GetChildIdentifierNameSyntax(semanticModel, declaration)
                .Intersect(semanticModel.AnalyzeDataFlow(declaration).DataFlowsIn)
                .ToList();

        private static string TypeNameOrAlias(Type type)
        {
            var nullbase = Nullable.GetUnderlyingType(type);
            if (nullbase != null)
                return TypeNameOrAlias(nullbase) + "?";

            if (type.BaseType == typeof(System.Array))
                return TypeNameOrAlias(type.GetElementType()) + "[]";

            if (_typeAlias.TryGetValue(type, out var alias))
                return alias;

            if (type.IsGenericType)
            {
                var name = type.Name.Split('`').FirstOrDefault();
                var parms =
                    type.GetGenericArguments()
                        .Select(a => type.IsConstructedGenericType ? TypeNameOrAlias(a) : a.Name);
                return $"{name}<{string.Join(",", parms)}>";
            }

            return type.Name;
        }

        private async Task<Document> CodeFixAsync(
            SyntaxNode root,
            CodeFixContext context,
            Diagnostic diagnostic
        )
        {
            var semanticModel = await context.Document.GetSemanticModelAsync();
            if (semanticModel == null) return context.Document;

            var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent
                ?.AncestorsAndSelf()
                ?.OfType<SyntaxNode>()
                .FirstOrDefault();

            if (declaration == null) return context.Document;

            var variables = GetVariablesThatIntersects(declaration, semanticModel);

            var sourceBlock = GetParentMethod(declaration);

            const string name = "NewMethod";
            var methodName = SyntaxFactory.Identifier(name);

            var parentClass = declaration.HasParent<ClassDeclarationSyntax>();
            var newMethod = CreateMethod(
                diagnostic,
                sourceBlock,
                methodName,
                variables,
                declaration
            );
            var newClass = parentClass.AddMembers(
                newMethod
            );

            var newRoot = root.ReplaceNode(parentClass, newClass);

            var sourceText = await UpdateSourceTextAsync(
                    diagnostic,
                    methodName.ValueText,
                    variables,
                    newRoot
                )
                .ConfigureAwait(false);

            return await Formatter.FormatAsync(
                context.Document.WithText(sourceText),
                new []{ newClass.Span }
            ).ConfigureAwait(false);
        }

        private async Task<SourceText> UpdateSourceTextAsync(
            Diagnostic diagnostic,
            string methodName,
            IReadOnlyCollection<ISymbol> variables,
            SyntaxNode newRoot
        )
        {
            var locations = diagnostic.AdditionalLocations.Select(x => (x.SourceSpan.Start, x.SourceSpan.Length)).ToList();
            locations.Add((diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));

            var allParameters = locations
                .Select(location =>
                {
                    var node = newRoot.FindToken(location.Start).Parent;
                    return GetChildLiteralExpressionSyntax(node).Select(parameter => (parameter.Index, parameter.Name, parameter.Type, parameter.SyntaxNode, parameter.Span, location)).ToList();
                }
                )
                .SelectMany(x => x)
                .GroupBy(x => (x.Index, x.SyntaxNode))
                .Where(x => x.Count() == 1)
                .SelectMany(x => x)
                .Distinct()
                .ToList();

            var replaceList = locations
                .Select(location =>
                {
                    var start = location.Start;
                    var length = location.Length;

                    return (new TextSpan(start, length), GetMethodCallString(allParameters.Where(x => x.location == location).Select(x => x.SyntaxNode.ToString())));
                }
            );

            var source = newRoot.GetText();

            foreach (var (textSpan, code) in replaceList.OrderByDescending(x => x.Item1.Start))
            {
                // Debug.WriteLine("textOut: '" + source.GetSubText(textSpan) + "' -> '" + code + "'");
                source = source.Replace(
                    textSpan,
                    code
                );
            }

            return source;

            string GetMethodCallString(IEnumerable<string> stringParameters) =>
                new StringBuilder(methodName)
                    .Append('(')
                    .Append(string.Join(", ", variables.Select(x => x.Name).Union(stringParameters)))
                    .Append(");")
                    .ToString();
        }
    }

    internal readonly struct MethodItem
    {
        public readonly SyntaxTokenList Modifiers;
        public readonly SyntaxTriviaList LeadingTrivia;

        public MethodItem(SyntaxTokenList modifiers, SyntaxTriviaList leadingTrivia)
        {
            Modifiers = modifiers;
            LeadingTrivia = leadingTrivia;
        }

        public override bool Equals(object obj) => obj is MethodItem other && Modifiers.Equals(other.Modifiers) && LeadingTrivia.Equals(other.LeadingTrivia);

        public override int GetHashCode()
        {
            var hashCode = -46165669;
            hashCode = hashCode * -1521134295 + Modifiers.GetHashCode();
            hashCode = hashCode * -1521134295 + LeadingTrivia.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out SyntaxTokenList modifiers, out SyntaxTriviaList leadingTrivia)
        {
            modifiers = Modifiers;
            leadingTrivia = LeadingTrivia;
        }

        public static implicit operator (SyntaxTokenList Modifiers, SyntaxTriviaList LeadingTrivia)(MethodItem value)
        {
            return (value.Modifiers, value.LeadingTrivia);
        }

        public static implicit operator MethodItem((SyntaxTokenList Modifiers, SyntaxTriviaList LeadingTrivia) value)
        {
            return new MethodItem(value.Modifiers, value.LeadingTrivia);
        }
    }
}