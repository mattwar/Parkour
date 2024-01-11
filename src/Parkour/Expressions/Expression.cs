namespace Parkour.Expressions;
using Symbols;
using Syntax;

public abstract class Expression : SemanticElement
{
    public TypeSymbol ResultType { get; }

    public virtual Symbol? ReferencedSymbol => null;

    private protected Expression(
        ContainsState state, 
        TypeSymbol? resultType, 
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(state | GetResultState(resultType), diagnostics, syntax)
    {
        this.ResultType = resultType ?? CommonSymbols.Unknown;
    }

    private static ContainsState GetResultState(TypeSymbol? resultType) =>
        (resultType == null || resultType == CommonSymbols.Unknown)
            ? ContainsState.Unknowns
            : ContainsState.None;
}
