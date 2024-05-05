namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class PropertyDeclaration : MemberDeclaration
{
    public Expression PropertyType { get; }
    public MethodDeclaration GetMethod { get; }
    public MethodDeclaration? SetMethod { get; }
    public FieldDeclaration? BackingField { get; }
    public PropertySymbol? PropertySymbol { get; }

    public PropertyDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression propertyType,
        FieldDeclaration? backingField,
        MethodDeclaration getMethod,
        MethodDeclaration? setMethod,
        ISourceLocation? location,
        PropertySymbol? propertySymbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
        State(propertyType)
        | State(backingField)
        | State(getMethod)
        | State(setMethod)
        | NotNullState(propertySymbol),
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
        this.PropertySymbol = propertySymbol;
    }

    public override Symbol? DeclaredSymbol => this.PropertySymbol;

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
