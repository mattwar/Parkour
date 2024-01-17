namespace Parkour.Semantics;
using Syntax;

public sealed class PathExpression : Expression
{
    public Expression Expression { get; }
    public ReferenceExpression Reference { get; }

    public PathExpression(
        Expression expression,
        ReferenceExpression reference,
        ISourceLocation? location,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            expression.State 
            | reference.State, 
            location,
            reference.ResultType, 
            diagnostics)
    {
        this.Expression = expression;
        this.Reference = reference;
    }
}

