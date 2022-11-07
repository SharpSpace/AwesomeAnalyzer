using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.DisposedAnalyzer,
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
    }
}

