namespace AwesomeAnalyzer.Test;

public static partial class VisualBasicCodeRefactoringVerifier<TCodeRefactoring>
    where TCodeRefactoring : CodeRefactoringProvider, new()
{
    public sealed class Test : VisualBasicCodeRefactoringTest<TCodeRefactoring, XUnitVerifier> { }
}