namespace Parkour.Semantics;
/// <summary>
/// Converts high-level semantics into low-level semantics,
/// rewriting declarations and expressions into a form compatible with emitting.
/// </summary>
public abstract class SemanticLowerer
{
    /// <summary>
    /// Rewrites high-level elements into low-level elements
    /// suitable for emitting.
    /// </summary>
    public abstract SemanticLowering Lower(
        SemanticBinding binding
        );
}