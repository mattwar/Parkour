namespace Parkour.Semantics;

using Parkour;
using Symbols;

public sealed class PropertyDeclaration : MemberDeclaration
{
    public override PropertySymbol? Symbol { get; }

    public Expression? PropertyType { get; }
    public MethodDeclaration GetMethod { get; }
    public MethodDeclaration? SetMethod { get; }
    public FieldDeclaration? BackingField { get; }

    private PropertyDeclaration(
        string name,
        Access access,
        BitSet<Modifier> modifiers,
        ImmutableList<AttributeExpression> attributes,
        Expression? propertyType,
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
        attributes,
        location,
        diagnostics)
    {
        this.PropertyType = propertyType;
        this.BackingField = backingField;
        this.GetMethod = getMethod;
        this.SetMethod = setMethod;
        this.Symbol = symbol;
    }

    public PropertyDeclaration(
        string name,
        Expression? propertyType,
        MethodDeclaration getMethod,
        MethodDeclaration? setMethod,
        ISourceLocation? location)
        : this(
              name,
              Access.Public,
              Modifier.None, 
              ImmutableList<AttributeExpression>.Empty,
              propertyType, 
              null, 
              getMethod, 
              setMethod, 
              location, 
              null, 
              null)
    {
    }


    public override PropertyDeclaration WithName(string name) =>
        name == this.Name ? this :
        new PropertyDeclaration(
            name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override PropertyDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public PropertyDeclaration WithSymbol(PropertySymbol? symbol) =>
        symbol == this.Symbol ? this :
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override PropertyDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override PropertyDeclaration WithAccess(Access access) =>
        access == this.Access ? this :
        new PropertyDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.Attributes,
            this.PropertyType,
            this.BackingField,
            this.GetMethod.WithAccess(access),
            this.SetMethod != null ? this.SetMethod.WithAccess(access) : null,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override PropertyDeclaration WithModifiers(BitSet<Modifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new PropertyDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.Attributes,
            this.PropertyType,
            this.BackingField != null ? this.BackingField.WithModifiers(modifiers) : null,
            this.GetMethod.WithModifiers(modifiers | Modifier.HideBySig | Modifier.Special),
            this.SetMethod != null ? this.SetMethod.WithModifiers(modifiers | Modifier.HideBySig | Modifier.Special) : null,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override PropertyDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes) =>
        attributes == this.Attributes ? this :
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            attributes,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public PropertyDeclaration WithPropertyType(Expression? propertyType) =>
        propertyType == this.PropertyType ? this :
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            propertyType,
            this.BackingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public PropertyDeclaration WithBackingField(FieldDeclaration? backingField) =>
        backingField == this.BackingField ? this :
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.PropertyType,
            backingField,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public PropertyDeclaration WithGetMethod(MethodDeclaration getMethod) =>
        getMethod == this.GetMethod ? this :
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.PropertyType,
            this.BackingField,
            getMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public PropertyDeclaration WithSetMethod(MethodDeclaration? setMethod) =>
        setMethod == this.SetMethod ? this :
        new PropertyDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.PropertyType,
            this.BackingField,
            this.GetMethod,
            setMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override int ChildCount => 
        base.ChildCount + 4;

    public override SemanticElement? GetChild(int index)
    {
        if (index < base.ChildCount)
            return base.GetChild(index);
        index -= base.ChildCount;
        return index switch
        {
            0 => this.PropertyType,
            1 => this.GetMethod,
            2 => this.SetMethod,
            3 => this.BackingField,
            _ => null
        };
    }

    public override PropertyDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var attributes = rewriter.Rewrite(this.Attributes);
        var propType = rewriter.Rewrite(this.PropertyType);
        var backingField = rewriter.Rewrite(this.BackingField);
        var getMethod = rewriter.Rewrite(this.GetMethod);
        var setMethod = rewriter.Rewrite(this.SetMethod);
        return this
            .WithAttributes(attributes)
            .WithPropertyType(propType!)
            .WithBackingField(backingField)
            .WithGetMethod(getMethod!)
            .WithSetMethod(setMethod);
    }
}
