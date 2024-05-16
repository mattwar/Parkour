namespace Parkour.Semantics;

using Symbols;

public sealed class PropertyDeclaration : MemberDeclaration
{
    public override PropertySymbol? Symbol { get; }

    public Expression PropertyType { get; }
    public MethodDeclaration GetMethod { get; }
    public MethodDeclaration? SetMethod { get; }
    public FieldDeclaration? BackingField { get; }

    public PropertyDeclaration(
        string name,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Expression propertyType,
        FieldDeclaration? backingField,
        MethodDeclaration getMethod,
        MethodDeclaration? setMethod,
        ISourceLocation? location,
        PropertySymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
        State(propertyType)
        | State(backingField)
        | State(getMethod)
        | State(setMethod)
        | NotNullState(symbol),
        name,
        access,
        modifiers,
        location,
        diagnostics)
    {
        this.PropertyType = propertyType ?? getMethod.ReturnType;
        this.BackingField = backingField;
        this.GetMethod = getMethod;
        this.SetMethod = setMethod;
        this.Symbol = symbol;
    }

    public override PropertyDeclaration WithName(string name) =>
        new PropertyDeclaration(
            name,
            this.Access,
            this.Modifiers,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override PropertyDeclaration WithLocation(ISourceLocation? location) =>
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public PropertyDeclaration WithSymbol(PropertySymbol? symbol) =>
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override PropertyDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override PropertyDeclaration WithAccess(SymbolAccess access) =>
        new PropertyDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override PropertyDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        new PropertyDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public PropertyDeclaration WithBackingField(FieldDeclaration? backingField) =>
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.PropertyType,
            backingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public PropertyDeclaration WithGetMethod(MethodDeclaration getMethod) =>
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.PropertyType,
            this.BackingField,
            getMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public PropertyDeclaration WithSetMethod(MethodDeclaration? setMethod) =>
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            setMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override int ChildCount => 4;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.PropertyType,
            1 => this.GetMethod,
            2 => this.SetMethod,
            3 => this.BackingField,
            _ => null
        };
}
