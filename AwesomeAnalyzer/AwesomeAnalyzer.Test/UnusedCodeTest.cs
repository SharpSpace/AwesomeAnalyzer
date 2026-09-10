using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.UnusedCodeAnalyzer,
    AwesomeAnalyzer.UnusedCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

public sealed class UnusedCodeTest
{
    [Fact]
    public async Task UnusedLocalVariable_DiagnosticAndFix()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public void Run()
                {
                    int {|JJ0012:value|} = 1;
                }
            }
            """,
            """
            class Program
            {
                public void Run()
                {

                }
            }
            """
        )
;
    }

    [Fact]
    public async Task UsedLocalVariable_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;

            class Program
            {
                public void Run()
                {
                    int value = 1;
                    Console.WriteLine(value);
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task UnusedPrivateMethod_DiagnosticAndFix()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                private void {|JJ0012:UnusedMethod|}()
                {
                }
            }
            """,
            """
            class Program
            {

            }
            """
        )
;
    }

    [Fact]
    public async Task UsedPrivateMethod_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            class Program
            {
                public void Run()
                {
                    UsedMethod();
                }

                private void UsedMethod()
                {
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task UnusedPrivateProperty_DiagnosticAndFix()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                private int {|JJ0012:Number|} { get; set; }
            }
            """,
            """
            class Program
            {

            }
            """
        )
;
    }

    [Fact]
    public async Task UnusedLocalFunction_DiagnosticAndFix()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public void Run()
                {
                    void {|JJ0012:Work|}()
                    {
                    }
                }
            }
            """,
            """
            class Program
            {
                public void Run()
                {

                }
            }
            """
        )
;
    }

    [Fact]
    public async Task UnusedVariableInMultiDeclaration_DiagnosticAndFix()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                public void Run()
                {
                    int used = 1, [|unused|] = 2;
                    Console.WriteLine(used);
                }
            }
            """,
            """
            using System;

            class Program
            {
                public void Run()
                {
                    int used = 1;
                    Console.WriteLine(used);
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task UnusedPrivateEvent_DiagnosticAndFix()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                private event EventHandler {|JJ0012:Changed|};
            }
            """,
            """
            using System;

            class Program
            {

            }
            """
        )
;
    }

    [Fact]
    public async Task UnusedNestedPrivateClass_DiagnosticAndFix()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                private class {|JJ0012:Inner|} { }
            }
            """,
            """
            class Program
            {

            }
            """
        )
;
    }

    [Fact]
    public async Task PrivateConstructor_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            class Program
            {
                private Program()
                {
                }

                public static Program Create()
                {
                    return new Program();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task PrivateEventWithSubscription_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;

            class Program
            {
                private event EventHandler Changed;

                public void Run()
                {
                    Changed += OnChanged;
                    Changed -= OnChanged;
                }

                private void OnChanged(object sender, EventArgs e)
                {
                }
            }
            """
        )
;
    }
}
