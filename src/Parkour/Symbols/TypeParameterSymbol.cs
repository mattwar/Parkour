using Parkour.Symbols;

public class TypeParameterSymbol : TypeSymbol
{
    public TypeParameterSymbol(
        string name,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes = null)
        : base(name, fnAttributes)
    {
    }
}