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
            State(parameterType)
            | NotNullState(parameterSymbol), 
            name, 
            location,
            diagnostics)
    {
        this.ParameterType = parameterType;
        this.ParameterSymbol = parameterSymbol;
    }

    public override Symbol? DeclaredSymbol => this.ParameterSymbol;

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        this.ParameterType;
}