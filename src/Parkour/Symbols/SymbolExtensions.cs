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
    /// Gets the symbol corresponding the symbol's full dotted name. (ie System.Int32)
    /// Returns the first symbol if more than one symbol with the same name and type exists.
    /// Returns null if no symbols with the name and type exists.
    /// </summary>
    public static TSymbol? GetFirstSymbolFromPath<TSymbol>(this NamespaceSymbol @namespace, string dottedPath)
        where TSymbol : Symbol
    {
        var symbols = _symbolListPool.AllocateFromPool();
        try
        {
            GetSymbolsFromPath(@namespace, dottedPath, symbols);
            return symbols.OfType<TSymbol>().FirstOrDefault();
        }
        finally
        {
            _symbolListPool.ReturnToPool(symbols);
        }
    }

    /// <summary>
    /// Gets the symbol corresponding the symbol's full dotted name. (ie System.Int32)
    /// Returns the first symbol if more than one symbol with the same name exists.
    /// Returns null if no symbols with the name exists.
    /// </summary>
    public static Symbol? GetFirstSymbolFromPath(this NamespaceSymbol @namespace, string dottedPath) =>
        GetFirstSymbolFromPath<Symbol>(@namespace, dottedPath);

    private static readonly char[] _namePathSplitChars = new[] { '.', '+' };

    public static bool IsDottedPath(string path) =>
        path.IndexOfAny(_namePathSplitChars) > 0;

    /// <summary>
    /// Gets all the symbols that can be reached with the specified dotted name.
    /// Typically this returns 1 or 0, but may return more if there are multiple symbols with the same name.
    /// </summary>
    public static void GetSymbolsFromPath(this NamespaceSymbol @namespace, string dottedName, List<Symbol> symbols)
    {
        var containers = _symbolListPool.AllocateFromPool();
        var results = _symbolListPool.AllocateFromPool();
        try
        {
            // containers start as just the global namespace, but may include more if multiple same named symbols exist
            containers.Add(@namespace);
            var nameStart = 0;

            while (nameStart < dottedName.Length)
            {
                var nextSplit = dottedName.IndexOfAny(_namePathSplitChars, nameStart);
                var nameEnd = nextSplit > nameStart ? nextSplit : dottedName.Length;
                var nameLength = nameEnd - nameStart;

                var arity = 0;
                var arityStart = dottedName.IndexOf('`', nameStart);
                if (arityStart > nameStart && arityStart < nameEnd)
                {
                    var aritySpan = dottedName.AsSpan().Slice(arityStart + 1, nameEnd - arityStart - 1);
                    int.TryParse(aritySpan, out arity);
                    nameLength = arityStart - nameStart;
                }

                results.Clear();
                GetSymbolsInContainers(dottedName, nameStart, nameLength, containers, results);
                RemoveArityMismatch(results, arity);
                nameStart = nameEnd + 1;
                containers.Clear();
                containers.AddRange(results);
            }

            // we might get here if dotted path ends in a dot (so just return last set of results)
            symbols.AddRange(results);
        }
        finally
        {
            _symbolListPool.ReturnToPool(containers);
            _symbolListPool.ReturnToPool(results);
        }

        static void GetSymbolsInContainers(string dottedName, int start, int length, List<Symbol> containers, List<Symbol> result)
        {
            foreach (var container in containers)
            {
                if (container is ContainerSymbol nsOrType)

                    // find all items with matching name from all containers
                    nsOrType.GetMembers(dottedName, start, length, result);
            }
        }

        static void RemoveArityMismatch(List<Symbol> symbols, int arity)
        {
            for (int i = symbols.Count - 1; i >= 0; i--)
            {
                if (symbols[i].Arity != arity)
                    symbols.RemoveAt(i);
            }
        }
    }

    private static readonly ObjectPool<List<Symbol>> _symbolListPool =
        new ObjectPool<List<Symbol>>(() => new List<Symbol>(), list => list.Clear());

}