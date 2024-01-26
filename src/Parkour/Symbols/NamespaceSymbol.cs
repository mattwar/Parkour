namespace Parkour.Symbols;
using Utils;

public class NamespaceSymbol : NamespaceOrTypeSymbol
{
    private Func<NamespaceSymbol, ImmutableList<Symbol>>? _fnMembers;
    private ImmutableList<Symbol>? _members;

    public override ImmutableList<Symbol> Members
    {
        get
        {
            if (_members == null && _fnMembers is { } fn)
            {
                _fnMembers = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _members, tmp, null);
            }

            return _members ?? ImmutableList<Symbol>.Empty;
        }
    }

    public NamespaceSymbol(
        string name, 
        Symbol? declaringSymbol,
        Func<NamespaceSymbol, ImmutableList<Symbol>> fnMembers)
        : base(name, declaringSymbol, SymbolAccess.Public, SymbolModifier.None)
    {
        _fnMembers = fnMembers;
    }

    public override int DeclarationCount => this.Members.Count;
    public override Symbol? GetDeclaration(int index) => this.Members[index];


    private Dictionary<TextKey, ImmutableList<Symbol>>? _keyMap;

    public override void GetMembers(string name, int start, int length, Func<Symbol, bool>? fnMatch, List<Symbol> symbols)
    {
        if (_keyMap == null)
        {
            var map = new Dictionary<TextKey, ImmutableList<Symbol>>(
                this.Members.GroupBy(m => m.Name).Select(g => KeyValuePair.Create((TextKey)g.Key, g.ToImmutableList()))
                );
            Interlocked.CompareExchange(ref _keyMap, map, null);
        }

        if (_keyMap.TryGetValue(new TextKey(name, start, length), out var syms))
        {
            if (fnMatch != null)
                symbols.AddRange(syms.Where(fnMatch));
            else
                symbols.AddRange(syms);
        }
    }

    /// <summary>
    /// Gets the symbol corresponding the symbol's full dotted name. (ie System.Int32)
    /// Returns the first symbol if more than one symbol with the same name and type exists.
    /// Returns null if no symbols with the name and type exists.
    /// </summary>
    public virtual TSymbol? GetFirstSymbolFromPath<TSymbol>(string dottedPath)
        where TSymbol : Symbol
    {
        var symbols = _symbolListPool.AllocateFromPool();
        try
        {
            GetSymbolsFromPath(dottedPath, symbols);
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
    public Symbol? GetFirstSymbolFromPath(string dottedPath) =>
        GetFirstSymbolFromPath<Symbol>(dottedPath);

    private static readonly char[] _namePathSplitChars = new[] { '.', '+' };

    public static bool IsDottedPath(string path) =>
        path.IndexOfAny(_namePathSplitChars) > 0;

    /// <summary>
    /// Gets all the symbols that can be reached with the specified dotted name.
    /// Typically this returns 1 or 0, but may return more if there are multiple symbols with the same name.
    /// </summary>
    public virtual void GetSymbolsFromPath(string dottedName, List<Symbol> symbols)
    {
        var containers = _symbolListPool.AllocateFromPool();
        var results = _symbolListPool.AllocateFromPool();
        try
        {
            // containers start as just the global namespace, but may include more if multiple same named symbols exist
            containers.Add(this);
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
                if (container is NamespaceOrTypeSymbol nsOrType)

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

    private readonly ObjectPool<List<Symbol>> _symbolListPool =
        new ObjectPool<List<Symbol>>(() => new List<Symbol>(), list => list.Clear());
}
