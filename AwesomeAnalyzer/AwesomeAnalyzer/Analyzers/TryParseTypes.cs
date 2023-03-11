namespace AwesomeAnalyzer.Analyzers
{
    public readonly struct TryParseTypes<T> // : ITryParseTypes
    {
        public TryParseTypes(string typeName, T testValue, T defaultValue)
        {
            TypeName = typeName;
            TestValue = testValue;
            DefaultValue = defaultValue;
        }

        public string Cast
        {
            get
            {
                switch (TypeName)
                {
                    case "byte":
                        return "(byte)";

                    case "sbyte":
                        return "(sbyte)";

                    case "short":
                        return "(short)";

                    case "ushort":
                        return "(ushort)";

                    default:
                        return string.Empty;
                }
            }
        }

        public T DefaultValue { get; }

        public string DefaultValueString => DefaultValue.ToString().ToLower();

        public T TestValue { get; }

        public string TestValueString => $"\"{TestValue.ToString().ToLower()}\"";

        public string TypeName { get; }
    }
}