using Parkour.Symbols;

public class TypeParameterSymbol : TypeSymbol
{
    public TypeParameterSymbol(string name, Type? runtimeType)
        : base(name, runtimeType)
    {
    }
}