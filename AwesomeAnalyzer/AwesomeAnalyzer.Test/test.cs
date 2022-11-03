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

        private enum MyEnum
        {
            a, b, c
        }

        public void D() { }

        private void A() { }

        private void B()
        {
            this.A();
        }

        private void C() { }

        //public string F { get; set; }

        //public int B { get; set; }

        //public bool A { get; set; }
    }
}