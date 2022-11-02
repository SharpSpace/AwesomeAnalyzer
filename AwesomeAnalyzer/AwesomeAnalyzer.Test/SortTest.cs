using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.SortAnalyzer,
    AwesomeAnalyzer.SortCodeFixProvider>;


namespace AwesomeAnalyzer.Test
{
    [TestClass]
    public sealed class SortTest
    {
        [TestMethod]
        public async Task Test1_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
sealed class Program
{
    void A() { }

    void B() { }

    void C() { }
}");
        }

        [TestMethod]
        public async Task TestSort1_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
sealed class Program
{
    void {|JJ1003:C|}() { }

    void B() { }

    void {|JJ1003:A|}() { }
}",
                @"
sealed class Program
{
    void A() { }

    void B() { }

    void C() { }
}");
        }

        [TestMethod]
        public async Task TestSort2_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
sealed class Program
{
    private string {|JJ1001:_c|};

    private string _b;

    private string {|JJ1001:_a|};

    public Program() { }

    void {|JJ1003:C|}() { }

    void B()
    {
        var a = ""a"";
    }

    void {|JJ1003:A|}() { }
}",
                @"
sealed class Program
{
    private string _a;

    private string _b;

    private string _c;

    public Program() { }

    void A() { }

    void B()
    {
        var a = ""a"";
    }

    void C() { }
}");
        }

        [TestMethod]
        public async Task TestSort3_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
sealed class Program
{
    private string {|JJ1001:_b|};

    private string {|JJ1001:_a|};
}",
                @"
sealed class Program
{
    private string _a;

    private string _b;
}");
        }

        [TestMethod]
        public async Task TestSort4_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
sealed class Program
{
    void {|JJ1003:C|}()
    {
    }

    void B()
    {
        var a = ""a"";
    }

    void {|JJ1003:A|}() { }
}",
                @"
sealed class Program
{
    void A() { }

    void B()
    {
        var a = ""a"";
    }

    void C()
    {
    }
}");
        }

        [TestMethod]
        public async Task TestSortMember1_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
sealed class Program
{
    private const string {|JJ1001:_d|} = ""Const"";

    public const string {|JJ1001:_e|} = ""Const"";
}",
                @"
sealed class Program
{
    public const string _e = ""Const"";

    private const string _d = ""Const"";
}");
        }

        [TestMethod]
        public async Task TestSortMember2_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
sealed class Program
{
    public string {|JJ1001:_c|};

    private const string _d = ""Const"";

    public const string {|JJ1001:_e|} = ""Const"";

    private readonly string {|JJ1001:_b|};

    public readonly string {|JJ1001:_f|};

    public static string {|JJ1001:_g|};

    private string _a;
}",
                @"
sealed class Program
{
    public const string _e = ""Const"";

    private const string _d = ""Const"";

    public static string _g;

    public readonly string _f;

    public string _c;

    private readonly string _b;

    private string _a;
}");
        }

        [TestMethod]
        public async Task TestSortMember3_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
namespace AwesomeAnalyzer.Test
{
    sealed class Program
    {
        private const string {|JJ1001:_d|} = ""Const"";

        public const string {|JJ1001:_e|} = ""Const"";
    }
}",
                @"
namespace AwesomeAnalyzer.Test
{
    sealed class Program
    {
        public const string _e = ""Const"";

        private const string _d = ""Const"";
    }
}");
        }
    }
}