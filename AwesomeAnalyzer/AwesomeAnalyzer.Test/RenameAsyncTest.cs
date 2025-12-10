using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.RenameAsyncAnalyzer,
    AwesomeAnalyzer.RenameAsyncCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

public sealed class RenameAsyncTest
{
    [Fact]
    public async Task Test_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                void [|MethodAsync|]()
                {
                }
            }
            """,
            fixedSource: """
            class Program
            {
                void Method()
                {
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
            class Program
            {
                string [|MethodAsync|]()
                {
                    return "Async";
                }
            }
            """,
            fixedSource: """
            class Program
            {
                string Method()
                {
                    return "Async";
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
            class Program
            {
                string [|MethodAsync|]()
                {
                    return "Async";
                }

                private void B()
                {
                    this.MethodAsync();
                }
            }
            """,
            fixedSource: """
            class Program
            {
                string Method()
                {
                    return "Async";
                }

                private void B()
                {
                    this.Method();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic1()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            class Program
            {
                void Method(){}
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic2()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            class Program
            {
                async void MethodAsync(){}
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic3()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace MyNamespace
            {
                class Program
                {
                    public async Task MethodAsync() {}
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic4()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace MyNamespace
            {
                class Program
                {
                    public async Task<string> MethodAsync() { return "Async"; }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic5()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace MyNamespace
            {
                interface Program
                {
                    Task MethodAsync();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic6()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace MyNamespace
            {
                class Program
                {
                    public Task MethodAsync() => Task.CompletedTask;
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic7()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace MyNamespace
            {
                class Program
                {
                    public Task MethodAsync()
                    {
                        return Task.CompletedTask;
                    }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic8()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace MyNamespace
            {
                class Program
                {
                    public Task<string> MethodAsync()
                    {
                        return Task.FromResult(string.Empty);
                    }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic9()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace MyNamespace
            {
                class Program
                {
                    public ValueTask MethodAsync()
                    {
                        return new ValueTask();
                    }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic10()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace MyNamespace
            {
                class Program
                {
                    public ValueTask MethodAsync()
                    {
                        return new ValueTask(Task.FromResult(string.Empty));
                    }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic11()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            namespace MyNamespace
            {
                class Program
                {
                    public ValueTask<Item> MethodAsync()
                    {
                        return new ValueTask<Item>();
                    }
                }

                sealed class Item { }
            }
            """
        )
;
    }
}