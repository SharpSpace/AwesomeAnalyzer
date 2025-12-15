using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.UseValueTaskAnalyzer,
    AwesomeAnalyzer.UseValueTaskCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

public sealed class UseValueTaskTest
{
    [Fact]
    public async Task Test_Diagnostic_TaskToValueTask()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task {|JJ0103:GetDataAsync|}()
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
                public async ValueTask GetDataAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """
        );
    }

    [Fact]
    public async Task Test_Diagnostic_TaskTToValueTaskT()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task<int> {|JJ0103:GetNumberAsync|}()
                {
                    await Task.Delay(1);
                    return 42;
                }
            }
            """,
            fixedSource:
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async ValueTask<int> GetNumberAsync()
                {
                    await Task.Delay(1);
                    return 42;
                }
            }
            """
        );
    }

    [Fact]
    public async Task Test_Diagnostic_TaskStringToValueTaskString()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task<string> {|JJ0103:GetTextAsync|}()
                {
                    await Task.Delay(1);
                    return "hello";
                }
            }
            """,
            fixedSource:
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async ValueTask<string> GetTextAsync()
                {
                    await Task.Delay(1);
                    return "hello";
                }
            }
            """
        );
    }

    [Fact]
    public async Task Test_NoDiagnostic_AlreadyValueTask()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async ValueTask GetDataAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """
        );
    }

    [Fact]
    public async Task Test_NoDiagnostic_AlreadyValueTaskT()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public async ValueTask<int> GetNumberAsync()
                {
                    await Task.Delay(1);
                    return 42;
                }
            }
            """
        );
    }

    [Fact]
    public async Task Test_NoDiagnostic_NotAsyncMethod()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            sealed class Program
            {
                public Task GetDataAsync()
                {
                    return Task.CompletedTask;
                }
            }
            """
        );
    }

    [Fact]
    public async Task Test_NoDiagnostic_OverrideMethod()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            class BaseClass
            {
                public virtual async Task GetDataAsync()
                {
                    await Task.CompletedTask;
                }
            }

            sealed class DerivedClass : BaseClass
            {
                public override async Task GetDataAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """
        );
    }

    [Fact]
    public async Task Test_NoDiagnostic_VirtualMethod()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            class BaseClass
            {
                public virtual async Task GetDataAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """
        );
    }

    [Fact]
    public async Task Test_NoDiagnostic_InterfaceImplementation()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Threading.Tasks;

            interface IDataService
            {
                Task GetDataAsync();
            }

            sealed class DataService : IDataService
            {
                public async Task GetDataAsync()
                {
                    await Task.CompletedTask;
                }
            }
            """
        );
    }
}
