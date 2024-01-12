namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class PropertyDeclaration : MemberDeclaration
{
    public Expression PropertyType { get; }
    public MethodDeclaration GetMethod { get; }
    public MethodDeclaration? SetMethod { get; }
    public FieldDeclaration? BackingField { get; }
    public PropertySymbol? Symbol { get; }

    public PropertyDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression propertyType,
        FieldDeclaration? backingField,
        MethodDeclaration getMethod,
        MethodDeclaration? setMethod,
        ISourceLocation? location,
        PropertySymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
          getMethod.State | (setMethod != null ? setMethod.State : ContainsState.None),
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
}
