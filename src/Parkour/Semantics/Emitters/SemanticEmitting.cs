namespace Parkour.Semantics;

public class SemanticEmitting
{
    /// <summary>
    /// Any diagnostics produced during emission.
    /// </summary>
    public ImmutableList<Diagnostic> Diagnostics { get; }

    public SemanticEmitting(ImmutableList<Diagnostic>? diagnostics)
    {
        this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
    }
}
