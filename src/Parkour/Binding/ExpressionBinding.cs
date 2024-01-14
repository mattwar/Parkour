namespace Parkour.Binding;
using Semantics;

public class ExpressionBinding
{
    public Expression UnboundExpression { get; }
    public Expression BoundExpression { get; }

    public ExpressionBinding(
        Expression unbound, 
        Expression bound)
    {
        this.UnboundExpression = unbound;
        this.BoundExpression = bound;
    }
}
