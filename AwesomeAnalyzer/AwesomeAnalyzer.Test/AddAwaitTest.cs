using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.AddAwaitAnalyzer,
    AwesomeAnalyzer.AddAwaitCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class AddAwaitTest
{
    [TestMethod]
    public async Task TestMissingAwait_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private void B() => {|JJ0101:CAsync|}();

                    private async Task CAsync() => await Task.CompletedTask;
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
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAwait_NoDiagnostic1()
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
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAwait_NoDiagnostic2()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace Test
            {
                class Program
                {
                    private async Task B()
                    {
                        var a = await TestClass.CAsync().ConfigureAwait(false);
                    }
                }

                class TestClass
                {
                    public static async Task<string> CAsync() => await Task.FromResult("Test");
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAwait_NoDiagnostic3()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace MyNamespace
            {
                using System.Threading.Tasks;

                interface Program
                {
                    Task MethodAsync();
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAwait_NoDiagnostic4()
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
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAwait_NoDiagnostic5()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace MyNamespace
            {
                using System.Threading.Tasks;

                class Program
                {
                    public void Method()
                    {
                        _ = Task.FromResult("Async");
                    }
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAwait_NoDiagnostic6()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            namespace MyNamespace
            {
                using System.Threading.Tasks;

                class Program
                {
                    public Program()
                    {
                        Task.FromResult("Async");
                    }
                }
            }
            """
        ).ConfigureAwait(false);
    }
}