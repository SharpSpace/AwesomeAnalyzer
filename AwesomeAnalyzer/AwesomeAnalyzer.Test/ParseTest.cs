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
}