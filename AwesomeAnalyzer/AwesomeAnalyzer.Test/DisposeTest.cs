using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.DisposedAnalyzer,
    AwesomeAnalyzer.DisposedCodeFixProvider>;

namespace AwesomeAnalyzer.Test
{
    [TestClass]
    public sealed class DisposeTest
    {
        [TestMethod]
        public async Task Test1_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.IO;

namespace MyNamespace
{
    public sealed class Program
    {
        private void A() 
        {
            using var reader = new StreamReader("""");
        }
    }
}");
        }

        [TestMethod]
        public async Task Test2_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.IO;

namespace MyNamespace
{
    public sealed class Program
    {
        private void A() 
        {
            var reader = new StreamReader("""");
            reader.Dispose();
        }
    }
}");
        }

        [TestMethod]
        public async Task Test1_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System.IO;

namespace MyNamespace
{
    public sealed class Program
    {
        private void A() 
        {
            [|var reader = new StreamReader("""")|];
        }
    }
}", fixedSource: @"
using System.IO;

namespace MyNamespace
{
    public sealed class Program
    {
        private void A() 
        {
            using var reader = new StreamReader("""");
        }
    }
}");
        }

        [TestMethod]
        public async Task Test2_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                LanguageVersion.CSharp8, 
                @"
using System.IO;

namespace MyNamespace
{
    public sealed class Program
    {
        private void A()
        {
            [|var reader = new StreamReader("""")|];
        }
    }
}", fixedSource: @"
using System.IO;

namespace MyNamespace
{
    public sealed class Program
    {
        private void A()
        {
            using (var reader = new StreamReader(""""))
            {
            }
        }
    }
}");
        }

        [TestMethod]
        public async Task Test3_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                LanguageVersion.CSharp8,
                @"
using System.IO;

namespace MyNamespace
{
    public sealed class Program
    {
        private void A()
        {
            var b = string.Empty;
            [|var reader = new StreamReader("""")|];
            var a = string.Empty;

            var c = string.Empty;
        }
    }
}", fixedSource: @"
using System.IO;

namespace MyNamespace
{
    public sealed class Program
    {
        private void A()
        {
            var b = string.Empty;
            using (var reader = new StreamReader(""""))
            {
                var a = string.Empty;

                var c = string.Empty;
            }
        }
    }
}");
        }
    }
}

