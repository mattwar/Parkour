namespace Parkour.Semantics;
using Symbols;
using Syntax;

public class OperatorExpression : Expression
{
    public string Kind { get; }
    public override Symbol? ReferencedSymbol { get; }

    public OperatorExpression(
        string kind, 
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
        this.Kind = kind;
        this.ReferencedSymbol = referencedSymbol;
    }
}
