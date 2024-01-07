using System.Diagnostics.CodeAnalysis;

namespace Parkour.Symbols;

public class SymbolEqualityComparer : IEqualityComparer<Symbol>
{
    private SymbolEqualityComparer() { }
    public static SymbolEqualityComparer Instance = new SymbolEqualityComparer();

    public bool Equals(Symbol? symbol1, Symbol? symbol2)
    {
        if (symbol1 == symbol2) return true;

        if (symbol1 == null && symbol2 == null) return true;
        if (symbol1 == null || symbol2 == null) return false;

        switch (symbol1)
        {
            case TypeSymbol type1 when symbol2 is TypeSymbol type2:
                return TypeEqualityComparer.Instance.Equals(type1, type2);
            default:
                return false;
        }
    }

    public int GetHashCode([DisallowNull] Symbol symbol)
    {
        switch (symbol)
        {
            case TypeSymbol type:
                return TypeEqualityComparer.Instance.GetHashCode(type);
            default:
                return symbol.GetHashCode();
        }
    }
}
