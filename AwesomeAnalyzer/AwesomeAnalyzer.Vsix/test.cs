
using System;
using System.IO;
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

        public string F { get; set; }

        public bool G { get; set; }

        public static void D() { }

        void A()
        {
            using var reader = new StreamReader("");
            
        }

        //private async Task B() => await this.C();

        private async Task C(Func<Task<string>> funcAsync) => await funcAsync();

        public event OnEvent Event;
    }
}

namespace Test
{
    
    sealed class Program
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CS0029: Cannot implicitly convert type 'string' to 'int'", Justification = "Test")]
        public int Method()
        {
            //var a = "0";
            //int i = a; //int.TryParse("0", out var value) ? value : 0;

            return 0;
        }
    }

    class Program2
    {
        public int Method()
        {
            return int.Parse("1");
        }
    }
}