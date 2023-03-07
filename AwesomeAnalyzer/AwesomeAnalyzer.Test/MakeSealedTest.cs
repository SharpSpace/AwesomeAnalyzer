using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.MakeSealedAnalyzer,
    AwesomeAnalyzer.MakeSealedCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class MakeSealedTest
{
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
                """
        ).ConfigureAwait(false);
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
                """
        ).ConfigureAwait(false);
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
                """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ClassTest_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"sealed class Program {}").ConfigureAwait(false);
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
                """
        ).ConfigureAwait(false);
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
                """
        ).ConfigureAwait(false);
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
                """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task PublicClassTest_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"public sealed class Program {}").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task PublicPartialClassTest_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
                public partial class {|JJ0001:Program|}
                { }
                """,
            fixedSource: """
                public sealed partial class Program
                { }
                """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task PublicStaticClassTest_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"public static class Program {}").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic1()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public sealed class Program : Program2 {}
            public class Program2 {}
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic2()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace Sample
            {
                public class Program { }
                public sealed class Program2: Program { }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic3()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace Sample
            {
                public class Program
                {
                    public virtual Item Item { get; set; }
                }

                public sealed class Item { }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic4()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace Sample
            {
                public abstract class Program { }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic5()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace Sample
            {
                public class Program { }
            }
            namespace Sample2
            {
                public sealed class Program2: Sample.Program { }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic6() {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace Sample
            {
                internal class Program<T> { }
                internal sealed class Program2<T>: Program<T> { }
            }
            """
        ).ConfigureAwait(false);
    }
}