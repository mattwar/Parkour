namespace Parkour.Semantics;

using Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class Declaration : SemanticElement
{
    private string DebugText => $"{GetType().Name}: {Name}";

    public string Name { get; }

    private protected Declaration(
        ContainsState state,
        string name,
        ISourceLocation? location,
        ImmutableList<Diagnostic>? diagnostics)
        : base(state, location, diagnostics)
    {
        this.Name = name;
    }

    public abstract Symbol? Symbol { get; }

    public abstract Declaration WithName(string name);
    public abstract Declaration WithLocation(ISourceLocation? location);
    public abstract Declaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics);
}