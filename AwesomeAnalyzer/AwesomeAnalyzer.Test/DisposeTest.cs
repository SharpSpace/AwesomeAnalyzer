using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.DisposedAnalyzer,
    AwesomeAnalyzer.DisposedCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class DisposeTest
{
    [TestMethod]
    public async Task Test_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.IO;

            namespace MyNamespace
            {
                public sealed class Program
                {
                    private void A()
                    {
                        [|var reader = new StreamReader("")|];
                    }
                }
            }
            """,
            """
            using System.IO;

            namespace MyNamespace
            {
                public sealed class Program
                {
                    private void A()
                    {
                        using var reader = new StreamReader("");
                    }
                }
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Diagnostic2() =>
    await VerifyCS.VerifyCodeFixAsync(
        LanguageVersion.CSharp8,
        """
        using System.IO;

        namespace MyNamespace
        {
            public sealed class Program
            {
                private void A()
                {
                    [|var reader = new StreamReader("")|];
                }
            }
        }
        """,
        """
        using System.IO;

        namespace MyNamespace
        {
            public sealed class Program
            {
                private void A()
                {
                    using (var reader = new StreamReader(""))
                    {
                    }
                }
            }
        }
        """
    )
    .ConfigureAwait(false);

    [TestMethod]
    public async Task Test_Diagnostic3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            LanguageVersion.CSharp8,
            """
            using System.IO;

            namespace MyNamespace
            {
                public sealed class Program
                {
                    private void A()
                    {
                        var b = string.Empty;
                        [|var reader = new StreamReader("")|];
                        var a = string.Empty;

                        var c = string.Empty;
                    }
                }
            }
            """,
            fixedSource: """
            using System.IO;

            namespace MyNamespace
            {
                public sealed class Program
                {
                    private void A()
                    {
                        var b = string.Empty;
                        using (var reader = new StreamReader(""))
                        {
                            var a = string.Empty;

                            var c = string.Empty;
                        }
                    }
                }
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic1() =>
    await VerifyCS.VerifyAnalyzerAsync(
        """
        using System.IO;

        namespace MyNamespace
        {
            public sealed class Program
            {
                private void A()
                {
                    using var reader = new StreamReader("");
                }
            }
        }
        """
    )
    .ConfigureAwait(false);

    [TestMethod]
    public async Task Test_NoDiagnostic2() =>
    await VerifyCS.VerifyAnalyzerAsync(
        """
        using System.IO;

        namespace MyNamespace
        {
            public sealed class Program
            {
                private void A()
                {
                    var reader = new StreamReader("");
                    reader.Dispose();
                }
            }
        }
        """
    )
    .ConfigureAwait(false);
}