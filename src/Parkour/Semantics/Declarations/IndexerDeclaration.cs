namespace Parkour.Semantics;

using Symbols;

public sealed class IndexerDeclaration : MemberDeclaration
{
    public override IndexerSymbol? Symbol { get; }

    public Expression? ElementType { get; }
    public MethodDeclaration GetMethod { get; }
    public MethodDeclaration? SetMethod { get; }

    public IndexerDeclaration(
        string name,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
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
        location,
        diagnostics)
    {
        this.ElementType = elementType;
        this.GetMethod = getMethod;
        this.SetMethod = setMethod;
        this.Symbol = symbol;
    }

    public IndexerDeclaration(
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Expression? elementType,
        MethodDeclaration getMethod,
        MethodDeclaration? setMethod,
        ISourceLocation? location,
        IndexerSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
        : this(
              "Item",
              access,
              modifiers,
              elementType,
              getMethod,
              setMethod,
              location,
              symbol,
              diagnostics
              )
    {
    }

    public override IndexerDeclaration WithName(string name) =>
        name == this.Name ? this :
        new IndexerDeclaration(
            name,
            this.Access,
            this.Modifiers,
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
            this.ElementType,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override IndexerDeclaration WithAccess(SymbolAccess access) =>
        access == this.Access ? this :
        new IndexerDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.ElementType,
            this.GetMethod.WithAccess(access),
            this.SetMethod != null ? this.SetMethod.WithAccess(access) : null,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override IndexerDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new IndexerDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.ElementType,
            this.GetMethod.WithModifiers(modifiers | SymbolModifier.HideBySig | SymbolModifier.Special),
            this.SetMethod != null ? this.SetMethod.WithModifiers(modifiers | SymbolModifier.HideBySig | SymbolModifier.Special) : null,
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
            this.ElementType,
            this.GetMethod,
            setMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override int ChildCount => 3;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.ElementType,
            1 => this.GetMethod,
            2 => this.SetMethod,
            _ => null
        };

    public override IndexerDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var elementType = rewriter.Rewrite(this.ElementType);
        var getMethod = rewriter.Rewrite(this.GetMethod);
        var setMethod = rewriter.Rewrite(this.SetMethod);
        return this
            .WithElementType(elementType!)
            .WithGetMethod(getMethod!)
            .WithSetMethod(setMethod);
    }
}