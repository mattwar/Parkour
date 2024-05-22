namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Emits semantics into a module.
/// </summary>
public abstract class SemanticEmitter
{
    /// <summary>
    /// Emits all semantics.
    /// </summary>
    public abstract EmitResult Emit(
        ImmutableList<SemanticElement> elements);

    public class EmitResult
    {
        public ImmutableList<Diagnostic> Diagnostics { get; }

        public EmitResult(ImmutableList<Diagnostic>? diagnostics)
        {
            this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        }
    }
}

