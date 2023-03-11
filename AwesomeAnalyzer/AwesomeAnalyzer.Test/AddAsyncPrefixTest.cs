using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.AddAsyncAnalyzer,
    AwesomeAnalyzer.AddAsyncPrefixCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class AddAsyncPrefixTest
{
    [TestMethod]
    public async Task TestMissingAsync_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private async Task B() => await {|JJ0102:C|}();

                    private async Task C() => await Task.CompletedTask;
                }
            }
            """,
            fixedSource: """
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private async Task B() => await CAsync();

                    private async Task CAsync() => await Task.CompletedTask;
                }
            }
            """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAsync_Diagnostic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private async Task C(Func<Task<string>> func) => await {|JJ0102:func|}();
                }
            }
            """,
            fixedSource: """
            using System;
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private async Task C(Func<Task<string>> funcAsync) => await {|JJ0102:funcAsync()|};
                }
            }
            """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAsync_Diagnostic3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private async Task B() => await {|JJ0102:C|}().ConfigureAwait(false);

                    private async Task C() => await Task.CompletedTask;
                }
            }
            """,
            fixedSource: """
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private async Task B() => await CAsync().ConfigureAwait(false);

                    private async Task CAsync() => await Task.CompletedTask;
                }
            }
            """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAsync_NoDiagnostic1()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private async Task B() => await CAsync();

                    private async Task CAsync() => await Task.CompletedTask;
                }
            }
            """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAsync_NoDiagnostic2()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace Test
            {
                class Program
                {
                    private string B() => nameof(B);
                }
            }
            """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAsync_NoDiagnostic3()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private async Task B() => await CAsync().ConfigureAwait(false);

                    private async Task CAsync() => await Task.CompletedTask;
                }
            }
            """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAsync_NoDiagnostic4()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace MyNamespace
            {
                using System.Threading.Tasks;

                class Program
                {
                    public Task<string> MethodAsync() { return Task.FromResult("Async"); }
                }
            }
            """
            )
            .ConfigureAwait(false);
    }
}