using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.MakeAsyncAnalyzer,
    AwesomeAnalyzer.MakeAsyncCodeFixProvider>;

namespace AwesomeAnalyzer.Test
{
    [TestClass]
    public class MakeAsyncTest
    {
        [TestMethod]
        public async Task Test1_No_Diagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
class Program 
{ 
    void Method(){}
}");
        }

        [TestMethod]
        public async Task Test2_No_Diagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
class Program 
{ 
    async void MethodAsync(){}
}");
        }

        [TestMethod]
        public async Task Test3_No_Diagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
namespace MyNamespace
{
    using System.Threading.Tasks;
    class Program
    {
        public async Task MethodAsync() {}
    }
}");
        }

        [TestMethod]
        public async Task Test4_No_Diagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
namespace MyNamespace
{
    using System.Threading.Tasks;
    class Program
    {
        public async Task<string> MethodAsync() { return ""Async""; }
    }
}");
        }

        [TestMethod]
        public async Task Test1_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
class Program 
{ 
    void MethodAsync()
    {
    }
}",
                VerifyCS.Diagnostic().WithSpan(4, 5, 6, 6),
                fixedSource: @"
class Program 
{ 
    void Method()
    {
    }
}");
        }

        [TestMethod]
        public async Task Test2_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
class Program 
{ 
    string MethodAsync()
    {
        return ""Async"";
    }
}",
                VerifyCS.Diagnostic().WithSpan(4, 5, 7, 6),
                fixedSource: @"
class Program 
{ 
    string Method()
    {
        return ""Async"";
    }
}");
        }
    }
}