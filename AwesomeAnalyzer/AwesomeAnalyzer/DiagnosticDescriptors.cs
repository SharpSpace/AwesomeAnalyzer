using Microsoft.CodeAnalysis;

namespace AwesomeAnalyzer
{
    public readonly struct DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor Rule0001MakeSealed = new DiagnosticDescriptor(
            "JJ0001",
            "Class should have modifier sealed",
            "Class '{0}' should contain modifier sealed",
            TextPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Class should have modifier sealed."
        );

        public static readonly DiagnosticDescriptor Rule0003MakeConst = new DiagnosticDescriptor(
            "JJ0003",
            "Variable can be a const",
            "Variable '{0}' can be a const",
            TextPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Variable can be a const."
        );

        public static readonly DiagnosticDescriptor Rule0004Disposed = new DiagnosticDescriptor(
            "JJ0004",
            "Statement is missing using because type implements IDisposable",
            "Add using to statement '{0}'",
            TextPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Statement is missing using because type implements IDisposable."
        );

        public static readonly DiagnosticDescriptor Rule0005ParseString = new DiagnosticDescriptor(
            "JJ2001",
            "Add TryParse",
            "Add TryParse",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Add TryParse."
        );

        public static readonly DiagnosticDescriptor Rule0006RemoveAsyncAwait = new DiagnosticDescriptor(
            "JJ0006",
            "Remove async and await",
            "Remove async and await in method '{0}'",
            TextPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Remove async and await."
        );

        public static readonly DiagnosticDescriptor Rule0007DontReturnNull = new DiagnosticDescriptor(
            "JJ0007",
            "Don't return null",
            "Don't return null in method '{0}'",
            TextPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Don't return null."
        );

        public static readonly DiagnosticDescriptor Rule0008Similar = new DiagnosticDescriptor(
            "JJ0008",
            "Similar Code Detected",
            "This code appears to be similar to other code in the project",
            TextPerformance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "This code appears to be similar to other code in the project."
        );

        public static readonly DiagnosticDescriptor Rule0009MakeImmutableRecord = new DiagnosticDescriptor(
            "JJ0009",
            "Property can be made immutable in Record",
            "Make property '{0}' immutable in Record",
            TextPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Make property immutable in Record."
        );

        public static readonly DiagnosticDescriptor Rule0100RenameAsync = new DiagnosticDescriptor(
            "JJ0100",
            "Method name contains Async prefix",
            "Method name '{0}' contains Async prefix and its not async",
            TextNaming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Removes Async prefix from method name."
        );

        public static readonly DiagnosticDescriptor Rule0101AddAwait = new DiagnosticDescriptor(
            "JJ0101",
            "Method call is missing Await",
            "Method call '{0}' is missing Await",
            TextNaming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Method call is missing Await."
        );

        public static readonly DiagnosticDescriptor Rule0102AddAsync = new DiagnosticDescriptor(
            "JJ0102",
            "Method name is missing Async prefix",
            "Method name '{0}' is missing Async prefix",
            TextNaming,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Method name is missing Async prefix."
        );

        public static readonly DiagnosticDescriptor Rule1001FieldSort = new DiagnosticDescriptor(
            "JJ1001",
            "Field needs to be sorted alphabetically",
            "Sort field '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sorts fields alphabetically."
        );

        public static readonly DiagnosticDescriptor Rule1002FieldOrder = new DiagnosticDescriptor(
            "JJ1002",
            "Field needs to be in correct order",
            "Order field '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order fields in correct order."
        );

        public static readonly DiagnosticDescriptor Rule1003MethodSort = new DiagnosticDescriptor(
            "JJ1003",
            "Method needs to be sorted alphabetically",
            "Sort method '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sorts methods alphabetically."
        );

        public static readonly DiagnosticDescriptor Rule1004MethodOrder = new DiagnosticDescriptor(
            "JJ1004",
            "Method needs to be in correct order",
            "Order method '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order methods in correct order."
        );

        public static readonly DiagnosticDescriptor Rule1005ConstructorOrder = new DiagnosticDescriptor(
            "JJ1005",
            "Constructor needs to be in correct order",
            "Order constructor '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order constructor in correct order."
        );

        public static readonly DiagnosticDescriptor Rule1006PropertySort = new DiagnosticDescriptor(
            "JJ1006",
            "Property needs to be sorted alphabetically",
            "Sort property '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sorts properties alphabetically."
        );

        public static readonly DiagnosticDescriptor Rule1007PropertyOrder = new DiagnosticDescriptor(
            "JJ1007",
            "Property needs to be in correct order",
            "Order property '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order properties in correct order."
        );

        public static readonly DiagnosticDescriptor Rule1008EnumSort = new DiagnosticDescriptor(
            "JJ1008",
            "Enum needs to be sorted alphabetically",
            "Sort enum '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sorts enums alphabetically."
        );

        public static readonly DiagnosticDescriptor Rule1009EnumOrder = new DiagnosticDescriptor(
            "JJ1009",
            "Enum needs to be in correct order",
            "Order enum '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order enums in correct order."
        );

        public static readonly DiagnosticDescriptor Rule1010DelegateSort = new DiagnosticDescriptor(
            "JJ1010",
            "Delegate needs to be sorted alphabetically",
            "Sort delegate '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sorts delegates alphabetically."
        );

        public static readonly DiagnosticDescriptor Rule1011DelegateOrder = new DiagnosticDescriptor(
            "JJ1011",
            "Delegate needs to be in correct order",
            "Order delegate '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order delegates in correct order."
        );

        public static readonly DiagnosticDescriptor Rule1012EventSort = new DiagnosticDescriptor(
            "JJ1012",
            "Event needs to be sorted alphabetically",
            "Sort event '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sorts events alphabetically."
        );

        public static readonly DiagnosticDescriptor Rule1013EventOrder = new DiagnosticDescriptor(
            "JJ1013",
            "Event needs to be in correct order",
            "Order event '{0}'",
            TextOrder,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Order events in correct order."
        );

        private const string TextNaming = "Naming";

        private const string TextOrder = "Order";

        private const string TextPerformance = "Performance";

        private const string TextUsage = "Usage";
    }
}