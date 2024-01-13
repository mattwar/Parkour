namespace Parkour.Symbols;

public static class SpecialSymbols
{
    public static readonly TypeSymbol Unknown = new TypeSymbol("Unknown", typeof(object));
    public static readonly TypeSymbol Null = new TypeSymbol("Null", typeof(object));
    public static readonly TypeSymbol Any = new TypeSymbol("Any", typeof(object));
    public static readonly TypeSymbol Void = new TypeSymbol("Void", typeof(void));
}
