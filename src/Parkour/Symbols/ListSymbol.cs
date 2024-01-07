namespace Parkour.Symbols;

public sealed class ListSymbol : TypeSymbol
{
    public TypeSymbol ElementType { get; }

    public ListSymbol(TypeSymbol elementType) : base($"List({elementType.Name})")
    {
        ElementType = elementType;
    }
}
