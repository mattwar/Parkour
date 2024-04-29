namespace Parkour.Emitting;
using Parkour.Binding;

public abstract class DeclarationEmitter
{
    public abstract EmitResult Emit(DeclarationBinding binding);
}

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