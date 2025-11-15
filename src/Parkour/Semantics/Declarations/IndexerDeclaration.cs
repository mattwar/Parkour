namespace Parkour.Semantics;

using Parkour;
using Symbols;

public sealed class IndexerDeclaration : MemberDeclaration
{
    public override IndexerSymbol? Symbol { get; }

    public Expression? ElementType { get; }
    public MethodDeclaration GetMethod { get; }
    public MethodDeclaration? SetMethod { get; }

    private IndexerDeclaration(
        string name,
        Access access,
        BitSet<Modifier> modifiers,
        ImmutableList<AttributeExpression> attributes,
        Expression? elementType,
        MethodDeclaration getMethod,
        MethodDeclaration? setMethod,
        ISourceLocation? location,
        IndexerSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
        State(elementType)
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
        this.ElementType = elementType;
        this.GetMethod = getMethod;
        this.SetMethod = setMethod;
        this.Symbol = symbol;
    }

    public IndexerDeclaration(
        Expression? elementType,
        MethodDeclaration getMethod,
        MethodDeclaration? setMethod,
        ISourceLocation? location)
        : this(
              "Item",
              Access.Public,
              Modifier.None,
              ImmutableList<AttributeExpression>.Empty,
              elementType, 
              getMethod, 
              setMethod, 
              location, 
              null, 
              null)
    {
    }

    public override IndexerDeclaration WithName(string name) =>
        name == this.Name ? this :
        new IndexerDeclaration(
            name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.ElementType,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override IndexerDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new IndexerDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.ElementType,
            this.GetMethod,
            this.SetMethod,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public IndexerDeclaration WithSymbol(IndexerSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new IndexerDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.ElementType,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override IndexerDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new IndexerDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.ElementType,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override IndexerDeclaration WithAccess(Access access) =>
        access == this.Access ? this :
        new IndexerDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.Attributes,
            this.ElementType,
            this.GetMethod.WithAccess(access),
            this.SetMethod != null ? this.SetMethod.WithAccess(access) : null,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override IndexerDeclaration WithModifiers(BitSet<Modifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new IndexerDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.Attributes,
            this.ElementType,
            this.GetMethod.WithModifiers(modifiers | Modifier.HideBySig | Modifier.Special),
            this.SetMethod != null ? this.SetMethod.WithModifiers(modifiers | Modifier.HideBySig | Modifier.Special) : null,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override IndexerDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes) =>
        attributes == this.Attributes ? this :
        new IndexerDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            attributes,
            this.ElementType,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public IndexerDeclaration WithElementType(Expression? elementType) =>
        elementType == this.ElementType ? this :
        new IndexerDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            elementType,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public IndexerDeclaration WithGetMethod(MethodDeclaration getMethod) =>
        getMethod == this.GetMethod ? this : 
        new IndexerDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.ElementType,
            getMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public IndexerDeclaration WithSetMethod(MethodDeclaration? setMethod) =>
        setMethod == this.SetMethod ? this :
        new IndexerDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.ElementType,
            this.GetMethod,
            setMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override int ChildCount => 
        base.ChildCount + 3;

    public override SemanticElement? GetChild(int index)
    {
        if (index < base.ChildCount)
            return base.GetChild(index);
        index -= base.ChildCount;
        return index switch
        {
            0 => this.ElementType,
            1 => this.GetMethod,
            2 => this.SetMethod,
            _ => null
        };
    }

    public override IndexerDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var attributes = rewriter.Rewrite(this.Attributes);
        var elementType = rewriter.Rewrite(this.ElementType);
        var getMethod = rewriter.Rewrite(this.GetMethod);
        var setMethod = rewriter.Rewrite(this.SetMethod);
        return this
            .WithAttributes(attributes)
            .WithElementType(elementType!)
            .WithGetMethod(getMethod!)
            .WithSetMethod(setMethod);
    }
}