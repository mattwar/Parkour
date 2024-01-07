namespace Parkour.Expressions;
using Symbols;

public sealed class ReferenceExpression : Expression
{
    public string Name { get; }
    public override Symbol? ReferencedSymbol { get; }

    public ReferenceExpression(
        string name,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(
            ContainsState.None,
            resultType,
            diagnostics)
    {
        this.Name = name;
        this.ReferencedSymbol = referencedSymbol;
    }
}

