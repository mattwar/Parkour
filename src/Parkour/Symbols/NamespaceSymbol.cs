namespace Parkour.Symbols;
using Utils;

public class NamespaceSymbol : MemberSymbol
{
    public NamespaceSymbol? DeclaringNamespace { get; }
    public override MemberSymbol? Container => DeclaringNamespace;

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
        NamespaceSymbol? declaringNamespace,
        Func<NamespaceSymbol, ImmutableList<Symbol>> fnMembers)
        : base(name)
    {
        DeclaringNamespace = declaringNamespace;
        _fnMembers = fnMembers;
    }

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
                var nameLength = nextSplit > nameStart ? nextSplit - nameStart : dottedName.Length - nameStart;

                if (nameStart + nameLength == dottedName.Length)
                {
                    // put final matches into final output list
                    GetSymbolsInContainers(dottedName, nameStart, nameLength, containers, symbols);
                    return;
                }
                else
                {
                    results.Clear();
                    GetSymbolsInContainers(dottedName, nameStart, nameLength, containers, results);
                    nameStart += nameLength + 1; // skip over ./+ too
                    containers.Clear();
                    containers.AddRange(results);
                }
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
                // find all items with matching name from all containers
                container.GetMembers(dottedName, start, length, result);
            }
        }
    }

    private readonly ObjectPool<List<Symbol>> _symbolListPool =
        new ObjectPool<List<Symbol>>(() => new List<Symbol>(), list => list.Clear());
}
