namespace AwesomeAnalyzer;

public static class DiagnosticDescriptors
{
    private const string Category = "Order";

    public static readonly DiagnosticDescriptor MakeSealedRule0001 = new DiagnosticDescriptor(
        "JJ0001",
        "Class should have modifier sealed",
        "Class should contain modifier sealed",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Class should have modifier sealed."
    );

    public static readonly DiagnosticDescriptor MakeConstRule0003 = new DiagnosticDescriptor(
        "JJ0003",
        "Variable can be a const",
        "Variable '{0}' can be a const",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Variable can be a const."
    );

    public static readonly DiagnosticDescriptor DisposedRule0004 = new DiagnosticDescriptor(
        "JJ0004",
        "Statement is missing using because type implements IDisposable",
        "Add using to statement {0}",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Statement is missing using because type implements IDisposable."
    );

    public static readonly DiagnosticDescriptor RenameAsyncRule0100 = new DiagnosticDescriptor(
        "JJ0100",
        "Method contains Async prefix",
        "This method contains Async prefix and its not async",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Removes Async prefix from method name."
    );

    public static readonly DiagnosticDescriptor AddAwaitRule0101 = new DiagnosticDescriptor(
        "JJ0101",
        "Method call is missing Await",
        "Method call {0} is missing Await",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Method call is missing Await."
    );

    public static readonly DiagnosticDescriptor AddAsyncRule0102 = new DiagnosticDescriptor(
        "JJ0102",
        "Method name is missing Async prefix",
        "Method name {0} is missing Async prefix",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Method name is missing Async prefix."
    );

    public static readonly DiagnosticDescriptor EnumSortRule1008 = new DiagnosticDescriptor(
        "JJ1008",
        "Enum needs to be sorted alphabetically",
        "Sort enum {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sorts enums alphabetically."
    );

    public static readonly DiagnosticDescriptor EnumOrderRule1009 = new DiagnosticDescriptor(
        "JJ1009",
        "Enum needs to be in correct order",
        "Order enum {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Order enums in correct order."
    );

    public static readonly DiagnosticDescriptor FieldSortRule1001 = new DiagnosticDescriptor(
        "JJ1001",
        "Field needs to be sorted alphabetically",
        "Sort field {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sorts fields alphabetically."
    );

    public static readonly DiagnosticDescriptor FieldOrderRule1002 = new DiagnosticDescriptor(
        "JJ1002",
        "Field needs to be in correct order",
        "Order field {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Order fields in correct order."
    );

    public static readonly DiagnosticDescriptor ConstructorOrderRule1005 = new DiagnosticDescriptor(
        "JJ1005",
        "Constructor needs to be in correct order",
        "Order constructor {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Order constructor in correct order."
    );

    public static readonly DiagnosticDescriptor DelegateSortRule1010 = new DiagnosticDescriptor(
        "JJ1010",
        "Delegate needs to be sorted alphabetically",
        "Sort delegate {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sorts delegates alphabetically."
    );

    public static readonly DiagnosticDescriptor DelegateOrderRule1011 = new DiagnosticDescriptor(
        "JJ1011",
        "Delegate needs to be in correct order",
        "Order delegate {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Order delegates in correct order."
    );

    public static readonly DiagnosticDescriptor EventSortRule1012 = new DiagnosticDescriptor(
        "JJ1012",
        "Event needs to be sorted alphabetically",
        "Sort event {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sorts events alphabetically."
    );

    public static readonly DiagnosticDescriptor EventOrderRule1013 = new DiagnosticDescriptor(
        "JJ1013",
        "Event needs to be in correct order",
        "Order event {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Order events in correct order."
    );

    public static readonly DiagnosticDescriptor PropertySortRule1006 = new DiagnosticDescriptor(
        "JJ1006",
        "Property needs to be sorted alphabetically",
        "Sort property {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sorts properties alphabetically."
    );

    public static readonly DiagnosticDescriptor PropertyOrderRule1007 = new DiagnosticDescriptor(
        "JJ1007",
        "Property needs to be in correct order",
        "Order property {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Order properties in correct order."
    );

    public static readonly DiagnosticDescriptor MethodSortRule1003 = new DiagnosticDescriptor(
        "JJ1003",
        "Method needs to be sorted alphabetically",
        "Sort method {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sorts methods alphabetically."
    );

    public static readonly DiagnosticDescriptor MethodOrderRule1004 = new DiagnosticDescriptor(
        "JJ1004",
        "Method needs to be in correct order",
        "Order method {0}", Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Order methods in correct order."
    );

    public static readonly DiagnosticDescriptor ParseIntRule2001 = new DiagnosticDescriptor(
        "JJ2001",
        "Add Parse",
        "Add Parse", 
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Add Parse."
    );
}