﻿using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.RenameAsyncAnalyzer,
    AwesomeAnalyzer.RenameAsyncCodeFixProvider>;

namespace AwesomeAnalyzer.Test
{
    [TestClass]
    public sealed class RenameAsyncTest
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
    void [|MethodAsync|]()
    {
    }
}",
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
    string [|MethodAsync|]()
    {
        return ""Async"";
    }
}",
                fixedSource: @"
class Program 
{ 
    string Method()
    {
        return ""Async"";
    }
}");
        }

        [TestMethod]
        public async Task Test3_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
class Program 
{ 
    string [|MethodAsync|]()
    {
        return ""Async"";
    }

    private void B()
    {
        this.MethodAsync();
    }
}",
                fixedSource: @"
class Program 
{ 
    string Method()
    {
        return ""Async"";
    }

    private void B()
    {
        this.Method();
    }
}");
        }

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
        private void B() => [|C()|];

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