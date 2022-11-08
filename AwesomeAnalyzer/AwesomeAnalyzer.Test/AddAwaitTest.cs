using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.AddAwaitAnalyzer,
    AwesomeAnalyzer.AddAwaitCodeFixProvider>;

namespace AwesomeAnalyzer.Test
{
    [TestClass]
    public sealed class AddAwaitTest
    {
        [TestMethod]
        public async Task TestMissingAwait1_NoDiagnostic()
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
        public async Task TestMissingAwait1_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System.Threading.Tasks;

namespace Test
{
    class Program 
    { 
        private void B() => {|JJ0101:CAsync()|};

        private async Task CAsync() => await Task.CompletedTask;
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