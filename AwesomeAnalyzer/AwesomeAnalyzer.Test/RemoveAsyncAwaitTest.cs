using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.RemoveAsyncAwaitAnalyzer,
    AwesomeAnalyzer.RemoveAsyncAwaitCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class RemoveAsyncAwaitTest
{
    [TestMethod]
    public async Task Test_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task {|JJ0006:Method|}()
                {
                    await Task.CompletedTask;
                }
            }
            """,
            fixedSource:
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public Task Method()
                {
                    return Task.CompletedTask;
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Diagnostic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task {|JJ0006:Method|}()
                {
                    await Task.CompletedTask.ConfigureAwait(false);
                }
            }
            """,
            fixedSource:
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public Task Method()
                {
                    return Task.CompletedTask;
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Diagnostic3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task {|JJ0006:Method|}() => await Task.CompletedTask;
            }
            """,
            fixedSource:
            """
            using System.Threading.Tasks;
            
            sealed class Program
            {
                public Task Method() => Task.CompletedTask;
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Diagnostic4()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task {|JJ0006:Method|}() => 
                    await Task.CompletedTask.ConfigureAwait(false);
            }
            """,
            fixedSource:
            """
            using System.Threading.Tasks;
            
            sealed class Program
            {
                public Task Method() => 
                    Task.CompletedTask;
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Diagnostic5() {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task {|JJ0006:Method|}() => 
                    await Task.CompletedTask
                        .ConfigureAwait(false);
            }
            """,
            fixedSource:
            """
            using System.Threading.Tasks;
            
            sealed class Program
            {
                public Task Method() => 
                    Task.CompletedTask;
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestMissingAsync_NoDiagnostic1()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            using System.Threading.Tasks;

            sealed class Program 
            { 
                public async Task Method()
                {
                    var a = await Task.FromResult(string.Empty);
                    await Task.CompletedTask;
                }
            }
            """
        ).ConfigureAwait(false);
    }
}