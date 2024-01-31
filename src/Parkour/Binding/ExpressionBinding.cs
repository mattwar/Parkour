namespace Parkour.Binding;
using Semantics;
using Symbols;

public class ExpressionBinding
{
    public Expression UnboundExpression { get; }
    public Expression BoundExpression { get; }
    public GlobalNamespaceSymbol ExternalSymbols { get; }

    public ExpressionBinding(
        Expression unbound, 
        Expression bound,
        GlobalNamespaceSymbol externalSymbols)
    {
        this.UnboundExpression = unbound;
        this.BoundExpression = bound;
        this.ExternalSymbols = externalSymbols;
    }
}
