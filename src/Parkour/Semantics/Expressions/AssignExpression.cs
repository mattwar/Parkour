namespace Parkour.Semantics;

/// <summary>
/// Assigns the result of the source expression to the location
/// specified by the target expression.
/// </summary>
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
            State(target)
            | State(source), 
            location,
            target.ResultType,
            diagnostics)
    {
        this.Target = target;
        this.Source = source;
    }

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Target,
            1 => this.Source,
            _ => null
        };
}
