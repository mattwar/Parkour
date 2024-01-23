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

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            1 => this.Reference,
            _ => null
        };
}

