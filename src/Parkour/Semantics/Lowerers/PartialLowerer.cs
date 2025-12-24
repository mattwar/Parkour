namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// A partial lowering step for a specific element type or feature.
/// </summary>
public abstract class PartialLowerer
{
    /// <summary>
    /// Rewrites high-level elements into low-level elements.
    /// </summary>
    public abstract ImmutableList<SemanticElement> Lower(
        ImmutableList<SemanticElement> elements,
        SymbolTable imports
        );
}