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
        await VerifyCS.VerifyAnalyzerAsync(
            """
            class Program
            {
                public int A1 { get; set; }

                public int A9 { get; set; }

                public int A10 { get; set; }
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic4()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            class Program
            {
                public int A1 { get; set; }

                public int A9 { get; set; }

                public int A10 { get; set; }

                record AaaProgram
                {
                    public int A { get; set; }

                    public int B { get; set; }
                }
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic5()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            class Program
            {
                public int A1 { get; set; }

                public int A9 { get; set; }

                public int A10 { get; set; }

                class AaaProgram
                {
                    public int A { get; set; }

                    public int B { get; set; }
                }
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic6()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public enum MyEnum
            {
                None
            }

            public class Program
            {
                public string A { get; set; }

                public void Method()
                {
                }
            }

            public enum MyEnum2
            {
                None
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic7()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            class Program
            {
                interface IAaaProgram
                {
                    int D { get; }

                    int E { get; }
                }

                private readonly struct BbbProgram
                {
                    public int F { get; }

                    public int G { get; }
                }

                public int A1 { get; set; }

                public int A9 { get; set; }

                public int A10 { get; set; }

                private readonly struct AaaProgram
                {
                    public int A { get; }

                    public int B { get; }
                }
            }
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
    public async Task TestSort_Diagnostic7()
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
    public async Task TestSort_Diagnostic8()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            namespace AwesomeAnalyzer.Test
            {
                public interface IReportPart
                {
                    string PlaceholderName { get; set; }
                }

                /// <summary>
                /// Header report part base
                /// </summary>
                public class ReportHeaderPartBase : IReportPart
                {
                    public ReportHeaderPartBase()
                    {
                        OriginalPlaceholderName = null;
                    }

                    public string {|JJ1006:PlaceholderName|} { get; set; }

                    public string {|JJ1006:Title|} { get; set; }

                    public string {|JJ1006:OriginalPlaceholderName|} { get; set; }
                }

                /// <summary>
                /// Footer report part base
                /// </summary>
                public class ReportFooterPartBase : IReportPart
                {
                    public ReportFooterPartBase()
                    {
                        OriginalPlaceholderName = null;
                    }

                    public string {|JJ1006:PlaceholderName|} { get; set; }

                    public string {|JJ1006:Title|} { get; set; }

                    public string {|JJ1006:OriginalPlaceholderName|} { get; set; }
                }
            }
            """,
            """
            namespace AwesomeAnalyzer.Test
            {
                public interface IReportPart
                {
                    string PlaceholderName { get; set; }
                }

                /// <summary>
                /// Header report part base
                /// </summary>
                public class ReportHeaderPartBase : IReportPart
                {
                    public ReportHeaderPartBase()
                    {
                        OriginalPlaceholderName = null;
                    }

                    public string OriginalPlaceholderName { get; set; }

                    public string PlaceholderName { get; set; }

                    public string Title { get; set; }
                }

                /// <summary>
                /// Footer report part base
                /// </summary>
                public class ReportFooterPartBase : IReportPart
                {
                    public ReportFooterPartBase()
                    {
                        OriginalPlaceholderName = null;
                    }

                    public string OriginalPlaceholderName { get; set; }

                    public string PlaceholderName { get; set; }

                    public string Title { get; set; }
                }
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSort_Diagnostic9()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            public class HttpResponseMessage {}
            public class HttpResponseMessageIntegration<TBody> : HttpResponseMessageIntegration
            {
                public HttpResponseMessageIntegration()
                {
                }

                public HttpResponseMessageIntegration(TBody data)
                {
                    Object = data;
                }

                public TBody Object { get; set; }
            }

            public class HttpResponseMessageIntegration : HttpResponseMessage
            {
                public string {|JJ1006:Method|} { get; set; }

                public string {|JJ1006:ContentType|} { get; set; }

                public string {|JJ1006:RequestUri|} { get; set; }

                public string RequestContent { get; set; }

                public string {|JJ1006:ExtraInfoJson|} { get; set; }
            }
            """,
            """
            public class HttpResponseMessage {}
            public class HttpResponseMessageIntegration<TBody> : HttpResponseMessageIntegration
            {
                public HttpResponseMessageIntegration(TBody data)
                {
                    Object = data;
                }

                public HttpResponseMessageIntegration()
                {
                }

                public TBody Object { get; set; }
            }

            public class HttpResponseMessageIntegration : HttpResponseMessage
            {
                public string ContentType { get; set; }

                public string ExtraInfoJson { get; set; }

                public string Method { get; set; }

                public string RequestContent { get; set; }

                public string RequestUri { get; set; }
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSort_Diagnostic10()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            public class Program
            {
                public string {|JJ1007:Object|} { get; set; }

                public {|JJ1005:Program|}()
                {
                }
            }
            """,
            """
            public class Program
            {
                public Program()
                {
                }

                public string Object { get; set; }
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestSort_Diagnostic11()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            public interface IProgram
            {
                public string {|JJ1006:Z|} { get; set; }

                public string {|JJ1006:A|} { get; set; }
            }
            """,
            """
            public interface IProgram
            {
                public string A { get; set; }
            
                public string Z { get; set; }
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