namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class ParameterDeclaration : Declaration
{
    public Expression? ParameterType { get; }
    public ParameterSymbol? Symbol { get; }

    public ParameterDeclaration(
        string name, 
        Expression? parameterType,
        ISourceLocation? location,
        ParameterSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            ContainsState.None, 
            name, 
            location,
            diagnostics)
    {
        this.ParameterType = parameterType;
        this.Symbol = symbol;
    }
}