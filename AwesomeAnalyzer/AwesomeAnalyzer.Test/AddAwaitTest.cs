using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.AddAwaitAnalyzer,
    AwesomeAnalyzer.AddAwaitCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class AddAwaitTest
{
    [TestMethod]
    public async Task TestMissingAwait1_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            using System.Threading.Tasks;

            namespace Test
            {
                class Program 
                { 
                    private async Task B() => await CAsync();

                    private async Task CAsync() => await Task.CompletedTask;
                }
            }
            """);
    }

    [TestMethod]
    public async Task TestMissingAwait2_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
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
            """);
    }


    [TestMethod]
    public async Task TestMissingAwait3_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            namespace MyNamespace
            {
                using System.Threading.Tasks;

                interface Program 
                { 
                    Task MethodAsync();
                }
            }
            """);
    }

    [TestMethod]
    public async Task TestMissingAwait4_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            namespace MyNamespace
            {
                using System.Threading.Tasks;

                class Program
                {
                    public Task<string> MethodAsync() { return Task.FromResult("Async"); }
                }
            }
            """);
    }

    [TestMethod]
    public async Task TestMissingAwait5_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
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
            """);
    }

    [TestMethod]
    public async Task TestMissingAwait1_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
            """);
    }
}