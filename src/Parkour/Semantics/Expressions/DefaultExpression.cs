namespace Parkour.Semantics;
using Symbols;

public sealed class DefaultExpression : Expression
{
    public Expression? TypeExpression { get; }

    public DefaultExpression(
        Expression? typeExpression,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.TypeExpression = typeExpression;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.TypeExpression,
            _ => null
        };
}
