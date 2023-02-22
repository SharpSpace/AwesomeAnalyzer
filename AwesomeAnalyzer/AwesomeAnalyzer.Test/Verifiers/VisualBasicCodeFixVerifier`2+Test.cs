namespace AwesomeAnalyzer.Test;

public static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public class Test : VisualBasicCodeFixTest<TAnalyzer, TCodeFix, MSTestVerifier>
    {
    }
}