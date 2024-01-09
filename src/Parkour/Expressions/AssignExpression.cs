namespace Parkour.Expressions;
using Syntax;

public sealed class AssignExpression : Expression
{
    public Expression Target { get; }
    public Expression Expression { get; }

    public AssignExpression(
        Expression target,
        Expression expression,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            target.State | expression.State, 
            target.ResultType, 
            diagnostics,
            syntax)
    {
        this.Target = target;
        this.Expression = expression;
    }
}
