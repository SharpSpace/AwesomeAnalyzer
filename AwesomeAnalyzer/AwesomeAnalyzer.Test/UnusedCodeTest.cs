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
                    [|int value = 1;|]
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
                [|private void UnusedMethod()|]
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
                [|private int Number { get; set; }|]
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
                    [|void Work()|]
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
}
