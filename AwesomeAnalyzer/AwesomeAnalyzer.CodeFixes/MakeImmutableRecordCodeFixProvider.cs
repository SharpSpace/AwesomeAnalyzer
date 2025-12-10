using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider))]
    [Shared]
    public sealed class MakeImmutableRecordCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            DiagnosticDescriptors.Rule0009MakeImmutableRecord.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var message = new StringBuilder();
            var declarations = new List<PropertyDeclarationSyntax>();
            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var syntaxes = root.FindToken(diagnosticSpan.Start)
                    .Parent?.AncestorsAndSelf()
                    .OfType<PropertyDeclarationSyntax>();
                if (syntaxes != null)
                {
                    declarations.AddRange(syntaxes);
                }

                message.AppendLine(diagnostic.GetMessage());
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: message.ToString(),
                    createChangedDocument: c => FixCodeAsync(
                        context.Document,
                        root,
                        declarations,
                        c
                    ),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)
                ),
                context.Diagnostics
            );
        }

        private static IEnumerable<(SyntaxNode item, SyntaxNode syntax)> FindRecordConstructorUsages(
            SemanticModel semanticModel,
            RecordDeclarationSyntax declaration,
            ImmutableList<PropertyDeclarationSyntax> propertyDeclarationSyntaxes
        )
        {
            var propertySymbol = semanticModel.GetDeclaredSymbol(declaration);
            if (propertySymbol == null)
            {
                yield break;
            }

            var symbolEqualityComparer = SymbolEqualityComparer.Default;

            var syntaxNode = declaration.GetRoot();
            var descendantNodes = syntaxNode.DescendantNodes().Where(x => x is ObjectCreationExpressionSyntax);

            var references = descendantNodes
                .SelectMany(method => method.DescendantNodes().OfType<ExpressionSyntax>())
                .Where(x => symbolEqualityComparer.Equals(semanticModel.GetSymbolInfo(x).Symbol, propertySymbol))
                .ToList();

            var propertyNames = propertyDeclarationSyntaxes.Select(x => x.Identifier.ValueText).ToImmutableList();

            foreach (var item in references)
            {
                if (!(item.Parent is ObjectCreationExpressionSyntax objectCreationExpressionSyntax)) continue;

                var assignmentExpressionSyntaxes = objectCreationExpressionSyntax.Initializer.Expressions
                    .OfType<AssignmentExpressionSyntax>().ToList();

                var argumentsToAdd = assignmentExpressionSyntaxes
                    .Where(x => propertyNames.Contains((x.Left as IdentifierNameSyntax).Identifier.ValueText));

                if (!assignmentExpressionSyntaxes
                    .Any(x => !propertyNames.Contains((x.Left as IdentifierNameSyntax).Identifier.ValueText)) || !argumentsToAdd.Any())
                {
                    continue;
                }

                var expressionsLastTrailingTrivia = objectCreationExpressionSyntax.Initializer.Expressions.Last().GetTrailingTrivia();

                var initializer = objectCreationExpressionSyntax.Initializer
                    .WithLeadingTrivia(objectCreationExpressionSyntax.Initializer.GetLeadingTrivia().Insert(0, SyntaxFactory.CarriageReturnLineFeed))
                    .WithExpressions(
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(
                            assignmentExpressionSyntaxes
                                .Where(x =>
                                    !propertyNames.Contains((x.Left as IdentifierNameSyntax).Identifier.ValueText)
                                )
                        )
                    )
                    .WithCloseBraceToken(
                        objectCreationExpressionSyntax.Initializer.CloseBraceToken
                            .WithLeadingTrivia(
                                SyntaxFactory.ParseLeadingTrivia(
                                        objectCreationExpressionSyntax.Initializer.CloseBraceToken.LeadingTrivia
                                            .ToFullString()
                                            .Replace(Environment.NewLine, string.Empty)
                                    )
                                    .InsertRange(0, expressionsLastTrailingTrivia.ToArray())
                            )
                    )
                    ;

                var syntax = objectCreationExpressionSyntax
                    .WithType(objectCreationExpressionSyntax.Type.WithoutTrivia())
                    .WithArgumentList(
                        (objectCreationExpressionSyntax.ArgumentList ?? SyntaxFactory.ArgumentList()).AddArguments(
                            argumentsToAdd.Select(x =>
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(x.Right.ToString().Replace("\"", string.Empty))
                                    )
                                )
                            ).ToArray()
                        ).WithoutTrivia()
                    )
                    .WithInitializer(
                        initializer
                    )
                    .WithTrailingTrivia(objectCreationExpressionSyntax.GetTrailingTrivia());

                yield return (item.Parent, syntax);
            }
        }

        private static async Task<Document> FixCodeAsync(
            Document document,
            SyntaxNode root,
            List<PropertyDeclarationSyntax> declarations,
            CancellationToken cancellationToken
        )
        {
            var record = declarations[0].HasParent<RecordDeclarationSyntax>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var propertyDeclarationSyntaxes = record.Members.OfType<PropertyDeclarationSyntax>().ToImmutableList();

            var propertiesUsed = GetPropertiesUsed(semanticModel, propertyDeclarationSyntaxes).ToList();
            var propertiesToChange = propertyDeclarationSyntaxes.Except(propertiesUsed).ToImmutableList();
            var parameterList = record.ParameterList ?? SyntaxFactory.ParameterList();
            var recordParameterList = parameterList
                .AddParameters(
                    propertiesToChange.Select((x, i) =>
                        SyntaxFactory.Parameter(
                            SyntaxFactory.Identifier(x.Identifier.ValueText)
                        )
                        .WithType(x.Type)
                        .WithLeadingTrivia((i + parameterList.Parameters.Count) > 0 ? SyntaxFactory.Space : new SyntaxTrivia())
                    ).ToArray()
                ).WithoutTrivia();

            var replaceList = new List<(SyntaxNode Item, SyntaxNode Replace)>();
            RecordDeclarationSyntax newRecord;
            if (propertiesUsed.Any())
            {
                var findRecordContructorUsages = FindRecordConstructorUsages(semanticModel, record, propertiesToChange);
                replaceList.AddRange(findRecordContructorUsages);

                newRecord = record
                    .RemoveNodes(propertiesToChange, SyntaxRemoveOptions.KeepNoTrivia)
                    .WithIdentifier(record.Identifier.WithoutTrivia())
                    .WithParameterList(recordParameterList.WithTrailingTrivia(record.GetTrailingTrivia()))
                    ;
            }
            else
            {
                newRecord = SyntaxFactory.RecordDeclaration(
                    record.AttributeLists,
                    record.Modifiers,
                    record.Keyword,
                    record.Identifier.WithoutTrivia(),
                    record.TypeParameterList,
                    recordParameterList,
                    record.BaseList,
                    record.ConstraintClauses,
                    new SyntaxList<MemberDeclarationSyntax>()
                )
                .WithLeadingTrivia(record.GetLeadingTrivia())
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithTrailingTrivia(record.GetTrailingTrivia());
            }

            var newRoot = replaceList
                .Aggregate(root, (syntax, tuple) => syntax.ReplaceNode(tuple.Item, tuple.Replace))
                .GetText()
                .Replace(record.FullSpan, newRecord.ToFullString());

            return document.WithText(newRoot);
        }

        private static IEnumerable<PropertyDeclarationSyntax> GetPropertiesUsed(
            SemanticModel semanticModel,
            ImmutableList<PropertyDeclarationSyntax> propertyDeclarationSyntaxes
        )
        {
            return propertyDeclarationSyntaxes.Where(x => IsUsed(semanticModel, x));
        }

        private static bool IsUsed(
            SemanticModel semanticModel,
            PropertyDeclarationSyntax propertyDeclaration
        )
        {
            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration);
            if (propertySymbol == null)
            {
                return false;
            }

            var makeImmutableRecordVisitor = new MakeImmutableRecordVisitor(semanticModel, propertySymbol);
            makeImmutableRecordVisitor.Visit(propertyDeclaration.GetRoot());

            return makeImmutableRecordVisitor.IsFound;
        }
    }
}