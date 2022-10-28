using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.MakeSealedAnalyzer,
    AwesomeAnalyzer.MakeSealedCodeFixProvider>;

namespace AwesomeAnalyzer.Test
{
    [TestClass]
    public class MakeSealedTest
    {
        [TestMethod]
        public async Task ClassTest_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"sealed class Program {}");
        }
        
        [TestMethod]
        public async Task PublicClassTest_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"public sealed class Program {}");
        }

        [TestMethod]
        public async Task ClassTest_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                source: @"
class Program
{ }",
                VerifyCS.Diagnostic().WithSpan(2,1,3,4), //DiagnosticResult.EmptyDiagnosticResults, // Verify.Diagnostic("AwesomeAnalyzer").WithLocation(11, 15).WithArguments("TypeName"),
                fixedSource: @"
sealed class Program
{ }");
        }

        [TestMethod]
        public async Task PublicClassTest_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                source: @"
public class Program
{ }",
                VerifyCS.Diagnostic().WithSpan(2, 1, 3, 4),
                fixedSource: @"
public sealed class Program
{ }");
        }

        [TestMethod]
        public async Task InternalClassTest_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                source: @"
internal class Program
{ }",
                VerifyCS.Diagnostic().WithSpan(2, 1, 3, 4),
                fixedSource: @"
internal sealed class Program
{ }");
        }

        [TestMethod]
        public async Task Private2ClassTest_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                source: @"
namespace Sample
{
    internal class Program { }
    internal class Program2 { }
}",
                new []
                {
                    VerifyCS.Diagnostic().WithSpan(4, 5, 4, 31),
                    VerifyCS.Diagnostic().WithSpan(5, 5, 5, 32)
                },
                fixedSource: @"
namespace Sample
{
    internal sealed class Program { }
    internal sealed class Program2 { }
}");
        }

        [TestMethod]
        public async Task ClassBaseClassTest_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                source: @"
namespace Sample
{
    public class Program { }
    public class Program2: Program { }
}",
                new []
                {
                    VerifyCS.Diagnostic().WithSpan(5, 5, 5, 39)
                },
                fixedSource: @"
namespace Sample
{
    public class Program { }
    public sealed class Program2: Program { }
}");
        }

        [TestMethod]
        public async Task ClassBaseClassDiffrentNamespacesTest_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                source: @"
namespace Sample
{
    public class Program { }
    public class Program2: Program { }
}
namespace Sample.Test
{
    public class Program { }
}",
                new[]
                {
                    VerifyCS.Diagnostic().WithSpan(9, 5, 9, 29),
                    VerifyCS.Diagnostic().WithSpan(5, 5, 5, 39)
                },
                fixedSource: @"
namespace Sample
{
    public class Program { }
    public sealed class Program2: Program { }
}
namespace Sample.Test
{
    public sealed class Program { }
}");
        }

    }
}

