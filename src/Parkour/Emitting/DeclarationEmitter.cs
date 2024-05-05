namespace Parkour.Emitting;

using Semantics;
using Symbols;

/// <summary>
/// Emits declarations into a module.
/// </summary>
public abstract class DeclarationEmitter
{
    /// <summary>
    /// Emits all declarations.
    /// </summary>
    public abstract EmitResult Emit(ImmutableList<Declaration> declarations);

    public class EmitResult
    {
        public ImmutableList<Diagnostic> Diagnostics { get; }

        public EmitResult(ImmutableList<Diagnostic>? diagnostics)
        {
            this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        }
    }
}

