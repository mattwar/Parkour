namespace Parkour.Expressions;
using Syntax;

[Flags]
internal enum ContainsState
{
    None = 0,
    Unknowns = 2,
    Diagnostics = 4
}

public abstract class SemanticElement
{
    public ImmutableList<Diagnostic> Diagnostics { get; }
    public SyntaxElement? Syntax { get; }
    internal ContainsState State { get; }

    /// <summary>
    /// This semantic or child semantics contains unknown/unbound elements.
    /// </summary>
    public bool ContainsUnknowns => (this.State & ContainsState.Unknowns) != 0;

    /// <summary>
    /// This semantic or child semantics contains diagnostics.
    /// </summary>
    public bool ContainsDiagnostics => (this.State & ContainsState.Diagnostics) != 0;

    /// <summary>
    /// This semantic has diagnostics
    /// </summary>
    public bool HasDiagnostics => this.Diagnostics.Count > 0;

    private protected SemanticElement(
        ContainsState state,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
    {
        this.State = state;
        this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        this.Syntax = syntax;
        if (this.Diagnostics.Count > 0)
            this.State |= ContainsState.Diagnostics;
    }

    public string ToText() =>
        new SemanticWriter().WriteToString(this);

    /// <summary>
    /// Get all contained diagnostics
    /// </summary>
    public ImmutableList<Diagnostic> GetContainedDiagnostics() =>
        this.SelectWhere(s => s.HasDiagnostics, s => s.Diagnostics)
            .SelectMany(dx => dx)
            .ToImmutableList();

    internal static ContainsState CombineState<TSemantic>(IEnumerable<TSemantic> items)
        where TSemantic : SemanticElement =>
        items.Aggregate(ContainsState.None, (s, e) => s | e.State);

    private string DebugText => ToText();
}

