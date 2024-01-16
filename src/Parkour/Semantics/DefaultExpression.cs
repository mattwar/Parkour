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
            ContainsState.None,
            location,
            resultType,
            diagnostics)
    {
        this.TypeExpression = typeExpression;
    }
}
