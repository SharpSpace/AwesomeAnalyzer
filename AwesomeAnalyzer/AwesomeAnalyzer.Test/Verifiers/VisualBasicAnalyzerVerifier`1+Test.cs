namespace AwesomeAnalyzer.Test;

public static partial class VisualBasicAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public sealed class Test : VisualBasicAnalyzerTest<TAnalyzer, MSTestVerifier>
    {
        public Test() { }
    }
}