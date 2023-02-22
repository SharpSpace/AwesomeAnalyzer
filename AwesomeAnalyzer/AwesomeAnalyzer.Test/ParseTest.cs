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
    public async Task Test1_NoDiagnosticAsync()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
                class Program 
                { 
                    public int Method()
                    {
                        return int.Parse("1");
                    }
                }
                """);
    }

    [TestMethod]
    public async Task Test2_NoDiagnosticAsync()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
                class Program 
                { 
                    public int Method()
                    {
                        return 0;
                    }
                }
                """);
    }

    [TestMethod]
    public async Task Test3_NoDiagnosticAsync()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
                class Program 
                { 
                    public int Method()
                    {
                        return int.TryParse("1", out var value) ? value : 0;
                    }
                }
                """);
    }

    [TestMethod]
    public async Task Test4_NoDiagnosticAsync()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
                class Program 
                { 
                    public int Method(string s = "")
                    {
                        return 0;
                    }
                }
                """);
    }

    [TestMethod]
    public async Task Test5_NoDiagnosticAsync()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
                class Program 
                { 
                    public int Method(bool b = false)
                    {
                        return 0;
                    }
                }
                """);
    }

    [TestMethod]
    public async Task TestInt1_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
                """);
    }

    [TestMethod]
    public async Task TestInt2_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
                """);
    }

    [TestMethod]
    public async Task TestInt10_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
                """);
    }

    [TestMethod]
    public async Task TestInt11_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
                """);
    }

    [TestMethod]
    public async Task TestInt20_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
                """);
    }

    [TestMethod]
    public async Task TestInt21_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
                """);
    }

    [TestMethod]
    public async Task TestDecimal1_Diagnostic()
    {
        var item = ParseAnalyzer.Types.Single(x => x.TypeName == "decimal");
        await VerifyCS.VerifyCodeFixAsync($$"""
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
                """);
    }

    [TestMethod]
    public async Task TestDecimal20_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
                """);
    }

    [TestMethod]
    public async Task TestByte1_Diagnostic()
    {
        var item = ParseAnalyzer.Types.Single(x => x.TypeName == "byte");
        await VerifyCS.VerifyCodeFixAsync($$"""
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
                """);
    }

    //[TestMethod]
    //public async Task TestChar1_Diagnostic()
    //{
    //    var item = ParseAnalyzer.Types.Single(x => x.TypeName == "char");
    //    await VerifyCS.VerifyCodeFixAsync($$"""
    //            class Program 
    //            { 
    //                public void Method()
    //                {
    //                    {{item.TypeName}} i = {|CS0029:{|JJ2001:{{item.TestValueString}}|}|};
    //                }
    //            }
    //            """,
    //        fixedSource: $$"""
    //            class Program 
    //            { 
    //                public void Method()
    //                {
    //                    {{item.TypeName}} i = {{item.TypeName}}.TryParse({{item.TestValueString}}, out var value) ? value : {{item.Cast}}{{item.DefaultValueString}};
    //                }
    //            }
    //            """);
    //}

    [TestMethod]
    public async Task Test1_Diagnostic()
    {
        foreach (var item in ParseAnalyzer.Types)
        {
            await VerifyCS.VerifyCodeFixAsync($$"""
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
                """);
        }
    }

    [TestMethod]
    public async Task Test2_Diagnostic()
    {
        foreach (var item in ParseAnalyzer.Types)
        {
            await VerifyCS.VerifyCodeFixAsync($$"""
                class Program 
                { 
                    public void Method()
                    {
                        {{item.TypeName}}? i = {|CS0029:{|JJ2001:{{item.TestValueString}}|}|};
                    }
                }
                """,
                fixedSource: $$"""
                class Program 
                { 
                    public void Method()
                    {
                        {{item.TypeName}}? i = {{item.TypeName}}.TryParse({{item.TestValueString}}, out var value) ? value : null;
                    }
                }
                """);
        }
    }
}