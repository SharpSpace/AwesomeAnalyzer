using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.SortAnalyzer,
    AwesomeAnalyzer.SortAndOrderCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class SortTest
{
    [TestMethod]
    public async Task Test_NoDiagnostic1()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
                sealed class Program
                {
                    void A() { }

                    void B() { }

                    void C() { }
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic2()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
                class Program
                {
                    public int Method()
                    {
                        return 0;
                    }
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;

                namespace DevTriggerServer;

                public class Program
                {
                    private static void Main(string[] args)
                    {
                    }
                }

                public class Hub
                {
                    private readonly string {|JJ1002:_mediator|};

                    private static string {|JJ1002:_z|};

                    public Hub()
                    {
                        Send();
                    }

                    private async Task Send()
                    {
                    }
                }

                public class Ping
                {
                    public string {|JJ1006:Message|} { get; set; }

                    public string B { get; set; }

                    public string {|JJ1006:A|} { get; set; }
                }

                public class Pong
                {
                    public DateTime DateTime { get; set; }
                }

                public class PingHandler
                {
                    public Task<Pong> Handle(Ping request)
                    {
                        //await DoPong(); // Whatever DoPong does
                        return Task.FromResult(new Pong { DateTime = DateTime.MaxValue });
                    }
                }

                //public class Pong1 : INotificationHandler<Ping>
                //{
                //    public Task Handle(Ping notification)
                //    {
                //        Debug.WriteLine("Pong 1");
                //        return Task.CompletedTask;
                //    }
                //}
                """,
            """
                using System;
                using System.Diagnostics;
                using System.Threading.Tasks;

                namespace DevTriggerServer;

                public class Program
                {
                    private static void Main(string[] args)
                    {
                    }
                }

                public class Hub
                {
                    private static string _z;

                    private readonly string _mediator;

                    public Hub()
                    {
                        Send();
                    }

                    private async Task Send()
                    {
                    }
                }

                public class Ping
                {
                    public string A { get; set; }

                    public string B { get; set; }

                    public string Message { get; set; }
                }

                public class Pong
                {
                    public DateTime DateTime { get; set; }
                }

                public class PingHandler
                {
                    public Task<Pong> Handle(Ping request)
                    {
                        //await DoPong(); // Whatever DoPong does
                        return Task.FromResult(new Pong { DateTime = DateTime.MaxValue });
                    }
                }

                //public class Pong1 : INotificationHandler<Ping>
                //{
                //    public Task Handle(Ping notification)
                //    {
                //        Debug.WriteLine("Pong 1");
                //        return Task.CompletedTask;
                //    }
                //}
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSort_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                sealed class Program
                {
                    void {|JJ1003:C|}() { }

                    void B() { }

                    void {|JJ1003:A|}() { }
                }
                """,
            """
                sealed class Program
                {
                    void A() { }

                    void B() { }

                    void C() { }
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSort_Diagnostic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                using System;

                sealed class Program
                {
                    public string {|JJ1007:F|} { get; set; }

                    private static void {|JJ1004:D|}() { }

                    public event OnEvent {|JJ1013:Event|};

                    public delegate void {|JJ1011:OnEvent|}(object sender, EventArgs args);

                    private string {|JJ1002:_a|};

                    void {|JJ1004:A|}() { }

                    private string {|JJ1002:_b|};

                    void {|JJ1004:B|}()
                    {
                        var a = "a";
                    }

                    private enum {|JJ1009:MyEnum|}
                    {
                        a, b, c
                    }

                    private string {|JJ1002:_c|};

                    public {|JJ1005:Program|}() { }

                    void C() { }
                }
                """,
            """
                using System;

                sealed class Program
                {
                    private string _a;

                    private string _b;

                    private string _c;

                    public Program() { }

                    public delegate void OnEvent(object sender, EventArgs args);

                    public event OnEvent Event;

                    private enum MyEnum
                    {
                        a, b, c
                    }

                    public string F { get; set; }

                    private static void D() { }

                    void A() { }

                    void B()
                    {
                        var a = "a";
                    }

                    void C() { }
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSort_Diagnostic3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                sealed class Program
                {
                    private string {|JJ1001:_b|};

                    private string {|JJ1001:_a|};
                }
                """,
            """
                sealed class Program
                {
                    private string _a;

                    private string _b;
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSort_Diagnostic4()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                sealed class Program
                {
                    void {|JJ1003:C|}()
                    {
                    }

                    void B()
                    {
                        var a = "a";
                    }

                    void {|JJ1003:A|}() { }
                }
                """,
            """
                sealed class Program
                {
                    void A() { }

                    void B()
                    {
                        var a = "a";
                    }

                    void C()
                    {
                    }
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSort_Diagnostic5()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                sealed class Program
                {
                    private string {|JJ1001:_b|};

                    private string {|JJ1001:_a|};
                }

                sealed class Program2
                {
                    private string {|JJ1001:_b|};

                    private string {|JJ1001:_a|};
                }
                """,
            """
                sealed class Program
                {
                    private string _a;

                    private string _b;
                }

                sealed class Program2
                {
                    private string _a;

                    private string _b;
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSort_Diagnostic6()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                class Program
                {
                    private readonly string {|JJ1002:_a|};

                    private static string {|JJ1002:_b|};
                }
                """,
            """
                class Program
                {
                    private static string _b;

                    private readonly string _a;
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSortMember_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                sealed class Program
                {
                    private const string {|JJ1002:_d|} = "Const";

                    public const string {|JJ1002:_e|} = "Const";
                }
                """,
            """
                sealed class Program
                {
                    public const string _e = "Const";

                    private const string _d = "Const";
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSortMember_Diagnostic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                sealed class Program
                {
                    public string {|JJ1002:_c|};

                    private const string {|JJ1002:_d|} = "Const";

                    public const string {|JJ1002:_e|} = "Const";

                    private readonly string {|JJ1002:_b|};

                    public readonly string {|JJ1002:_f|};

                    public static string {|JJ1002:_g|};

                    private string _a;
                }
                """,
            """
                sealed class Program
                {
                    public const string _e = "Const";

                    public static string _g;

                    public readonly string _f;

                    public string _c;

                    private const string _d = "Const";

                    private readonly string _b;

                    private string _a;
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSortMember_Diagnostic3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                namespace AwesomeAnalyzer.Test
                {
                    sealed class Program
                    {
                        private const string {|JJ1002:_d|} = "Const";

                        public const string {|JJ1002:_e|} = "Const";
                    }
                }
                """,
            """
                namespace AwesomeAnalyzer.Test
                {
                    sealed class Program
                    {
                        public const string _e = "Const";

                        private const string _d = "Const";
                    }
                }
                """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSortProperty_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
                namespace AwesomeAnalyzer.Test
                {
                    sealed class Program
                    {
                        public string {|JJ1006:C|} { get; set; }

                        public int B { get; set; }

                        public bool {|JJ1006:A|} { get; set; }
                    }
                }
                """,
            """
                namespace AwesomeAnalyzer.Test
                {
                    sealed class Program
                    {
                        public bool A { get; set; }

                        public int B { get; set; }

                        public string C { get; set; }
                    }
                }
                """
            )
            .ConfigureAwait(false);
    }
}