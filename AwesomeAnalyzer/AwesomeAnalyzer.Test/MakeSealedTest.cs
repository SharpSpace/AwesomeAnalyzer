using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.MakeSealedAnalyzer,
    AwesomeAnalyzer.MakeSealedCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public class MakeSealedTest
{
    [TestMethod]
    public async Task ClassTest_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"sealed class Program {}");
    }

    [TestMethod]
    public async Task PublicClassTest_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"public sealed class Program {}");
    }

    [TestMethod]
    public async Task PublicStaticClassTest_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"public static class Program {}");
    }

    [TestMethod]
    public async Task ClassTest_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            class {|JJ0001:Program|}
            { }
            """,
            fixedSource: """
            sealed class Program
            { }
            """);
    }

    [TestMethod]
    public async Task PublicClassTest_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            public class {|JJ0001:Program|}
            { }
            """,
            fixedSource: """
            public sealed class Program
            { }
            """);
    }

    [TestMethod]
    public async Task InternalClassTest_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            internal class {|JJ0001:Program|}
            { }
            """,
            fixedSource: """
            internal sealed class Program
            { }
            """);
    }

    [TestMethod]
    public async Task Private2ClassTest_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            namespace Sample
            {
                internal class {|JJ0001:Program|} { }
                internal class {|JJ0001:Program2|} { }
            }
            """,
            fixedSource: """
            namespace Sample
            {
                internal sealed class Program { }
                internal sealed class Program2 { }
            }
            """);
    }

    [TestMethod]
    public async Task ClassBaseClassTest_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            namespace Sample
            {
                public class Program { }
                public class {|JJ0001:Program2|}: Program { }
            }
            """,
            fixedSource: """
            namespace Sample
            {
                public class Program { }
                public sealed class Program2: Program { }
            }
            """);
    }

    [TestMethod]
    public async Task ClassBaseClassDifferentNamespacesTest_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            namespace Sample
            {
                public class Program { }
                public class {|JJ0001:Program2|}: Program { }
            }
            namespace Sample.Test
            {
                public class {|JJ0001:Program|} { }
            }
            """,
            fixedSource: """
            namespace Sample
            {
                public class Program { }
                public sealed class Program2: Program { }
            }
            namespace Sample.Test
            {
                public sealed class Program { }
            }
            """);
    }

}