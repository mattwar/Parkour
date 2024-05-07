namespace Parkour.Semantics;
using Symbols;

public sealed class DefaultExpression : Expression
{
    public Expression? Type { get; }

    public DefaultExpression(
        Expression? type,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Type = type;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Type,
            _ => null
        };
}
