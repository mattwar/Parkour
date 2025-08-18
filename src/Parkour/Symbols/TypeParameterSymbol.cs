using Parkour.Symbols;

public class TypeParameterSymbol : TypeSymbol
{
    public TypeParameterSymbol(
        string name,
        Symbol? declaringSymbol,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes = null)
        : base(name, declaringSymbol, SymbolAccess.Public, SymbolModifier.None, fnAttributes)
    {
    }
}