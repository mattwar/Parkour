namespace Parkour.Emitting;
using Parkour.Lowering;

/// <summary>
/// Emits lowered symbols into a <see cref="ModuleBuilder"/>.
/// </summary>
public abstract class Emitter
{
    /// <summary>
    /// Emits lowered symbols into a <see cref="ModuleBuilder"/>
    /// </summary>
    public abstract EmitResult Emit(DeclarationLowering binding, ModuleBuilder builder);

    public class EmitResult
    {
        public ImmutableList<Diagnostic> Diagnostics { get; }
        public bool Success { get; }

        public EmitResult(ImmutableList<Diagnostic>? diagnostics)
        {
            this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
            this.Success = Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);
        }
    }
}

