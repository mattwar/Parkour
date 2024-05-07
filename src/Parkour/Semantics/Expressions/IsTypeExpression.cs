namespace Parkour.Semantics;

using Symbols;

public class IsTypeExpression : Expression
{
    public Expression Expression { get; }
    public Expression Type { get; }

    public IsTypeExpression(
        Expression expression,
        Expression type,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression)
            | State(type)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Expression = expression;
        this.Type = type;
    }

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            1 => this.Type,
            _ => null
        };
}
