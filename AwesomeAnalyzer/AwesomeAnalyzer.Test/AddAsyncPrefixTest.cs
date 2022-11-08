using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.AddAsyncAnalyzer,
    AwesomeAnalyzer.AddAsyncPrefixCodeFixProvider>;

namespace AwesomeAnalyzer.Test
{
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
    }
}