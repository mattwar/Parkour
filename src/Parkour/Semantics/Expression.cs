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
        : base(
            state 
            | NotNullState(resultType), 
            location, 
            diagnostics)
    {
        this.ResultType = resultType ?? SpecialSymbols.Unknown;
    }
}
