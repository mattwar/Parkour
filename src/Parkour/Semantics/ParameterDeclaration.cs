namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class ParameterDeclaration : Declaration
{
    public Expression? ParameterType { get; }
    public ParameterSymbol? ParameterSymbol { get; }

    public ParameterDeclaration(
        string name, 
        Expression? parameterType,
        ISourceLocation? location,
        ParameterSymbol? parameterSymbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            OptionalState(parameterType)
            | NotNullState(parameterSymbol), 
            name, 
            location,
            diagnostics)
    {
        this.ParameterType = parameterType;
        this.ParameterSymbol = parameterSymbol;
    }
}