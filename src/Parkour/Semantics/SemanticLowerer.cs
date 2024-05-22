namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Converts high-level semantics into low-level semantics,
/// rewriting declarations and expressions into a form compatible with emitting.
/// </summary>
public abstract class SemanticLowerer
{
    /// <summary>
    /// Converts high-level declarations into low-level declarations.
    /// </summary>
    public abstract SemanticLowering Lower(
        ImmutableList<SemanticElement> elements,
        SymbolTable symbols);
}