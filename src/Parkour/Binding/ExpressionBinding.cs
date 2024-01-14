namespace Parkour.Binding;
using Semantics;
using Symbols;

public class ExpressionBinding
{
    public Expression UnboundExpression { get; }
    public Expression BoundExpression { get; }
    public NamespaceSymbol ExternalSymbols { get; }

    public ExpressionBinding(
        Expression unbound, 
        Expression bound,
        NamespaceSymbol externalSymbols)
    {
        this.UnboundExpression = unbound;
        this.BoundExpression = bound;
        this.ExternalSymbols = externalSymbols;
    }
}
