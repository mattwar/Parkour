namespace Parkour.Semantics;
using Symbols;

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
    public ISourceLocation? Location { get; }
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
        ISourceLocation? location,
        ImmutableList<Diagnostic>? diagnostics
        )
    {
        this.State = state;
        this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        this.Location = location;
        if (this.Diagnostics.Count > 0)
            this.State |= ContainsState.Diagnostics;
    }

    public string ToText() =>
        new SemanticWriter().WriteToString(this);

    /// <summary>
    /// Get all contained diagnostics that match the filter
    /// </summary>
    public void GetContainedDiagnostics(Func<Diagnostic, bool>? filter, List<Diagnostic> diagnostics)
    {
        diagnostics.AddRange(
            this.SelectWhere(s => s.HasDiagnostics, s => s.Diagnostics)
            .SelectMany(dx => dx)
            .Where(d => filter == null || filter(d))
            );
    }

    /// <summary>
    /// Get all contained diagnostics.
    /// </summary>
    public void GetContainedDiagnostics(List<Diagnostic> diagnostics) =>
        GetContainedDiagnostics(null, diagnostics);

    internal static ContainsState CombineState<TSemantic>(IEnumerable<TSemantic>? items)
        where TSemantic : SemanticElement =>
        items != null
            ? items.Aggregate(ContainsState.None, (s, e) => s | e.State)
            : ContainsState.None;

    internal static ContainsState OptionalState(SemanticElement? element) =>
        element != null ? element.State : ContainsState.None;

    internal static ContainsState NotNullState(Symbol? symbol) =>
        symbol == null || symbol == SpecialSymbols.Unknown
            ? ContainsState.Unknowns 
            : ContainsState.None;

    internal static ContainsState NotNullOrDiagnosticState(Symbol? symbol, ImmutableList<Diagnostic>? diagnostics) =>
        (symbol == null || symbol == SpecialSymbols.Unknown) && (diagnostics == null || diagnostics.Count == 0)
            ? ContainsState.Unknowns
            : ContainsState.None;
}

