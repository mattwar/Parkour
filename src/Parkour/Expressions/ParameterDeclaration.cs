namespace Parkour.Expressions;
using Symbols;
using Analysis;

public sealed class ParameterDeclaration : Declaration
{
    public TypeSymbol ParameterType { get; }

    public ParameterDeclaration(string name, TypeSymbol? parameterType)
        : base(ContainsState.None, name, SymbolAccess.Public, SymbolModifier.None, null)
    {
        this.ParameterType = parameterType ?? SymbolModel.Any;
    }
}