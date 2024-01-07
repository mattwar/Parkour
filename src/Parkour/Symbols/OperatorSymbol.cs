namespace Parkour.Symbols;

public class OperatorSymbol : FunctionSymbol
{
    public OperatorSymbol(string name, ImmutableList<ParameterSymbol> parameters, TypeSymbol? returnType)
        : base(name, parameters, returnType)
    {
    }
}
