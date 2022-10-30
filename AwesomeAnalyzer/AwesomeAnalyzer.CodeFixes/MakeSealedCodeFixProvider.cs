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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace AwesomeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeSealedCodeFixProvider)), Shared]
    public sealed class MakeSealedCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MakeSealedAnalyzer.DiagnosticId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.MakeSealedCodeFixTitle,
                    createChangedDocument: c => MakeConstAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.MakeSealedCodeFixTitle)),
                diagnostic);
        }

        private static async Task<Document> MakeConstAsync(
            Document document,
            ClassDeclarationSyntax localDeclaration,
            CancellationToken cancellationToken
        )
        {
            var firstToken = localDeclaration.GetFirstToken();
            var leadingTrivia = firstToken.LeadingTrivia;
            var trimmedLocal = localDeclaration.ReplaceToken(
            firstToken, 
                firstToken.WithLeadingTrivia(SyntaxTriviaList.Empty)
            );

            var oldClassSource = localDeclaration.GetText().ToString();
            var newClassSource = oldClassSource.Replace("class", "sealed class");

            var oldSource = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).ToFullString();
            var newSource = $"{oldSource.Substring(0, localDeclaration.FullSpan.Start)}{newClassSource}{oldSource.Substring(localDeclaration.FullSpan.End)}";

            return document.WithText(SourceText.From(newSource));


            //var code = trimmedLocal.ToString();
            //var newCode = code.Insert(code.IndexOf("class"), "sealed ");
            //SyntaxFactory.parse

            //SyntaxNode
            //var syntaxToken = SyntaxFactory.ParseStatement("public sealed class Program {}");
            //var test = SyntaxFactory.Token( syntaxToken); // syntaxToken.Parent;
            //trimmedLocal.ReplaceSyntax(syntaxToken);

            //ClassDeclarationSyntax.DeserializeFrom()

            //var constToken = SyntaxFactory.Token(leadingTrivia, SyntaxKind.SealedKeyword, SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker));
            var constToken = SyntaxFactory.Token(leadingTrivia, SyntaxKind.SealedKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space));
            SyntaxTokenList newModifiers;
            if (trimmedLocal.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)))
            {
                newModifiers = trimmedLocal.Modifiers.Insert(1, constToken);
            }
            else
            {
                newModifiers = trimmedLocal.Modifiers.Insert(0, constToken);
            }

            // Produce the new local declaration.
            var newLocal = trimmedLocal.WithModifiers(newModifiers);

            // Add an annotation to format the new local declaration.
            var formattedLocal = newLocal.WithAdditionalAnnotations(Formatter.Annotation);

            // Replace the old local declaration with the new local declaration.
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(localDeclaration, formattedLocal);

            // Return document with transformed tree.
            return document.WithSyntaxRoot(newRoot);


            //// Remove the leading trivia from the local declaration.
            //var firstToken = localDeclaration.GetFirstToken();
            //var leadingTrivia = firstToken.LeadingTrivia;
            //var trimmedLocal = localDeclaration.ReplaceToken(
            //    firstToken, firstToken.WithLeadingTrivia(SyntaxTriviaList.Empty));

            //// Create a const token with the leading trivia.
            //var constToken = SyntaxFactory.Token(leadingTrivia, SyntaxKind.ConstKeyword, SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker));

            //// Insert the const token into the modifiers list, creating a new modifiers list.
            //var newModifiers = trimmedLocal.Modifiers.Insert(0, constToken);

            //// If the type of the declaration is 'var', create a new type name
            //// for the inferred type.
            //var variableDeclaration = localDeclaration.Declaration;
            //var variableTypeName = variableDeclaration.Type;
            //if (variableTypeName.IsVar)
            //{
            //    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            //    // Special case: Ensure that 'var' isn't actually an alias to another type
            //    // (e.g. using var = System.String).
            //    var aliasInfo = semanticModel.GetAliasInfo(variableTypeName, cancellationToken);
            //    if (aliasInfo == null)
            //    {
            //        // Retrieve the type inferred for var.
            //        var type = semanticModel.GetTypeInfo(variableTypeName, cancellationToken).ConvertedType;

            //        // Special case: Ensure that 'var' isn't actually a type named 'var'.
            //        if (type.Name != "var")
            //        {
            //            // Create a new TypeSyntax for the inferred type. Be careful
            //            // to keep any leading and trailing trivia from the var keyword.
            //            var typeName = SyntaxFactory.ParseTypeName(type.ToDisplayString())
            //                .WithLeadingTrivia(variableTypeName.GetLeadingTrivia())
            //                .WithTrailingTrivia(variableTypeName.GetTrailingTrivia());

            //            // Add an annotation to simplify the type name.
            //            var simplifiedTypeName = typeName.WithAdditionalAnnotations(Simplifier.Annotation);

            //            // Replace the type in the variable declaration.
            //            variableDeclaration = variableDeclaration.WithType(simplifiedTypeName);
            //        }
            //    }
            //}

            //// Produce the new local declaration.
            //var newLocal = trimmedLocal.WithModifiers(newModifiers)
            //                           .WithDeclaration(variableDeclaration);

            //// Add an annotation to format the new local declaration.
            //var formattedLocal = newLocal.WithAdditionalAnnotations(Formatter.Annotation);

            //// Replace the old local declaration with the new local declaration.
            //var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            //var newRoot = oldRoot.ReplaceNode(localDeclaration, formattedLocal);

            //// Return document with transformed tree.
            //return document.WithSyntaxRoot(newRoot);
        }
    }
}