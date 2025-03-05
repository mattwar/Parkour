using System.Diagnostics.CodeAnalysis;

namespace Parkour.Symbols;

public class TypeEqualityComparer : IEqualityComparer<TypeSymbol>
{
    public static TypeEqualityComparer Instance = new TypeEqualityComparer();

    private TypeEqualityComparer() { }

    public bool Equals(TypeSymbol? type1, TypeSymbol? type2)
    {
        // same instance
        if (type1 == type2) return true;

        if (type1 == null && type2 == null) return true;
        if (type1 == null || type2 == null) return false;

        switch (type1)
        {
            case ArraySymbol array1 when type2 is ArraySymbol array2:
                return array1.Dimensions == array2.Dimensions
                    && array1.IsSZArray == array2.IsSZArray
                    && Equals(array1.ElementType, array2.ElementType);

            case UnionSymbol union1 when type2 is UnionSymbol union2:
                if (union1.Types.Count != union2.Types.Count)
                    return false;
                for (int i = 0; i < union1.Types.Count; i++)
                {
                    if (!Equals(union1.Types[i], union2.Types[i]))
                        return false;
                }
                return true;

            case GroupSymbol group1 when type2 is GroupSymbol group2:
                if (group1.Symbols.Count != group2.Symbols.Count)
                    return false;
                for (int i = 0; i < group1.Symbols.Count; i++)
                {
                    if (!SymbolEqualityComparer.Instance.Equals(group1.Symbols[i], group2.Symbols[1]))
                        return false;
                }
                return true;

            default:
                // must both be construct with same definition
                if (!type1.IsConstructed 
                    || !type2.IsConstructed
                    || !Equals(type1.Definition, type2.Definition)
                    || type1.TypeArguments.Count != type2.TypeArguments.Count)
                    return false;

                // must have same type arguments.
                for (int i = 0; i < type1.TypeArguments.Count; i++)
                {
                    if (!Equals(type1.TypeArguments[i], type2.TypeArguments[i]))
                        return false;
                }

                return true;
        }
    }

    public int GetHashCode([DisallowNull] TypeSymbol type)
    {
        var hc = 0;

        switch (type)
        {
            case ArraySymbol array:
                hc = GetHashCode(array.ElementType);
                break;
            case UnionSymbol union:
                for (int i = 0; i < union.Types.Count; i++)
                {
                    hc = HashCode.Combine(hc, GetHashCode(union.Types[i]));
                }
                break;
            case GroupSymbol group:
                for (int i = 0; i < group.Symbols.Count; i++)
                {
                    hc = HashCode.Combine(hc, SymbolEqualityComparer.Instance.GetHashCode(group.Symbols[i]));
                }
                break;
            default:
                if (type.IsConstructed && type.Definition != null)
                {
                    hc = GetHashCode(type.Definition);
                    for (int i = 0; i < type.TypeArguments.Count; i++)
                    {
                        hc = HashCode.Combine(GetHashCode(type.TypeArguments[i]));
                    }
                }
                else
                {
                    // rest are singleton types so we use the runtime default hashcode.
                    hc = type.GetHashCode();
                }
                break;
        }

        return hc;
    }
}

public class TypeListEqualityComparer : IEqualityComparer<ImmutableList<TypeSymbol>>
{
    public static TypeListEqualityComparer Instance = new TypeListEqualityComparer();

    private TypeListEqualityComparer() { }

    public bool Equals(ImmutableList<TypeSymbol>? list1, ImmutableList<TypeSymbol>? list2)
    {
        // same instance
        if (list1 == list2) return true;

        if (list1 == null && list2 == null) return true;
        if (list1 == null || list2 == null) return false;

        if (list1.Count != list2.Count)
            return false;

        var typeComparer = TypeEqualityComparer.Instance;

        for (int i = 0; i < list1.Count; i++)
        {
            if (!typeComparer.Equals(list1[i], list2[i]))
                return false;
        }

        return true;
    }

    public int GetHashCode([DisallowNull] ImmutableList<TypeSymbol> list)
    {
        var hc = 0;
        var typeComparer = TypeEqualityComparer.Instance;

        for (int i = 0; i < list.Count; i++)
        {
            hc = HashCode.Combine(hc, typeComparer.GetHashCode(list[i]));
        }

        return hc;
    }
}
