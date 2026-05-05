using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.EnumerateMultipleTimesAnalyzer,
    AwesomeAnalyzer.EnumerateMultipleTimesCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

public sealed class EnumerateMultipleTimesTest
{
    [Fact]
    public async Task Test_Diagnostic_MethodCallAndForeach()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> [|items = GetItems()|];
                    var count = items.Count();
                    foreach (var item in items) { }
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> items = GetItems().ToList();
                    var count = items.Count();
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic_TwoForeachLoops()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<string> GetItems() => new[] { "a", "b" };

                private void Method()
                {
                    IEnumerable<string> [|items = GetItems()|];
                    foreach (var item in items) { }
                    foreach (var item in items) { }
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<string> GetItems() => new[] { "a", "b" };

                private void Method()
                {
                    IEnumerable<string> items = GetItems().ToList();
                    foreach (var item in items) { }
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic_TwoLinqMethods()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> [|items = GetItems()|];
                    var any = items.Any();
                    var first = items.First();
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> items = GetItems().ToList();
                    var any = items.Any();
                    var first = items.First();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic_VarInferredAsIEnumerable()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    var [|items = GetItems()|];
                    var count = items.Count();
                    foreach (var item in items) { }
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    var items = GetItems().ToList();
                    var count = items.Count();
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic_LinqChainAndForeach()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> [|items = GetItems().Where(x => x > 1)|];
                    var count = items.Count();
                    foreach (var item in items) { }
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> items = GetItems().Where(x => x > 1).ToList();
                    var count = items.Count();
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic_AnyAndForeach()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> [|items = GetItems()|];
                    if (items.Any())
                    {
                        foreach (var item in items) { }
                    }
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> items = GetItems().ToList();
                    if (items.Any())
                    {
                        foreach (var item in items) { }
                    }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_SingleForeach()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> items = GetItems();
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_SingleMaterializationCall()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> items = GetItems();
                    var list = items.ToList();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_ToListTwiceOnAlreadyMaterialized()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    var items = GetItems().ToList();
                    var x = items.ToList();
                    var y = items.ToList();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_AlreadyList()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private void Method()
                {
                    List<int> items = new List<int> { 1, 2, 3 };
                    var count = items.Count;
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_VarAlreadyList()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    var items = GetItems().ToList();
                    var count = items.Count;
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_InitializerIsNewList()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private void Method()
                {
                    IEnumerable<int> items = new List<int> { 1, 2, 3 };
                    var count = items.Count();
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_InitializerIsArray()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private void Method()
                {
                    IEnumerable<int> items = new[] { 1, 2, 3 };
                    var count = items.Count();
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_DeclaredAsIList()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private void Method()
                {
                    IList<int> items = new List<int> { 1, 2, 3 };
                    var count = items.Count;
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_NoUsage()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;

            sealed class Program
            {
                private static IEnumerable<int> GetItems() => new[] { 1, 2, 3 };

                private void Method()
                {
                    IEnumerable<int> items = GetItems();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_VarInferredAsList()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private void Method()
                {
                    var items = new List<int> { 1, 2, 3 };
                    var count = items.Count;
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_InitializerIsDictionary()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private void Method()
                {
                    IEnumerable<KeyValuePair<int, string>> items = new Dictionary<int, string> { { 1, "a" } };
                    var count = items.Count();
                    foreach (var item in items) { }
                }
            }
            """
        )
;
    }
}
