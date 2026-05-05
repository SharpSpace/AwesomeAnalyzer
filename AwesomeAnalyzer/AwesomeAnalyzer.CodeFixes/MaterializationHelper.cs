using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AwesomeAnalyzer
{
    /// <summary>
    /// Chooses the most performance-appropriate LINQ materialization method for a local
    /// variable that is being enumerated multiple times, and produces the necessary
    /// syntax edits.
    ///
    /// Decision logic (in priority order):
    /// 1. <c>ToHashSet()</c> — when every observable usage of the variable is a LINQ
    ///    <c>Contains()</c> call.  <c>HashSet&lt;T&gt;</c> turns those from O(n) into O(1).
    ///    Requires the target compilation to expose <c>Enumerable.ToHashSet</c> (available
    ///    in .NET Core 2.0+, .NET Standard 2.1+ and .NET 5+; skipped otherwise).
    /// 2. <c>ToList()</c>  — when the variable is accessed by index ([i]) or through a
    ///    mutating <c>List&lt;T&gt;</c> method (Add, Remove, …).
    /// 3. <c>ToArray()</c> — default for all other read-only enumeration patterns.
    ///    An array has less overhead than <c>List&lt;T&gt;</c> (no internal capacity buffer).
    /// </summary>
    internal static class MaterializationHelper
    {
        public enum Method { ToArray, ToList, ToHashSet }

        /// <summary>
        /// Analyses how <paramref name="variable"/> is used inside its containing block and
        /// returns the materialization method that maximises runtime performance.
        /// </summary>
        public static Method ChooseBestMethod(
            VariableDeclaratorSyntax variable,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // Walk up to the containing block via explicit type checks.
            var localDeclaration = variable.Parent?.Parent as LocalDeclarationStatementSyntax;
            var containingBlock = localDeclaration?.Parent;
            if (containingBlock == null) return Method.ToArray;

            var symbol = semanticModel.GetDeclaredSymbol(variable, cancellationToken);
            if (symbol == null) return Method.ToArray;

            bool requiresList = false;
            bool hasAnyUsage = false;
            bool allInvocationsAreContains = true;  // optimistic; cleared on first non-Contains usage

            foreach (var node in containingBlock.DescendantNodes())
            {
                if (!(node is IdentifierNameSyntax identifier)) continue;

                var resolvedSymbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
                if (!SymbolEqualityComparer.Default.Equals(resolvedSymbol, symbol)) continue;

                // Indexed access (e.g. items[0]) — only List<T> has an indexer among our choices.
                if (identifier.Parent is ElementAccessExpressionSyntax elem && elem.Expression == identifier)
                {
                    requiresList = true;
                    break;
                }

                if (identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression == identifier)
                {
                    if (memberAccess.Parent is InvocationExpressionSyntax invocation)
                    {
                        var methodSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
                        if (methodSymbol != null)
                        {
                            // Mutating List<T> methods — require ToList().
                            if (IsMutatingListMethod(methodSymbol))
                            {
                                requiresList = true;
                                break;
                            }

                            hasAnyUsage = true;

                            // Track whether this invocation is a LINQ Contains() call.
                            if (!IsLinqContains(methodSymbol))
                            {
                                allInvocationsAreContains = false;
                            }
                        }
                    }
                    else
                    {
                        // Property access (e.g. .Count property on ICollection) — not a Contains invocation.
                        hasAnyUsage = true;
                        allInvocationsAreContains = false;
                    }
                }
                else
                {
                    // Other usage (e.g. foreach, passing as argument, assignment) — not Contains.
                    hasAnyUsage = true;
                    allInvocationsAreContains = false;
                }
            }

            if (requiresList)
            {
                return Method.ToList;
            }

            // Only suggest ToHashSet when the target compilation actually provides Enumerable.ToHashSet.
            // The method is available in .NET Core 2.0+, .NET Standard 2.1+, and .NET 5+,
            // but NOT in .NET Standard 2.0 or .NET Framework 4.x.
            if (hasAnyUsage && allInvocationsAreContains && IsToHashSetAvailable(semanticModel.Compilation))
            {
                return Method.ToHashSet;
            }

            return Method.ToArray;
        }

        /// <summary>Returns the LINQ extension method name for the chosen materialization.</summary>
        public static string GetMethodName(Method method)
        {
            switch (method)
            {
                case Method.ToHashSet: return "ToHashSet";
                case Method.ToList: return "ToList";
                default: return "ToArray";
            }
        }

        /// <summary>Returns a human-readable title suitable for a code-fix action.</summary>
        public static string GetFixTitle(Method method)
        {
            switch (method)
            {
                case Method.ToHashSet: return "Materialize as hash set";
                case Method.ToList: return "Materialize as list";
                default: return "Materialize as array";
            }
        }

        /// <summary>
        /// Returns true when <paramref name="expression"/> has lower syntactic precedence than
        /// member-access, meaning it must be parenthesized before appending <c>.Method()</c>.
        /// </summary>
        public static bool NeedsParentheses(ExpressionSyntax expression)
        {
            return expression is ConditionalExpressionSyntax ||
                   expression is BinaryExpressionSyntax ||
                   expression is AssignmentExpressionSyntax ||
                   expression is AwaitExpressionSyntax ||
                   expression is CastExpressionSyntax ||
                   expression is ThrowExpressionSyntax ||
                   expression is LambdaExpressionSyntax ||
                   expression is QueryExpressionSyntax;
        }

        /// <summary>
        /// Builds a <c>target.MethodName()</c> invocation expression, optionally parenthesizing
        /// <paramref name="initializerExpression"/> first when precedence requires it.
        /// </summary>
        public static InvocationExpressionSyntax BuildMaterializationInvocation(
            ExpressionSyntax initializerExpression,
            string methodName)
        {
            ExpressionSyntax target;
            if (NeedsParentheses(initializerExpression))
            {
                target = SyntaxFactory.ParenthesizedExpression(
                    initializerExpression.WithoutTrivia()
                ).WithTriviaFrom(initializerExpression);
            }
            else
            {
                target = initializerExpression;
            }

            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    target.WithoutTrailingTrivia(),
                    SyntaxFactory.IdentifierName(methodName)
                )
            ).WithTrailingTrivia(initializerExpression.GetTrailingTrivia());
        }

        /// <summary>
        /// Ensures <c>using System.Linq;</c> is present in the compilation unit of
        /// <paramref name="document"/>, adding it when missing.
        /// </summary>
        public static async Task<Document> EnsureUsingSystemLinqAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!(root is CompilationUnitSyntax compilationUnit)) return document;

            bool alreadyPresent = compilationUnit.Usings.Any(u =>
                u.Alias == null &&
                !u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) &&
                string.Equals(u.Name?.ToFullString().Trim(), "System.Linq", StringComparison.Ordinal)
            );
            if (alreadyPresent) return document;

            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName("System"),
                    SyntaxFactory.IdentifierName("Linq")
                ).WithLeadingTrivia(SyntaxFactory.Space)
            ).WithTrailingTrivia(SyntaxFactory.ElasticLineFeed);

            var newRoot = compilationUnit.AddUsings(usingDirective);
            return document.WithSyntaxRoot(newRoot);
        }

        private static bool IsToHashSetAvailable(Compilation compilation)
        {
            var enumerableType = compilation.GetTypeByMetadataName("System.Linq.Enumerable");
            return enumerableType != null && enumerableType.GetMembers("ToHashSet").Length > 0;
        }

        private static bool IsLinqContains(IMethodSymbol method)
        {
            return string.Equals(method.Name, "Contains", StringComparison.Ordinal) &&
                   string.Equals(
                       method.ContainingType?.ContainingNamespace?.ToDisplayString(),
                       "System.Linq",
                       StringComparison.Ordinal
                   );
        }

        private static bool IsMutatingListMethod(IMethodSymbol method)
        {
            // Only flag direct instance methods on List<T>-like types — not LINQ projections.
            var ns = method.ContainingType?.ContainingNamespace?.ToDisplayString();
            if (string.Equals(ns, "System.Linq", StringComparison.Ordinal)) return false;

            switch (method.Name)
            {
                case "Add":
                case "AddRange":
                case "Clear":
                case "Insert":
                case "InsertRange":
                case "Remove":
                case "RemoveAll":
                case "RemoveAt":
                case "RemoveRange":
                case "Reverse":
                case "Sort":
                    return true;
                default:
                    return false;
            }
        }
    }
}
