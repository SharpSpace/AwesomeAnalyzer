using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassAsStructCodeFixProvider))]
    [Shared]
    public sealed class ClassAsStructCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.Rule0200ClassAsStruct.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.ClassAsStructCodeFixTitle,
                    createChangedDocument: async token =>
                    {
                        var root = await context.Document.GetSyntaxRootAsync(token).ConfigureAwait(false);
                        var diagnostic = context.Diagnostics.First();
                        var diagnosticSpan = diagnostic.Location.SourceSpan;

                        var classDeclaration = root.FindToken(diagnosticSpan.Start)
                            .Parent.AncestorsAndSelf()
                            .OfType<ClassDeclarationSyntax>()
                            .First();

                        // Create a struct declaration from the class declaration
                        var structDeclaration = SyntaxFactory.StructDeclaration(
                            classDeclaration.AttributeLists,
                            classDeclaration.Modifiers,
                            SyntaxFactory.Token(SyntaxKind.StructKeyword).WithTriviaFrom(
                                classDeclaration.ChildTokens().First(t => t.IsKind(SyntaxKind.ClassKeyword))
                            ),
                            classDeclaration.Identifier,
                            classDeclaration.TypeParameterList,
                            classDeclaration.BaseList,
                            classDeclaration.ConstraintClauses,
                            classDeclaration.OpenBraceToken,
                            classDeclaration.Members,
                            classDeclaration.CloseBraceToken,
                            classDeclaration.SemicolonToken
                        ).WithTriviaFrom(classDeclaration);

                        var newRoot = root.ReplaceNode(classDeclaration, structDeclaration);
                        return context.Document.WithSyntaxRoot(newRoot);
                    },
                    equivalenceKey: nameof(CodeFixResources.ClassAsStructCodeFixTitle)
                ),
                context.Diagnostics
            );

            return Task.CompletedTask;
        }
    }
}
