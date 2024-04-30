namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class IndexerDeclaration : MemberDeclaration
{
    public Expression ElementType { get; }
    public MethodDeclaration GetMethod { get; }
    public MethodDeclaration? SetMethod { get; }
    public IndexerSymbol? IndexerSymbol { get; }

    public IndexerDeclaration(
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression elementType,
        MethodDeclaration getMethod,
        MethodDeclaration? setMethod,
        ISourceLocation? location,
        IndexerSymbol? indexerSymbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
        State(elementType)
        | State(getMethod)
        | State(setMethod)
        | NotNullState(indexerSymbol),
        "Item",
        access,
        modifiers,
        location,
        diagnostics)
    {
        this.ElementType = elementType ?? getMethod.ReturnType;
        this.GetMethod = getMethod;
        this.SetMethod = setMethod;
        this.IndexerSymbol = indexerSymbol;
    }

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