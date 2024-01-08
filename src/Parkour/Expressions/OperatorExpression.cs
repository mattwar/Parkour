namespace Parkour.Expressions;
using Symbols;

public class OperatorExpression : Expression
{
    public string Kind { get; }
    public override Symbol? ReferencedSymbol { get; }

    public OperatorExpression(
        string kind, 
        Symbol? referencedSymbol, 
        TypeSymbol? resultType, 
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(ContainsState.None, resultType, diagnostics)
    {
        this.Kind = kind;
        this.ReferencedSymbol = referencedSymbol;
    }
}
