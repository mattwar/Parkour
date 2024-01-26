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

            case MethodSymbol method1 when symbol2 is MethodSymbol method2:
                // must both be constructed with same definition
                if (!method1.IsConstructed 
                    || !method2.IsConstructed
                    || method1.ConstructedFrom != method2.ConstructedFrom
                    || method1.TypeArguments.Count != method2.TypeArguments.Count)
                    return false;

                // must have same type arguments.
                for (int i = 0; i < method1.TypeArguments.Count; i++)
                {
                    if (!Equals(method1.TypeArguments[i], method2.TypeArguments[i]))
                        return false;
                }

                return true;

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
            case MethodSymbol method:
                if (method.IsConstructed && method.ConstructedFrom != null)
                {
                    var hc = GetHashCode(method.ConstructedFrom);

                    for (int i = 0; i < method.TypeArguments.Count; i++)
                    {
                        hc = HashCode.Combine(GetHashCode(method.TypeArguments[i]));
                    }
                    return hc;
                }
                goto default;

            default:
                return symbol.GetHashCode();
        }
    }
}
