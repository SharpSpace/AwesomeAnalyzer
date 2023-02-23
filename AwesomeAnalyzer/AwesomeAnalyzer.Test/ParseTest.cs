using System.Linq;
using AwesomeAnalyzer.Analyzers;
using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.ParseAnalyzer,
    AwesomeAnalyzer.ParseCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class ParseTest
{
    [TestMethod]
    public async Task Test_Diagnostic1()
    {
        foreach (var item in ParseAnalyzer.Types)
        {
            await VerifyCS.VerifyCodeFixAsync(
                $$"""
                class Program
                {
                    public void Method()
                    {
                        {{item.TypeName}} i = {|CS0029:{|JJ2001:{{item.TestValueString}}|}|};
                    }
                }
                """,
                fixedSource:
                $$"""
                class Program
                {
                    public void Method()
                    {
                        {{item.TypeName}} i = {{item.TypeName}}.TryParse({{item.TestValueString}}, out var value) ? value : {{item.Cast}}{{item.DefaultValueString}};
                    }
                }
                """
            ).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task Test_Diagnostic2()
    {
        foreach (var item in ParseAnalyzer.Types)
        {
            await VerifyCS.VerifyCodeFixAsync(
                $$"""
                class Program
                {
                    public void Method()
                    {
                        {{item.TypeName}}? i = {|CS0029:{|JJ2001:{{item.TestValueString}}|}|};
                    }
                }
                """,
                fixedSource:
                $$"""
                class Program
                {
                    public void Method()
                    {
                        {{item.TypeName}}? i = {{item.TypeName}}.TryParse({{item.TestValueString}}, out var value) ? value : null;
                    }
                }
                """
            ).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task Test_NoDiagnosticAsync1()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
                class Program
                {
                    public int Method()
                    {
                        return int.Parse("1");
                    }
                }
                """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnosticAsync2()
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
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnosticAsync3()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
                class Program
                {
                    public int Method()
                    {
                        return int.TryParse("1", out var value) ? value : 0;
                    }
                }
                """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnosticAsync4()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
                class Program
                {
                    public int Method(string s = "")
                    {
                        return 0;
                    }
                }
                """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnosticAsync5()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
                class Program
                {
                    public int Method(bool b = false)
                    {
                        return 0;
                    }
                }
                """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnosticAsync6() {
        await VerifyCS.VerifyAnalyzerAsync(
            """
                class Program
                {
                    public void Method()
                    {
                        var a = "s";
                        var b = a;
                    }
                }
                """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestByte_Diagnostic1()
    {
        var item = ParseAnalyzer.Types.Single(x => x.TypeName == "byte");
        await VerifyCS.VerifyCodeFixAsync(
            $$"""
            class Program
            {
                public void Method()
                {
                    {{item.TypeName}} i = {|CS0029:{|JJ2001:{{item.TestValueString}}|}|};
                }
            }
            """,
            fixedSource: $$"""
            class Program
            {
                public void Method()
                {
                    {{item.TypeName}} i = {{item.TypeName}}.TryParse({{item.TestValueString}}, out var value) ? value : {{item.Cast}}{{item.DefaultValueString}};
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestDecimal_Diagnostic1()
    {
        var item = ParseAnalyzer.Types.Single(x => x.TypeName == "decimal");
        await VerifyCS.VerifyCodeFixAsync(
            $$"""
            class Program
            {
                public void Method()
                {
                    {{item.TypeName}} i = {|CS0029:{|JJ2001:{{item.TestValueString}}|}|};
                }
            }
            """,
            fixedSource: $$"""
            class Program
            {
                public void Method()
                {
                    {{item.TypeName}} i = {{item.TypeName}}.TryParse({{item.TestValueString}}, out var value) ? value : {{item.DefaultValueString}};
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestDecimal_Diagnostic20()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public void Method()
                {
                    var s = "1";
                    decimal i = {|CS0029:{|JJ2001:s|}|};
                }
            }
            """,
            fixedSource: """
            class Program
            {
                public void Method()
                {
                    var s = "1";
                    decimal i = decimal.TryParse(s, out var value) ? value : 0;
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestInt_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public void Method()
                {
                    int i = {|CS0029:{|JJ2001:"1"|}|};
                }
            }
            """,
            fixedSource: """
            class Program
            {
                public void Method()
                {
                    int i = int.TryParse("1", out var value) ? value : 0;
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestInt_Diagnostic10()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public int Method()
                {
                    return {|CS0029:{|JJ2001:"1"|}|};
                }
            }
            """,
            fixedSource: """
            class Program
            {
                public int Method()
                {
                    return int.TryParse("1", out var value) ? value : 0;
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestInt_Diagnostic11()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public int? Method()
                {
                    return {|CS0029:{|JJ2001:"1"|}|};
                }
            }
            """,
            fixedSource: """
            class Program
            {
                public int? Method()
                {
                    return int.TryParse("1", out var value) ? value : null;
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestInt_Diagnostic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public void Method()
                {
                    int? i = {|CS0029:{|JJ2001:"1"|}|};
                }
            }
            """,
            fixedSource: """
            class Program
            {
                public void Method()
                {
                    int? i = int.TryParse("1", out var value) ? value : null;
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestInt_Diagnostic20()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public void Method()
                {
                    var s = "1";
                    int i = {|CS0029:{|JJ2001:s|}|};
                }
            }
            """,
            fixedSource: """
            class Program
            {
                public void Method()
                {
                    var s = "1";
                    int i = int.TryParse(s, out var value) ? value : 0;
                }
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TestInt_Diagnostic21()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public void Method()
                {
                    var s = "1";
                    int? i = {|CS0029:{|JJ2001:s|}|};
                }
            }
            """,
            fixedSource: """
            class Program
            {
                public void Method()
                {
                    var s = "1";
                    int? i = int.TryParse(s, out var value) ? value : null;
                }
            }
            """
        ).ConfigureAwait(false);
    }
}