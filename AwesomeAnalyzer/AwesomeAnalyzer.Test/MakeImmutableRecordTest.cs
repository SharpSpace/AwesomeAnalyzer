using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.MakeImmutableRecordAnalyzer,
    AwesomeAnalyzer.MakeImmutableRecordCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

[TestClass]
public sealed class MakeImmutableRecordTest
{
    [TestMethod]
    public async Task Test_Diagnostic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            public record Test
            {
                {|JJ0009:public string Name { get; set; }|}
            }

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """,
            fixedSource: """
            public record Test(string Name);

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Diagnostic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            public record Test
            {
                {|JJ0009:public string Name { get; set; }|}
                {|JJ0009:public string Lastname { get; set; }|}
            }

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """,
            fixedSource: """
            public record Test(string Name, string Lastname);

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Diagnostic3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            public record Test
            {
                public string Name { get; set; }
                {|JJ0009:public string Lastname { get; set; }|}
            }

            public class Test2
            {
                public Test2()
                {
                    var test = new Test
                    {
                        Name = "test",
                        Lastname = "testsson"
                    };

                    test.Name = "test2";
                }
            }

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """,
            fixedSource: """
            public record Test(string Lastname)
            {
                public string Name { get; set; }
            }

            public class Test2
            {
                public Test2()
                {
                    var test = new Test("testsson")
                    {
                        Name = "test"
                    };

                    test.Name = "test2";
                }
            }
            
            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Diagnostic4()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            public record Test
            {
                {|JJ0009:public string Name { get; set; }|}
                public string Lastname { get; set; }
                {|JJ0009:public string Email { get; set; }|}
            }

            public class Test2
            {
                public Test2()
                {
                    var test = new Test
                    {
                        Name = "test",
                        Lastname = "testsson",
                        Email = "test@testsson@test.com"
                    };

                    test.Lastname = "testsson2";
                }
            }

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """,
            fixedSource: """
            public record Test(string Name, string Email)
            {
                public string Lastname { get; set; }
            }

            public class Test2
            {
                public Test2()
                {
                    var test = new Test("test","test@testsson@test.com")
                    {
                        Lastname = "testsson"
                    };

                    test.Lastname = "testsson2";
                }
            }
            
            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """
        ).ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task Test_Diagnostic5()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
            public record Test
            {
                {|JJ0009:public string Name { get; init; }|}
            }

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """,
            fixedSource: """
            public record Test(string Name);

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic1()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public record Test
            {
                public string Name { get; }
            }

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """
        ).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_NoDiagnostic2()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public record Test
            {
                public string Name { get; private set; }
            }

            namespace System.Runtime.CompilerServices
            {
                internal static class IsExternalInit {}
            }
            """
        ).ConfigureAwait(false);
    }
}