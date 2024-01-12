namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class ReferenceExpression : Expression
{
    public string Name { get; }
    public override Symbol? ReferencedSymbol { get; }

    public ReferenceExpression(
        string name,
        ISourceLocation? location,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            ContainsState.None,
            location,
            resultType,
            diagnostics)
    {
        this.Name = name;
        this.ReferencedSymbol = referencedSymbol;
    }
}

