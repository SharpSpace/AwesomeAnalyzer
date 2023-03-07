using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.SimilarAnalyzer,
    AwesomeAnalyzer.SimilarCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class SimilarTest
{
    [TestMethod]
    public async Task Test_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            public class Program
            {
                private static void Main(string[] args)
                {
                    var enumerable = Enumerable.Range(0, 10).ToList();
                    {|JJ0008:foreach (var i in enumerable)
                    {
                        Console.WriteLine(i);
                    }|}

                    Console.WriteLine(10);

                    {|JJ0008:foreach (var i in enumerable)
                    {
                        Console.WriteLine(i);
                    }|}
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            public class Program
            {
                private static void Main(string[] args)
                {
                    var enumerable = Enumerable.Range(0, 10).ToList();
                    NewMethod(enumerable);
                    Console.WriteLine(10);

                    NewMethod(enumerable);
                }

                private static void NewMethod(List<int> enumerable)
                {
                    foreach (var i in enumerable)
                    {
                        Console.WriteLine(i);
                    }
                }
            }
            """
        )
        .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Diagnostic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            public class Program
            {
                private static void Main(string[] args)
                {
                    switch (true)
                    {
                        case true:
                        {
                            var enumerable = Enumerable.Range(0, 10).ToList();
                            {|JJ0008:foreach (var i in enumerable)
                            {
                                Console.WriteLine(i);
                            }|}

                            Console.WriteLine(10);

                            {|JJ0008:foreach (var i in enumerable)
                            {
                                Console.WriteLine(i);
                            }|}
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            public class Program
            {
                private static void Main(string[] args)
                {
                    switch (true)
                    {
                        case true:
                        {
                            var enumerable = Enumerable.Range(0, 10).ToList();
                            NewMethod(enumerable);
                            Console.WriteLine(10);

                            NewMethod(enumerable);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                }

                private static void NewMethod(List<int> enumerable)
                {
                            foreach (var i in enumerable)
                            {
                                Console.WriteLine(i);
                            }
                }
            }
            """
            )
            .ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic1()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            using System;

            sealed class Program
            {
                private static void Main()
                {
                    try
                    {
                        var a = "a";
                    }
                    catch (Exception e)
                    {
                        throw;
                    }

                    try
                    {
                        var b = "a";
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }
            }
            """
        )
        .ConfigureAwait(false);
    }
}