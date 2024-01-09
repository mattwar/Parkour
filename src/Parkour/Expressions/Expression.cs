namespace Parkour.Expressions;
using Symbols;
using Syntax;

public abstract class Expression
{
    public TypeSymbol ResultType { get; }
    public ImmutableList<Diagnostic> Diagnostics { get; }
    public SyntaxElement? Syntax { get; }
    internal ContainsState State { get; }

    public virtual Symbol? ReferencedSymbol => null;

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

    private protected Expression(
        ContainsState state, 
        TypeSymbol? resultType, 
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
    {
        this.State = state;
        this.ResultType = resultType ?? CommonSymbols.Unknown;
        this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        this.Syntax = syntax;

        if (this.ResultType == CommonSymbols.Unknown)
            this.State |= ContainsState.Unknowns;

        if (this.Diagnostics.Count > 0)
            this.State |= ContainsState.Diagnostics;
    }

    public string ToText() =>
        new ExpressionWriter().WriteExpression(this);

    /// <summary>
    /// Get all contained diagnostics
    /// </summary>
    public ImmutableList<Diagnostic> GetContainedDiagnostics() =>
        this.SelectWhere(s => s.HasDiagnostics, s => s.Diagnostics)
            .SelectMany(dx => dx)
            .ToImmutableList();

    internal static ContainsState CombineState(IEnumerable<Expression> items) =>
        items.Aggregate(ContainsState.None, (s, e) => s | e.State);
}

[Flags]
internal enum ContainsState
{
    None = 0,
    Unknowns = 2,
    Diagnostics = 4
}

