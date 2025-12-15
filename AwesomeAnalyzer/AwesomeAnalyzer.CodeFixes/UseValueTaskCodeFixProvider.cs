using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseValueTaskCodeFixProvider))]
    [Shared]
    public sealed class UseValueTaskCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.Rule0103UseValueTask.Id
        );

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null) return;

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (methodDeclaration == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.UseValueTaskCodeFixTitle,
                    createChangedDocument: c => ConvertToValueTaskAsync(context.Document, methodDeclaration, c),
                    equivalenceKey: nameof(CodeFixResources.UseValueTaskCodeFixTitle)
                ),
                diagnostic
            );
        }

        private static async Task<Document> ConvertToValueTaskAsync(
            Document document,
            MethodDeclarationSyntax methodDeclaration,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null) return document;

            var returnType = methodDeclaration.ReturnType;
            var typeInfo = semanticModel.GetTypeInfo(returnType, cancellationToken);

            if (typeInfo.Type == null) return document;

            TypeSyntax newReturnType;
            var typeName = typeInfo.Type.ToDisplayString();

            if (typeName == "System.Threading.Tasks.Task")
            {
                // Task -> ValueTask
                newReturnType = SyntaxFactory.IdentifierName("ValueTask");
            }
            else if (typeName.StartsWith("System.Threading.Tasks.Task<"))
            {
                // Task<T> -> ValueTask<T>
                var genericType = (INamedTypeSymbol)typeInfo.Type;
                if (genericType.TypeArguments.Length == 1)
                {
                    var typeArgument = genericType.TypeArguments[0];
                    var typeArgumentSyntax = SyntaxFactory.ParseTypeName(typeArgument.ToDisplayString());
                    newReturnType = SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("ValueTask"),
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(typeArgumentSyntax)
                        )
                    );
                }
                else
                {
                    return document;
                }
            }
            else
            {
                return document;
            }

            newReturnType = newReturnType.WithTriviaFrom(returnType);

            var newMethodDeclaration = methodDeclaration.WithReturnType(newReturnType);

            var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
