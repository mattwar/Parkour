namespace Parkour.Binding;
using Semantics;
using Symbols;

public class ExpressionBinding
{
    /// <summary>
    /// The external symbols given as input to binding.
    /// </summary>
    public GlobalNamespaceSymbol ExternalSymbols { get; }

    /// <summary>
    /// The expression given as input to binding.
    /// </summary>
    public Expression UnboundExpression { get; }

    /// <summary>
    /// The expression after binding.
    /// </summary>
    public Expression BoundExpression { get; }

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
