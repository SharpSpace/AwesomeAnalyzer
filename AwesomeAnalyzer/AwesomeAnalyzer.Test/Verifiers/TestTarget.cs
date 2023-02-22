﻿namespace AwesomeAnalyzer.Test;

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

    public override string Language => LanguageNames.CSharp;

    public override Type SyntaxKindType => typeof(SyntaxKind);

    protected override string DefaultFileExt => "cs";

    protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
        => new[] { new TCodeFix() };

    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        => new[] { new TAnalyzer() };

    protected override CompilationOptions CreateCompilationOptions()
        => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

    protected override ParseOptions CreateParseOptions()
        => new CSharpParseOptions(_languageVersion, DocumentationMode.Diagnose);
}