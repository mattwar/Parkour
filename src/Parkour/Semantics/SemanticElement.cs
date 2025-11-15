namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// The base class for all semantic elements, including expressions, statements, and declarations.
/// Semantic elements start out unbound and become either bound or assigned diagnostics during semantic analysis.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class SemanticElement
{
    internal protected virtual string DebugText => $"{GetType().Name}";

    /// <summary>
    /// Any diagnostics associated with this <see cref="SemanticElement"/>.
    /// </summary>
    public ImmutableList<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// The source location corresponding to this <see cref="SemanticElement"/>.
    /// </summary>
    public ISourceLocation? Location { get; }

    /// <summary>
    /// The aggregated state of this <see cref="SemanticElement"/> and its descendants.
    /// </summary>
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

    /// <summary>
    /// Semantic state that aggregates from leaves to root.
    /// </summary>
    [Flags]
    internal protected enum ContainsState
    {
        None = 0,

        /// <summary>
        /// Expression is unbound.
        /// </summary>
        Unbound = 2,

        /// <summary>
        /// Expression has or contains diagnostics.
        /// </summary>
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

    /// <summary>
    /// Combine the <see cref="ContainsState"/> of multiple semantic elements.
    /// </summary>
    protected static ContainsState CombineState<TSemantic>(IEnumerable<TSemantic>? elements)
        where TSemantic : SemanticElement =>
        elements != null
            ? elements.Aggregate(ContainsState.None, (s, e) => s | State(e))
            : ContainsState.None;

    /// <summary>
    /// Accesses the <see cref="ContainsState"/> of a semantic element.
    /// </summary>
    protected static ContainsState State(SemanticElement? element) =>
        element != null ? element._state : ContainsState.None;

    /// <summary>
    /// An assertion that the symbol is not null.
    /// If the symbol is null the state will be <see cref="ContainsState.Unbound"/>.
    /// </summary>
    protected static ContainsState NotNullState(Symbol? symbol) =>
        symbol == null || symbol == SpecialSymbols.Unknown
            ? ContainsState.Unbound 
            : ContainsState.None;

    /// <summary>
    /// An assertion that the symbol is not null and there are no diagnostics.
    /// If either are true, the state is <see cref="ContainsState.Unbound"/>.
    /// </summary>
    protected static ContainsState NotNullOrDiagnosticState(Symbol? symbol, ImmutableList<Diagnostic>? diagnostics) =>
        (symbol == null || symbol == SpecialSymbols.Unknown) 
            && (diagnostics == null || diagnostics.Count == 0)
            ? ContainsState.Unbound
            : ContainsState.None;

    /// <summary>
    /// Creates a new instance of the semantic element with the specified source location.
    /// </summary>
    public abstract SemanticElement WithLocation(ISourceLocation? location);

    /// <summary>
    /// Creates a new instance of the semantic element with the specified diagnostics.
    /// </summary>
    public abstract SemanticElement WithDiagnostics(ImmutableList<Diagnostic> diagnostics);

    /// <summary>
    /// Invokes the rewriter on the children, returning a new semantic element if any children changed.
    /// </summary>
    public abstract SemanticElement RewriteChildren(SemanticRewriter rewriter);

    /// <summary>
    /// Returns a lowerer for this kind of semantic element.
    /// Elements that are not understood by the emitter must be lowered into elements that are understood.
    /// </summary>
    public virtual PartialLowerer? Lowerer => null;
}