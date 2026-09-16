namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Emits low-level semantic elements into a target representation.
/// </summary>
public abstract class SemanticEmitter
{
    /// <summary>
    /// Emits all low-level elements into a target representation.
    /// This is typically an IL assembly, machine code, or some other executable format.
    /// </summary>
    public abstract SemanticEmitting Emit(
        SemanticLowering lowering);
}

