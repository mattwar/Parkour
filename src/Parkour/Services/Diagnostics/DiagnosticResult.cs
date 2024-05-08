namespace Parkour.Services;

public record DiagnosticResult(ImmutableList<Diagnostic> Diagnostics)
{
    public static DiagnosticResult Empty = 
        new DiagnosticResult(ImmutableList<Diagnostic>.Empty);
}
