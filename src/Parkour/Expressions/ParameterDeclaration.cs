namespace Parkour.Expressions;
using Symbols;
using Syntax;

public sealed class ParameterDeclaration : Declaration
{
    public Expression? ParameterType { get; }

    public ParameterDeclaration(
        string name, 
        Expression? parameterType,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            ContainsState.None, 
            name, 
            SymbolAccess.Public, 
            SymbolModifier.None, 
            diagnostics,
            syntax)
    {
        this.ParameterType = parameterType;
    }
}