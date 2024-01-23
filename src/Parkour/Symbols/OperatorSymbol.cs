namespace Parkour.Symbols;

public class OperatorSymbol : LambdaSymbol
{
    public string Kind { get; }

    public OperatorSymbol(
        string name, 
        string kind, 
        Func<LambdaSymbol, ImmutableList<ParameterSymbol>> fnParameters, 
        Func<TypeSymbol> fnReturnType)
        : base(
            name, 
            fnParameters,
            fnReturnType,
            null)
    {
        this.Kind = kind;
    }
}
