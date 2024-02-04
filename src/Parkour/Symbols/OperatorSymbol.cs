namespace Parkour.Symbols;

public class OperatorSymbol : FunctionSymbol
{
    public string Kind { get; }

    public OperatorSymbol(
        string name, 
        string kind, 
        Func<FunctionSymbol, ImmutableList<ParameterSymbol>> fnParameters, 
        Func<TypeSymbol> fnReturnType)
        : base(
            name,
            null,
            fnParameters,
            fnReturnType)
    {
        this.Kind = kind;
    }
}
