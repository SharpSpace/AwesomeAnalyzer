using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.DontReturnNullAnalyzer,
    AwesomeAnalyzer.DontReturnNullCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

public sealed class DontReturnNullTests
{
    [Fact]
    public async Task Test_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;

            sealed class Program
            {
                public List<string> Method()
                {
                    {|JJ0007:return null;|}
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;

            sealed class Program
            {
                public List<string> Method()
                {
                    return new List<string>();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;

            sealed class Program
            {
                public IList<string> Method()
                {
                    {|JJ0007:return null;|}
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;

            sealed class Program
            {
                public IList<string> Method()
                {
                    return new List<string>();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                public IEnumerable<string> Method()
                {
                    {|JJ0007:return null;|}
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                public IEnumerable<string> Method()
                {
                    return Enumerable.Empty<string>();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic4()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            sealed class Program
            {
                public string[] Method()
                {
                    {|JJ0007:return null;|}
                }
            }
            """,
            fixedSource:
            """
            using System;

            sealed class Program
            {
                public string[] Method()
                {
                    return Array.Empty<string>();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic5()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections;

            sealed class Program
            {
                public ArrayList Method()
                {
                    {|JJ0007:return null;|}
                }
            }
            """,
            fixedSource:
            """
            using System.Collections;

            sealed class Program
            {
                public ArrayList Method()
                {
                    return new ArrayList();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic1()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;

            sealed class Program
            {
                public List<string> Method()
                {
                    return new List<string>();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic2()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;

            sealed class Program
            {
                public int? Method()
                {
                    return null;
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic3()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;

            sealed class Program
            {
                public string Method()
                {
                    return null;
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic4()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;

            sealed class Program
            {
                public Item Method()
                {
                    return null;
                }
            }

            sealed class Item { }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic5()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            sealed class Program
            {
                public async Task<Item> Method()
                {
                    await Task.CompletedTask;
                    return null;
                }
            }

            sealed class Item { }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic6()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            sealed class Program
            {
                public async ValueTask<Item> Method()
                {
                    await Task.CompletedTask;
                    return null;
                }
            }

            sealed class Item { }
            """
        )
;
    }
}