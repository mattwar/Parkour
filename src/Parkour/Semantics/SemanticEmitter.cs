namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Emits low-level elements into a final representation.
/// </summary>
public abstract class SemanticEmitter
{
    /// <summary>
    /// Emits all low-level elements into final representation.
    /// </summary>
    public abstract EmitResult Emit(
        SemanticLowering lowering);

    public class EmitResult
    {
        public ImmutableList<Diagnostic> Diagnostics { get; }

        public EmitResult(ImmutableList<Diagnostic>? diagnostics)
        {
            this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        }
    }
}

