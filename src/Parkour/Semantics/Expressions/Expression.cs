namespace Parkour.Semantics;

using Symbols;

public abstract class Expression : SemanticElement
{
    internal protected override string DebugText => 
        $"{GetType().Name}: {ResultType?.FullName ?? "???"}";

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

    public abstract Expression WithResultType(TypeSymbol? resultType);
}
