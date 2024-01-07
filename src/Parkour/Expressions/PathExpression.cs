namespace Parkour.Expressions;

public sealed class PathExpression : Expression
{
    public Expression Expression { get; }
    public ReferenceExpression Reference { get; }

    public PathExpression(
        Expression expression,
        ReferenceExpression reference,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(expression.State | reference.State, reference.ResultType, diagnostics)
    {
        this.Expression = expression;
        this.Reference = reference;
    }
}

