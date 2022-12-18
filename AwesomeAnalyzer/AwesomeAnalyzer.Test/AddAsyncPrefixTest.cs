using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.AddAsyncAnalyzer,
    AwesomeAnalyzer.AddAsyncPrefixCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class AddAsyncPrefixTest
{
    [TestMethod]
    public async Task TestMissingAsync1_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;

namespace Test
{
    class Program 
    { 
        private async Task B() => await CAsync();

        private async Task CAsync() => await Task.CompletedTask;
    }
}");
    }

    [TestMethod]
    public async Task TestMissingAsync2_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
namespace Test
{
    class Program 
    { 
        private string B() => nameof(B);
    }
}");
    }

    [TestMethod]
    public async Task TestMissingAsync3_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;

namespace Test
{
    class Program 
    { 
        private async Task B() => await CAsync().ConfigureAwait(false);

        private async Task CAsync() => await Task.CompletedTask;
    }
}");
    }

    [TestMethod]
    public async Task TestMissingAsync4_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(@"
namespace MyNamespace
{
    using System.Threading.Tasks;

    class Program
    {
        public Task<string> MethodAsync() { return Task.FromResult(""Async""); }
    }
}");
    }

    [TestMethod]
    public async Task TestMissingAsync1_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(@"
using System.Threading.Tasks;

namespace Test
{
    class Program 
    { 
        private async Task B() => await {|JJ0102:C|}();

        private async Task C() => await Task.CompletedTask;
    }
}",
            fixedSource: @"
using System.Threading.Tasks;

namespace Test
{
    class Program 
    { 
        private async Task B() => await CAsync();

        private async Task CAsync() => await Task.CompletedTask;
    }
}");
    }

    [TestMethod]
    public async Task TestMissingAsync2_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Threading.Tasks;

namespace Test
{
    class Program 
    { 
        private async Task C(Func<Task<string>> func) => await {|JJ0102:func|}();
    }
}",
            fixedSource: @"
using System;
using System.Threading.Tasks;

namespace Test
{
    class Program 
    { 
        private async Task C(Func<Task<string>> funcAsync) => await {|JJ0102:funcAsync()|};
    }
}");
    }
}