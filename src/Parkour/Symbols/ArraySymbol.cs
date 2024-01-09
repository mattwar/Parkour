namespace Parkour.Symbols;

public sealed class ArraySymbol : TypeSymbol
{
    public TypeSymbol ElementType { get; }

    public ArraySymbol(TypeSymbol elementType) 
        : base($"Array({elementType.Name})")
    {
        ElementType = elementType;
    }
}
