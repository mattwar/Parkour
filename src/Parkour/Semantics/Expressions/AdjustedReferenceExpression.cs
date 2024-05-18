namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// An expression that filters or maps the referenced symbols of the prior expression.
/// </summary>
public abstract class AdjustedReferenceExpression : Expression
{
    public abstract Expression TypeOrMember { get; }

    internal protected AdjustedReferenceExpression(
        ContainsState state,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(state, location, resultType, diagnostics)
    {
    }
}
