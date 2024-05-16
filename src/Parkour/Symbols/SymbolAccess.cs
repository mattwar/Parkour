namespace Parkour.Symbols;

public class SymbolAccess
{
    private readonly string _name;

    public SymbolAccess(string name)
    {
        _name = name;
    }

    public override string ToString() => _name;

    public static SymbolAccess Public = new SymbolAccess(nameof(Public));
    public static SymbolAccess Private = new SymbolAccess(nameof(Private));
    public static SymbolAccess Protected = new SymbolAccess(nameof(Protected));
    public static SymbolAccess ProtectedAndInternal = new SymbolAccess(nameof(ProtectedAndInternal));
    public static SymbolAccess ProtectedOrInternal = new SymbolAccess(nameof(ProtectedOrInternal));
    public static SymbolAccess Internal = new SymbolAccess(nameof(Internal));
}
