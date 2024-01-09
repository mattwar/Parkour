namespace Parkour.Expressions;
using Syntax;

public sealed class PathExpression : Expression
{
    public Expression Expression { get; }
    public ReferenceExpression Reference { get; }

    public PathExpression(
        Expression expression,
        ReferenceExpression reference,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            expression.State | reference.State, 
            reference.ResultType, 
            diagnostics,
            syntax)
    {
        this.Expression = expression;
        this.Reference = reference;
    }
}

