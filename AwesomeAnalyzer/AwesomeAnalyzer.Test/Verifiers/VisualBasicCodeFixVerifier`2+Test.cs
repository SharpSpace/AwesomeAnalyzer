namespace AwesomeAnalyzer.Test;

public static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public sealed class Test : VisualBasicCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier> { }
}