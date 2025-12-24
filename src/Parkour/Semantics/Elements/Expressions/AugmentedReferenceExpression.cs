namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// The base class for expressions that augment other type or member expressions;
/// such as <see cref="ArityExpression"/>, <see cref="ArrayExpression"/> or <see cref="ConstructExpression"/>.
/// </summary>
public abstract class AugmentedReferenceExpression : Expression
{
    /// <summary>
    /// The type of member whose reference is being augmented.
    /// </summary>
    public abstract Expression TypeOrMember { get; }

    internal protected AugmentedReferenceExpression(
        ContainsState state,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(state, location, resultType, diagnostics)
    {
    }
}
