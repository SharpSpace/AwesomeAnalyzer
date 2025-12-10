using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.RemoveAsyncAwaitAnalyzer,
    AwesomeAnalyzer.RemoveAsyncAwaitCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

public sealed class RemoveAsyncAwaitTest
{
    [Fact]
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
            )
;
    }

    [Fact]
    public async Task Test_Diagnostic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task {|JJ0006:Method|}()
                {
                    await Task.CompletedTask
                        .ConfigureAwait(false);
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
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task<string> {|JJ0006:Method|}() => await Task.FromResult(string.Empty);
            }
            """,
            fixedSource:
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public Task<string> Method() => Task.FromResult(string.Empty);
            }
            """
        )
;
    }

    [Fact]
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
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic5()
    {
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
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic6()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task<string> {|JJ0006:Method|}() =>
                    await new ValueTask<string>(string.Empty).ConfigureAwait(false);
            }
            """,
            fixedSource:
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public ValueTask<string> Method() =>
                    new ValueTask<string>(string.Empty);
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic7()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task {|JJ0006:Method|}()
                {
                    await new ValueTask<string>(string.Empty);
                }
            }
            """,
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public ValueTask<string> Method()
                {
                    return new ValueTask<string>(string.Empty);
                }
            }
            """
        )
; // ValueTask.FromResult(string.Empty)
    }

    [Fact]
    public async Task Test_Diagnostic8()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task {|JJ0006:Method|}()
                {
                    if (1 == 1)
                    {
                        return;
                    }
                    
                    await Task.CompletedTask;
                }
            }
            """,
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public Task Method()
                {
                    if (1 == 1)
                    {
                        return Task.CompletedTask;
                    }
                    
                    return Task.CompletedTask;
                }
            }
            """
            )
;
    }

    [Fact]
    public async Task Test_Diagnostic9()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;
        
            class Program
            {
                public Task GetUser() => Task.Run({|JJ0006:async () => await Task.Delay(0)|});
            }
            """,
            """
            using System.Threading.Tasks;

            class Program
            {
                public Task GetUser() => Task.Run(() => Task.Delay(0));
            }
            """
            )
;
    }

    [Fact]
    public async Task TestMissingAsync_NoDiagnostic1()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
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
        )
;
    }

    //[Fact]
    //public async Task TestMissingAsync_NoDiagnostic2()
    //{
    //    await VerifyCS.VerifyAnalyzerAsync(
    //        """
    //        using System.Threading.Tasks;
    //        using Microsoft.VisualStudio.TestTools.UnitTesting;

    //        [TestClass]
    //        public sealed class Tests
    //        {
    //            [Fact]
    //            public async Task Test()
    //            {
    //                await Task.CompletedTask;
    //            }
    //        }
    //        """
    //    )
    //    .ConfigureAwait(false);
    //}
}