
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AwesomeAnalyzer.Test
{
    internal sealed class Program
    {
        public const string _e = "Const";

        private const string _d = "Const";

        public static string _g;

        public readonly string _f;

        public string _c;

        private readonly string _b;

        private string _a;

        public Program() { }

        public delegate void OnEvent(object sender, EventArgs args);

        private enum MyEnum
        {
            a, b, c
        }

        public int E { get; set; }

        public string[] F { get; set; } = Array.Empty<string>();

        public List<string> Property { get; set; }

        private IEnumerable<string> H() => 1 == 1 ? null : Enumerable.Empty<string>();

        public static void D() { }

        void A()
        {
            using var reader = new StreamReader("");
            
        }

        //private async Task B() => await this.C();

        private async Task C(Func<Task<string>> funcAsync) => await funcAsync();

        public event OnEvent Event;

        private IEnumerable<string> Method()
        {
            return new List<string>();
        }

        private IEnumerable<int> Method2()
        {
            return new int[0];
        }
    }
}

namespace Test
{
    sealed class Program
    {
        void A()
        {
            using var reader = new StreamReader("");
        }
    }
}