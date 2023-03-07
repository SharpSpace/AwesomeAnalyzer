namespace AwesomeAnalyzer.Test;

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
            var compilationOptions = solution.GetProject(projectId)?.CompilationOptions;
            compilationOptions = compilationOptions?.WithSpecificDiagnosticOptions(
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

    //public Task RunAsync(CancellationToken cancellationToken, LanguageVersion languageVersion) {
    //    var parseOptions = new CSharpParseOptions(languageVersion, DocumentationMode.Diagnose);
    //    var document = CreateDocument(TestCode, parseOptions: parseOptions);
    //    var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(new[] { document }, true, cancellationToken).ConfigureAwait(false);
    //    var compilerDiagnostics = await GetCompilerDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
    //    var attempts = analyzerDiagnostics.Length;
    //    for (var i = 0; i < attempts; ++i) {
    //        var actions = new List<CodeAction>();
    //        var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, d) => actions.Add(a), cancellationToken);
    //        await new TCodeFix().RegisterCodeFixesAsync(context).ConfigureAwait(false);
    //        if (!actions.Any()) {
    //            break;
    //        }

    //        document = await ApplyFixAsync(document, actions[0], cancellationToken).ConfigureAwait(false);
    //        analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(new[] { document }, true, cancellationToken).ConfigureAwait(false);
    //    }

    //    var fixedCode = await GetStringFromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
    //    var expected = ExpectedDiagnostics.OrderBy(d => d.Span.Start).ToArray();
    //    var actual = analyzerDiagnostics.Concat(compilerDiagnostics).OrderBy(d => d.Location.SourceSpan.Start).ToArray();
    //    VerifyDiagnosticResults(expected, actual);
    //    Assert.AreEqual(FixedCode.Trim(), fixedCode.Trim());
    //    return Task.CompletedTask;
    //}
}