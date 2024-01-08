namespace Parkour.Symbols;
using Utils;

public class NamespaceSymbol : Symbol
{
    private Func<ImmutableList<Symbol>>? _fnMembers;
    private ImmutableList<Symbol>? _members;

    public override ImmutableList<Symbol> Members
    {
        get
        {
            if (_members == null && _fnMembers != null)
            {
                _members = _fnMembers();
                _fnMembers = null;
            }

            return _members ?? ImmutableList<Symbol>.Empty;
        }
    }

    public NamespaceSymbol(string name, Func<ImmutableList<Symbol>> fnMembers)
        : base(name)
    {
        _fnMembers = fnMembers;
    }

    public NamespaceSymbol(string name, ImmutableList<Symbol> members)
        : base(name)
    {
        _members = members;
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
}
