namespace Parkour.Symbols;

public class IntrinsicSymbol : FunctionSymbol
{
    public FunctionSymbol RelatedFunction { get; }

    public IntrinsicSymbol(string name, ImmutableList<ParameterSymbol> parameters, TypeSymbol? returnType, FunctionSymbol relatedFunction)
        : base(name, parameters, returnType)
    {
        RelatedFunction = relatedFunction;
    }
}
