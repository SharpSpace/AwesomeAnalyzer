using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.ClassAsStructAnalyzer,
    AwesomeAnalyzer.ClassAsStructCodeFixProvider>;

namespace AwesomeAnalyzer.Test;

public sealed class ClassAsStructTest
{
    [Fact]
    public async Task AbstractClass_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public abstract class Point
            {
                public readonly int X;
                public readonly int Y;
            }
            """
        )
;
    }

    [Fact]
    public async Task ClassWithBaseClass_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public class Base { }
            public class Point : Base
            {
                public readonly int X;
                public readonly int Y;
            }
            """
        )
;
    }

    [Fact]
    public async Task ClassWithConstFields_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
                public class {|JJ0200:Constants|}
                {
                    public const int MaxValue = 100;
                }
                """,
            fixedSource: """
                public struct Constants
                {
                    public const int MaxValue = 100;
                }
                """
        )
;
    }

    [Fact]
    public async Task ClassWithInterface_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public interface IPoint { }
            public class Point : IPoint
            {
                public readonly int X;
                public readonly int Y;
            }
            """
        )
;
    }

    [Fact]
    public async Task ClassWithMutableField_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public class Point
            {
                public int X;
                public readonly int Y;
            }
            """
        )
;
    }

    [Fact]
    public async Task ClassWithMutableProperty_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public class Point
            {
                public int X { get; set; }
                public int Y { get; }
            }
            """
        )
;
    }

    [Fact]
    public async Task ClassWithReadOnlyProperties_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
                public class {|JJ0200:Point|}
                {
                    public int X { get; }
                    public int Y { get; }
                }
                """,
            fixedSource: """
                public struct Point
                {
                    public int X { get; }
                    public int Y { get; }
                }
                """
        )
;
    }

    [Fact]
    public async Task ClassWithVirtualMember_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public class Point
            {
                public readonly int X;
                public readonly int Y;
                
                public virtual int GetDistance() => 0;
            }
            """
        )
;
    }

    [Fact]
    public async Task EmptyClass_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public class Point
            {
            }
            """
        )
;
    }

    [Fact]
    public async Task InternalClassWithReadOnlyFields_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
                internal class {|JJ0200:Point|}
                {
                    public readonly int X;
                    public readonly int Y;
                }
                """,
            fixedSource: """
                internal struct Point
                {
                    public readonly int X;
                    public readonly int Y;
                }
                """
        )
;
    }

    [Fact]
    public async Task LargeClass_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public class Point
            {
                public readonly int X;
                public readonly int Y;
                public readonly int Z;
                public readonly int W;
            }
            """
        )
;
    }

    [Fact]
    public async Task PartialClass_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
                public partial class {|JJ0200:Point|}
                {
                    public readonly int X;
                    public readonly int Y;
                }
                """,
            fixedSource: """
                public partial struct Point
                {
                    public readonly int X;
                    public readonly int Y;
                }
                """
        )
;
    }

    [Fact]
    public async Task SmallImmutableClass_Diagnostic()
    {
        await VerifyCS.VerifyCodeFixAsync(
            source: """
                public class {|JJ0200:Point|}
                {
                    public readonly int X;
                    public readonly int Y;
                }
                """,
            fixedSource: """
                public struct Point
                {
                    public readonly int X;
                    public readonly int Y;
                }
                """
        )
;
    }

    [Fact]
    public async Task StaticClass_NoDiagnostic()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            public static class Point
            {
                public static readonly int X = 0;
                public static readonly int Y = 0;
            }
            """
        )
;
    }
}
