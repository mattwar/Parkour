namespace Parkour.Symbols;

public static class SymbolExtensions
{
    /// <summary>
    /// Walks the tree of symbol declarations top-down
    /// </summary>
    public static void WalkDeclarations(this Symbol? symbol, Action<Symbol>? action)
    {
        if (symbol == null)
            return;

        action?.Invoke(symbol);

        for (int i = 0, n = symbol.DeclaredSymbolCount; i < n; i++)
        {
            var decl = symbol.GetDeclaredSymbol(i);
            WalkDeclarations(decl, action);
        }
    }

    /// <summary>
    /// Gets all the members of this type and all base types that match the predicate.
    /// </summary>
    public static void GetHierarchyMembers(
        this ContainerSymbol symbol, 
        Func<Symbol, bool> predicate,
        bool firstMatchesOnly,
        List<Symbol> symbols)
    {
        var initialCount = symbols.Count;

        symbol.GetMembers(predicate, symbols);

        if (symbol is TypeSymbol typeSymbol)
        {
            if (firstMatchesOnly && symbols.Count > initialCount)
                return;

            foreach (var bt in typeSymbol.BaseTypes)
            {
                if (!bt.IsInterface || typeSymbol.IsInterface)
                {
                    bt.GetMembers(predicate, symbols);

                    if (firstMatchesOnly && symbols.Count > initialCount)
                        return;
                }
            }
        }
    }

    /// <summary>
    /// Gets all the members of this type and all base types that match the name and predicate.
    /// </summary>
    public static void GetHierarchyMembers(
        this TypeSymbol symbol, 
        string name, int start, int length, 
        Func<Symbol, bool>? predicate,
        bool firstMatchesOnly,
        List<Symbol> symbols)
    {
        var initialCount = symbols.Count;

        symbol.GetMembers(name, start, length, predicate, symbols);

        if (firstMatchesOnly && symbols.Count > initialCount)
            return;

        foreach (var bt in symbol.BaseTypes)
        {
            if (!bt.IsInterface || symbol.IsInterface)
            {
                bt.GetMembers(name, start, length, predicate, symbols);

                if (firstMatchesOnly && symbols.Count > initialCount)
                    return;
            }
        }
    }

    /// <summary>
    /// Gets all the members of this type and all base types that match the name and predicate.
    /// </summary>
    public static void GetHierarchyMembers(
        this TypeSymbol symbol, 
        string name, 
        Func<Symbol, bool>? predicate,
        bool firstMatchesOnly,
        List<Symbol> symbols) 
        =>
        GetHierarchyMembers(symbol, name, 0, name.Length, predicate, firstMatchesOnly, symbols);

    /// <summary>
    /// Gets the first member of this type and all base types that matches the predicate.
    /// </summary>
    public static TSymbol? GetFirstHierarchyMember<TSymbol>(
        this TypeSymbol symbol, 
        string? name, 
        Func<TSymbol, bool>? predicate)
        where TSymbol: Symbol
    {
        var member = symbol.GetFirstMember(name, predicate);
        if (member != null)
            return member;

        foreach (var bt in symbol.BaseTypes)
        {
            if (!bt.IsInterface || symbol.IsInterface)
            {
                member = bt.GetFirstMember(name, predicate);
                if (member != null)
                    return member;
            }
        }

        return null;
    }
}
