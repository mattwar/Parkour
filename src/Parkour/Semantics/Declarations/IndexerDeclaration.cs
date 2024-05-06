namespace Parkour.Semantics;

using Symbols;

public sealed class IndexerDeclaration : MemberDeclaration
{
    public override IndexerSymbol? Symbol { get; }

    public Expression ElementType { get; }
    public MethodDeclaration GetMethod { get; }
    public MethodDeclaration? SetMethod { get; }

    public IndexerDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression elementType,
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
        this.ElementType = elementType ?? getMethod.ReturnType;
        this.GetMethod = getMethod;
        this.SetMethod = setMethod;
        this.Symbol = symbol;
    }

    public IndexerDeclaration(
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression elementType,
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

    public override IndexerDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
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
        new IndexerDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.ElementType,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override IndexerDeclaration WithModifiers(SymbolModifier modifiers) =>
        new IndexerDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.ElementType,
            this.GetMethod,
            this.SetMethod,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public IndexerDeclaration WithElementType(Expression elementType) =>
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
}