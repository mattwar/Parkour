namespace Parkour.Symbols;

public class OperatorSymbol : FunctionSymbol
{
    public string Kind => this.Name;

    public OperatorSymbol(
        string kind, 
        Func<FunctionSymbol, ImmutableList<ParameterSymbol>> fnParameters, 
        Func<TypeSymbol> fnReturnType)
        : base(
            kind,
            null,
            fnParameters,
            fnReturnType)
    {
    }
}
