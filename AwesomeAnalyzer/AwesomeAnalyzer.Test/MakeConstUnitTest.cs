using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.MakeConstAnalyzer,
    AwesomeAnalyzer.MakeConstCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

public sealed class MakeConstUnitTest
{
    [Fact]
    public async Task DeclarationIsInvalid_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    int x = {|CS0029:"abc"|};
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task DeclarationIsNotString_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    object s = "abc";
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task InitializerIsNotConstant_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    int i = DateTime.Now.DayOfYear;
                    Console.WriteLine(i);
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task LocalIntCouldBeConstant_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    [|int i = 0;|]
                    Console.WriteLine(i);
                }
            }
            """,
            """
            using System;
            class Program
            {
                static void Main()
                {
                    const int i = 0;
                    Console.WriteLine(i);
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task MultipleInitializers_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    int i = 0, j = DateTime.Now.DayOfYear;
                    Console.WriteLine(i);
                    Console.WriteLine(j);
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task NoInitializer_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    int i;
                    i = 0;
                    Console.WriteLine(i);
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task StringCouldBeConstant_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    [|string s = "abc";|]
                }
            }
            """,
            """
            using System;
            class Program
            {
                static void Main()
                {
                    const string s = "abc";
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task VariableIsAlreadyConst_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    const int i = 0;
                    Console.WriteLine(i);
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task VariableIsAssigned_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    int i = 0;
                    Console.WriteLine(i++);
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task VarIntDeclarationCouldBeConstant_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    [|var item = 4;|]
                }
            }
            """,
            """
            using System;
            class Program
            {
                static void Main()
                {
                    const int item = 4;
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task VarStringDeclarationCouldBeConstant_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            class Program
            {
                static void Main()
                {
                    [|var item = "abc";|]
                }
            }
            """,
            """
            using System;
            class Program
            {
                static void Main()
                {
                    const string item = "abc";
                }
            }
            """
        )
;
    }
}