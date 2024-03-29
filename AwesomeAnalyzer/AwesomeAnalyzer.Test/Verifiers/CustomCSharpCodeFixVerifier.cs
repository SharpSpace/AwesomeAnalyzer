﻿namespace AwesomeAnalyzer.Test;

public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
where TAnalyzer : DiagnosticAnalyzer, new()
where TCodeFix : CodeFixProvider, new()
{
    public static Task VerifyAnalyzerAsync(
        LanguageVersion languageVersion,
        string source,
        params DiagnosticResult[] expected)
    {
        var test = new TestTarget<TAnalyzer, TCodeFix>(languageVersion)
        {
            TestCode = source,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(CancellationToken.None);
    }

    public static Task VerifyCodeFixAsync(LanguageVersion languageVersion, string source, string fixedSource)
        => VerifyCodeFixAsync(languageVersion, source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

    public static Task VerifyCodeFixAsync(
        LanguageVersion languageVersion,
        string source,
        DiagnosticResult[] expected,
        string fixedSource)
    {
        var test = new TestTarget<TAnalyzer, TCodeFix>(languageVersion)
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(CancellationToken.None);
    }
}