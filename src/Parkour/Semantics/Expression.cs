namespace Parkour.Semantics;
using Symbols;
using Syntax;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class Expression : SemanticElement
{
    private string DebugText => $"{GetType().Name}: {ResultType.Name}";

    public TypeSymbol ResultType { get; }

    public virtual Symbol? ReferencedSymbol => null;

    private protected Expression(
        ContainsState state,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(state | GetResultState(resultType), location, diagnostics)
    {
        this.ResultType = resultType ?? CommonSymbols.Unknown;
    }

    private static ContainsState GetResultState(TypeSymbol? resultType) =>
        (resultType == null || resultType == CommonSymbols.Unknown)
            ? ContainsState.Unknowns
            : ContainsState.None;
}
