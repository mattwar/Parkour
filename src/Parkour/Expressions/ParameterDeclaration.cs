namespace Parkour.Expressions;
using Symbols;
using Syntax;

public sealed class ParameterDeclaration : Declaration
{
    public Expression? ParameterType { get; }
    public ParameterSymbol? Symbol { get; }

    public ParameterDeclaration(
        string name, 
        Expression? parameterType,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax,
        ParameterSymbol? symbol)
        : base(
            ContainsState.None, 
            name, 
            diagnostics,
            syntax)
    {
        this.ParameterType = parameterType;
        this.Symbol = symbol;
    }
}