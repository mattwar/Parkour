namespace Parkour.Expressions;
using Symbols;
using Analysis;

public sealed class ParameterDeclaration : Declaration
{
    public Expression? ParameterType { get; }

    public ParameterDeclaration(string name, Expression? parameterType)
        : base(ContainsState.None, name, SymbolAccess.Public, SymbolModifier.None, null)
    {
        this.ParameterType = parameterType;
    }
}