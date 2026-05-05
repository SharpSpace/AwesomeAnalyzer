using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.EnumerateQueryableMultipleTimesAnalyzer,
    AwesomeAnalyzer.EnumerateQueryableMultipleTimesCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

public sealed class EnumerateQueryableMultipleTimesTest
{
    [Fact]
    public async Task Test_Diagnostic_TwoForeachLoops()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IQueryable<string> GetQuery() => null;

                private void Method()
                {
                    IQueryable<string> [|query = GetQuery()|];
                    foreach (var item in query) { }
                    foreach (var item in query) { }
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IQueryable<string> GetQuery() => null;

                private void Method()
                {
                    var query = GetQuery().ToList();
                    foreach (var item in query) { }
                    foreach (var item in query) { }
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
                private static IQueryable<int> GetQuery() => null;

                private void Method()
                {
                    IQueryable<int> [|query = GetQuery()|];
                    var any = query.Any();
                    var first = query.First();
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IQueryable<int> GetQuery() => null;

                private void Method()
                {
                    var query = GetQuery().ToList();
                    var any = query.Any();
                    var first = query.First();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_Diagnostic_LinqMethodAndForeach()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IQueryable<int> GetQuery() => null;

                private void Method()
                {
                    IQueryable<int> [|query = GetQuery()|];
                    var count = query.Count();
                    foreach (var item in query) { }
                }
            }
            """,
            fixedSource:
            """
            using System.Collections.Generic;
            using System.Linq;

            sealed class Program
            {
                private static IQueryable<int> GetQuery() => null;

                private void Method()
                {
                    var query = GetQuery().ToList();
                    var count = query.Count();
                    foreach (var item in query) { }
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
            using System.Linq;

            sealed class Program
            {
                private static IQueryable<int> GetQuery() => null;

                private void Method()
                {
                    IQueryable<int> query = GetQuery();
                    foreach (var item in query) { }
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_SingleMethodCall()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System.Linq;

            sealed class Program
            {
                private static IQueryable<int> GetQuery() => null;

                private void Method()
                {
                    IQueryable<int> query = GetQuery();
                    var count = query.Count();
                }
            }
            """
        )
;
    }

    [Fact]
    public async Task Test_NoDiagnostic_IEnumerableNotAffected()
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
                    var count = items.Count();
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
            using System.Linq;

            sealed class Program
            {
                private static IQueryable<int> GetQuery() => null;

                private void Method()
                {
                    IQueryable<int> query = GetQuery();
                }
            }
            """
        )
;
    }
}
