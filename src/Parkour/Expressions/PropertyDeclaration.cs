namespace Parkour.Expressions;
using Symbols;
using Syntax;

public sealed class PropertyDeclaration : Declaration
{
    public MethodDeclaration GetMethod { get; }
    public MethodDeclaration? SetMethod { get; }
    public FieldDeclaration? UnderlyingField { get; }
    public Expression PropertyType { get; }

    public PropertyDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        MethodDeclaration getMethod,
        MethodDeclaration? setMethod,
        FieldDeclaration? underlyingField,
        Expression propertyType,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
    : base(
          getMethod.State | (setMethod != null ? setMethod.State : ContainsState.None),
          name,
          access,
          modifiers,
          diagnostics,
          syntax)
    {
        this.GetMethod = getMethod;
        this.SetMethod = setMethod;
        this.UnderlyingField = underlyingField;
        this.PropertyType = propertyType ?? getMethod.ReturnType;
    }
}
