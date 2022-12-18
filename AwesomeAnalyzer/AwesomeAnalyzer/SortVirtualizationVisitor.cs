namespace AwesomeAnalyzer;

public sealed class SortVirtualizationVisitor : CSharpSyntaxRewriter
{
    public enum Types
    {
        Field = 1,
        Constructor = 2,
        Delegate = 3,
        EventField = 4,
        Event = 5,
        Enum = 6,
        Property = 7,
        Methods = 8,
    }

    public enum ModifiersSort
    {
        PublicConst = 1,
        PrivateConst = 2,
        PublicStatic = 3,
        PublicReadOnly = 4,
        Public = 5,
        PrivateStatic = 6,
        PrivateReadOnly = 7,
        Private = 8
    }

    public Dictionary<Types, List<TypesInformation>> Members { get; }

    public List<ClassInformation> Classes { get; }

    public SortVirtualizationVisitor()
    {
        Members = new Dictionary<Types, List<TypesInformation>>();
        Classes = new List<ClassInformation>();
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var item = new ClassInformation
        {
            ClassName = node.Identifier.ValueText,
            FullSpan = node.FullSpan,
        };

        Classes.Add(item);

        return base.VisitClassDeclaration(node);
    }

    public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        var item = new TypesInformation(
            Types.Enum,
            node.Identifier.ValueText,
            node.FullSpan,
            default,
            default,
            node.HasParent<ClassDeclarationSyntax>().Identifier.ValueText
        );

        SetModifiers(node.Modifiers, item);

        AddToList(item, Types.Enum);

        return base.VisitEnumDeclaration(node);
    }

    public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        var item = new TypesInformation(
            Types.Field,
            node.Declaration.Variables[0].Identifier.ValueText,
            node.FullSpan,
            default,
            default,
            node.HasParent<ClassDeclarationSyntax>().Identifier.ValueText
        );

        SetModifiers(node.Modifiers, item);

        AddToList(item, Types.Field);

        return base.VisitFieldDeclaration(node);
    }

    public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var item = new TypesInformation(
            Types.Constructor,
            node.Identifier.ValueText,
            node.FullSpan,
            default,
            default,
            node.HasParent<ClassDeclarationSyntax>().Identifier.ValueText
        );

        AddToList(item, Types.Constructor);

        return base.VisitConstructorDeclaration(node);
    }

    public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
    {
        var item = new TypesInformation(
            Types.Delegate,
            node.Identifier.ValueText,
            node.FullSpan,
            default,
            default,
            node.HasParent<ClassDeclarationSyntax>().Identifier.ValueText
        );

        SetModifiers(node.Modifiers, item);

        AddToList(item, Types.Delegate);

        return base.VisitDelegateDeclaration(node);
    }

    public override SyntaxNode VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
    {
        var item = new TypesInformation(
            Types.EventField,
            node.Declaration.Variables[0].Identifier.ValueText,
            node.FullSpan,
            default,
            default,
            node.HasParent<ClassDeclarationSyntax>().Identifier.ValueText
        );

        SetModifiers(node.Modifiers, item);

        AddToList(item, Types.EventField);

        return base.VisitEventFieldDeclaration(node);
    }

    public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
    {
        var item = new TypesInformation(
            Types.Event,
            node.Identifier.ValueText,
            node.FullSpan,
            default,
            default,
            node.HasParent<ClassDeclarationSyntax>().Identifier.ValueText
        );

        SetModifiers(node.Modifiers, item);

        AddToList(item, Types.Event);

        return base.VisitEventDeclaration(node);
    }

    public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var modifiers = node.Modifiers.Select(x => x.ValueText).ToList();
        var modifiersString = string.Join(string.Empty, modifiers);
        var item = new TypesInformation(
            Types.Property,
            node.Identifier.ValueText,
            node.FullSpan,
            string.Join(",", modifiers),
            string.IsNullOrEmpty(modifiersString)
                ? (int)ModifiersSort.Private
                : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true),
            node.HasParent<ClassDeclarationSyntax>().Identifier.ValueText
        );

        AddToList(item, Types.Property);

        return base.VisitPropertyDeclaration(node);
    }

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var modifiers = node.Modifiers.Select(x => x.ValueText).ToList();
        var modifiersString = string.Join(string.Empty, modifiers);
        var item = new TypesInformation(
            Types.Methods,
            node.Identifier.ValueText,
            node.FullSpan,
            string.Join(",", modifiers),
            string.IsNullOrEmpty(modifiersString)
                ? (int)ModifiersSort.Private
                : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true),
            node.HasParent<ClassDeclarationSyntax>().Identifier.ValueText
        );

        AddToList(item, Types.Methods);

        return base.VisitMethodDeclaration(node);
    }

    private static void SetModifiers(SyntaxTokenList modifiers, TypesInformation item)
    {
        var modifiersText = modifiers.Select(x => x.ValueText).ToList();
        var modifiersString = string.Join(string.Empty, modifiersText);

        item.Modifiers = string.Join(",", modifiersText);
        item.ModifiersOrder = string.IsNullOrEmpty(modifiersString) ? (int)ModifiersSort.Private : (int)Enum.Parse(typeof(ModifiersSort), modifiersString, true);
    }

    private void AddToList(TypesInformation item, Types constructor)
    {
        if (this.Members.ContainsKey(constructor))
        {
            this.Members[constructor].Add(item);
        }
        else
        {
            this.Members.Add(
                constructor,
                new List<TypesInformation> { item }
            );
        }
    }
}

public sealed record TypesInformation(
    SortVirtualizationVisitor.Types Type,
    string Name,
    TextSpan FullSpan,
    string Modifiers,
    int ModifiersOrder,
    string ClassName
)
{
    public int ModifiersOrder { get; set; } = ModifiersOrder;
        
    public string Modifiers { get; set; } = Modifiers;
}