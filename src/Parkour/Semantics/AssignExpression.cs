namespace Parkour.Semantics;
using Syntax;

public sealed class AssignExpression : Expression
{
    public Expression Target { get; }
    public Expression Source { get; }

    public AssignExpression(
        Expression target,
        Expression source,
        ISourceLocation? location,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            target.State | source.State, 
            location,
            target.ResultType,
            diagnostics)
    {
        this.Target = target;
        this.Source = source;
    }
}
