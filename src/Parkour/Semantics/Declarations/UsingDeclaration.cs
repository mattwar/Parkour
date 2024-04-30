namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Puts the specified symbol and its members in scope.
/// </summary>
public class UsingDeclaration : Declaration
{
    public Expression Expression { get; }
    public AliasSymbol? AliasedSymbol { get; }

    public UsingDeclaration(
        string name,
        Expression expression,
        ISourceLocation? location,
        AliasSymbol? aliasSymbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression),
            name,
            location,
            diagnostics)
    {
        this.Expression = expression;
        this.AliasedSymbol = aliasSymbol;
    }

    public override int ChildCount => 1;
    public override SemanticElement? GetChild(int index) =>
        index == 0 ? this.Expression : null;
}
