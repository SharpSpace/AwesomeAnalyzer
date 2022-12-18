using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace AwesomeAnalyzer.Test
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public static async Task VerifyAnalyzerAsync(LanguageVersion languageVersion, string source, params DiagnosticResult[] expected)
        {
            var test = new TestTarget<TAnalyzer, TCodeFix>(languageVersion)
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        public static async Task VerifyCodeFixAsync(LanguageVersion languageVersion, string source, string fixedSource)
            => await VerifyCodeFixAsync(languageVersion, source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        public static async Task VerifyCodeFixAsync(LanguageVersion languageVersion, string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new TestTarget<TAnalyzer, TCodeFix>(languageVersion)
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }

    public class TestTarget<TAnalyzer, TCodeFix> : CodeFixTest<MSTestVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        private readonly LanguageVersion _languageVersion;

        public TestTarget(LanguageVersion languageVersion)
        {
            _languageVersion = languageVersion;
            SolutionTransforms.Add((solution, projectId) =>
            {
                var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings)
                );
                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                return solution;
            });
        }

        protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            => new[] { new TCodeFix() };

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => new[] { new TAnalyzer() };

        protected override string DefaultFileExt => "cs";

        public override string Language => LanguageNames.CSharp;

        public override Type SyntaxKindType => typeof(SyntaxKind);

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

        protected override ParseOptions CreateParseOptions()
            => new CSharpParseOptions(_languageVersion, DocumentationMode.Diagnose);
    }
}
