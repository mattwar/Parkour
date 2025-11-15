using Parkour;
using Parkour.Symbols;

public class TypeParameterSymbol : TypeSymbol
{
    public TypeParameterSymbol(
        string name,
        Symbol? declaringSymbol,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes = null)
        : base(name, declaringSymbol, Access.Public, Modifier.None, fnAttributes)
    {
    }
}