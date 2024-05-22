namespace Parkour.Semantics;

public class SemanticLowering
{
    public ImmutableList<SemanticElement> Elements { get; }
    public ImmutableList<Diagnostic> Diagnostics { get; }

    public SemanticLowering(
        ImmutableList<SemanticElement> elements,
        ImmutableList<Diagnostic>? diagnostics = null)
    {
        this.Elements = elements;

        if (diagnostics == null)
        {
            var dx = new List<Diagnostic>();
            foreach (var elem in elements)
            {
                elem.GetContainedDiagnostics(dx);
            }
            diagnostics = dx.ToImmutableList();
        }

        this.Diagnostics = diagnostics;
    }
}