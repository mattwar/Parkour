namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// The base class of all expressions.
/// Expressions represent instructions that compute values.
public abstract class Expression : SemanticElement
{
    internal protected override string DebugText => 
        $"{GetType().Name}: {ResultType?.FullName ?? "???"}";

    /// <summary>
    /// The result type of the expression, determined during semantic analysis.
    /// </summary>
    public TypeSymbol ResultType { get; }

    /// <summary>
    /// The symbol referenced by the expression, determined during semantic analysis.
    /// </summary>
    public virtual Symbol? ReferencedSymbol => null;

    private protected Expression(
        ContainsState state,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            state 
            | NotNullState(resultType), 
            location, 
            diagnostics)
    {
        this.ResultType = resultType ?? SpecialSymbols.Unknown;
    }

    /// <summary>
    /// Creates a new expression that is semantically equivalent to the current expression but with the <see cref="ResultType"/> set.
    /// </summary>
    public abstract Expression WithResultType(TypeSymbol? resultType);
}
