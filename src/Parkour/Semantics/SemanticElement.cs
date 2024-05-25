namespace Parkour.Semantics;

using Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class SemanticElement
{
    internal protected virtual string DebugText => $"{GetType().Name}";

    public ImmutableList<Diagnostic> Diagnostics { get; }
    public ISourceLocation? Location { get; }
    private readonly ContainsState _state;

    /// <summary>
    /// This <see cref="SemanticElement"/> or its descendants is unbound.
    /// </summary>
    public bool IsUnbound => (_state & ContainsState.Unbound) != 0;

    /// <summary>
    /// This <see cref="SemanticElement"/> or its descendants has diagnostics.
    /// </summary>
    public bool ContainsDiagnostics => (_state & ContainsState.Diagnostics) != 0;

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
        _state = state;
        Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        Location = location;
        if (Diagnostics.Count > 0)
            _state |= ContainsState.Diagnostics;
    }

    /// <summary>
    /// The number of child elements this <see cref="SemanticElement"/> contains.
    /// </summary>
    public abstract int ChildCount { get; }

    /// <summary>
    /// Gets the child <see cref="SemanticElement"/> at the specified index.
    /// </summary>
    public abstract SemanticElement? GetChild(int index);

    /// <summary>
    /// Get the first element at the location specified.
    /// </summary>
    public virtual SemanticElement? GetElementAtLocation(int position)
    {
        return GetElement(this);

        SemanticElement? GetElement(SemanticElement element)
        {
            if (element.Location is { } location
                && position >= location.Start
                && position < location.End)
            {
                return element;
            }

            for (int i = 0, n = element.ChildCount; i < n; i++)
            {
                var child = element.GetChild(i);
                if (child != null)
                {
                    var childBest = GetElement(child);
                    if (childBest != null)
                        return childBest;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Returns the element in a textual form, useful for debugging.
    /// </summary>
    public string ToDebugText() =>
        new SemanticWriter().WriteToString(this);

    [Flags]
    internal protected enum ContainsState
    {
        None = 0,
        Unbound = 2,
        Diagnostics = 4
    }

    /// <summary>
    /// Get all contained diagnostics.
    /// </summary>
    public ImmutableList<Diagnostic> GetContainedDiagnostics()
    {
        if (this.ContainsDiagnostics)
        {
            return
                this.SelectWhere(s => s.HasDiagnostics, s => s.Diagnostics)
                .SelectMany(dx => dx)
                .ToImmutableList();
        }
        else
        {
            return ImmutableList<Diagnostic>.Empty;
        }
    }

    protected static ContainsState CombineState<TSemantic>(IEnumerable<TSemantic>? items)
        where TSemantic : SemanticElement =>
        items != null
            ? items.Aggregate(ContainsState.None, (s, e) => s | State(e))
            : ContainsState.None;

    protected static ContainsState State(SemanticElement? element) =>
        element != null ? element._state : ContainsState.None;

    protected static ContainsState NotNullState(Symbol? symbol) =>
        symbol == null || symbol == SpecialSymbols.Unknown
            ? ContainsState.Unbound 
            : ContainsState.None;

    protected static ContainsState NotNullOrDiagnosticState(Symbol? symbol, ImmutableList<Diagnostic>? diagnostics) =>
        (symbol == null || symbol == SpecialSymbols.Unknown) 
            && (diagnostics == null || diagnostics.Count == 0)
            ? ContainsState.Unbound
            : ContainsState.None;

    public abstract SemanticElement WithLocation(ISourceLocation? location);
    public abstract SemanticElement WithDiagnostics(ImmutableList<Diagnostic> diagnostics);

    /// <summary>
    /// Invokes the rewriter on the children
    /// </summary>
    public abstract SemanticElement RewriteChildren(SemanticRewriter rewriter);

    /// <summary>
    /// Returns a lowerer for this kind of semantic element.
    /// Elements that are not understood by the emitter must be lowered into elements that are understood.
    /// </summary>
    public virtual PartialLowerer? Lowerer => null;
}