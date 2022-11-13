using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = AwesomeAnalyzer.Test.CSharpCodeFixVerifier<
    AwesomeAnalyzer.Analyzers.NullListAnalyzer,
    AwesomeAnalyzer.NullListCodeFixProvider>;

namespace AwesomeAnalyzer.Test
{
    [TestClass]
    public sealed class NullListTest
    {
        [TestMethod]
        public async Task Test1_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program 
    { 
        private IEnumerable<string> Method() => Enumerable.Empty<string>();
    }
}");
        }

        [TestMethod]
        public async Task Test2_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program 
    { 
        private string Method() => string.Empty;
    }
}");
        }

        [TestMethod]
        public async Task Test1_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program 
    { 
        private IEnumerable<string> Method() => [|null|];

        private IEnumerable<int> Method2() => [|null|];
    }
}", @"
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program 
    { 
        private IEnumerable<string> Method() => Enumerable.Empty<string>();

        private IEnumerable<int> Method2() => Enumerable.Empty<int>();
    }
}");
        }

        [TestMethod]
        public async Task Test2_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program 
    { 
        private IEnumerable<string> Method() => 1 == 1 ? [|null|] : Enumerable.Empty<string>();

        private IEnumerable<int> Method2() => 1 == 1 ? [|null|] : Enumerable.Empty<int>();
    }
}", @"
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program 
    { 
        private IEnumerable<string> Method() => 1 == 1 ? Enumerable.Empty<string>() : Enumerable.Empty<string>();

        private IEnumerable<int> Method2() => 1 == 1 ? Enumerable.Empty<int>() : Enumerable.Empty<int>();
    }
}");
        }

        [TestMethod]
        public async Task Test3_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program 
    { 
        private List<string> Method() => [|null|];

        private List<int> Method2() => [|null|];
    }
}", @"
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program 
    { 
        private List<string> Method() => new List<string>();

        private List<int> Method2() => new List<int>();
    }
}");
        }

        [TestMethod]
        public async Task Test5_Diagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

namespace Sample
{
    class Program 
    { 
        private string[] Method() => [|null|];

        private int[] Method2() => [|null|];
    }
}", @"
using System;

namespace Sample
{
    class Program 
    { 
        private string[] Method() => Array.Empty<string>();

        private int[] Method2() => Array.Empty<int>();
    }
}");
        }
    }
}