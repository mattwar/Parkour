namespace Parkour.Expressions;

public sealed class AssignExpression : Expression
{
    public Expression Target { get; }
    public Expression Expression { get; }

    public AssignExpression(
        Expression target,
        Expression expression,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(target.State | expression.State, target.ResultType, diagnostics)
    {
        this.Target = target;
        this.Expression = expression;
    }
}
