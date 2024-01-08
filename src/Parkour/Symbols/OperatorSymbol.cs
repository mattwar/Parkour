namespace Parkour.Symbols;

public class OperatorSymbol : FunctionSymbol
{
    public string Kind { get; }

    public OperatorSymbol(
        string name, 
        string kind, 
        ImmutableList<ParameterSymbol> parameters, 
        TypeSymbol? returnType)
        : base(name, parameters, returnType)
    {
        this.Kind = kind;
    }
}
