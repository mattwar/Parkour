using Parkour.Utils;

namespace Parkour.Symbols;

/// <summary>
/// Containers are symbols with members.
/// </summary>
public abstract class ContainerSymbol : MemberSymbol
{
    protected ContainerSymbol(
        string name, 
        Symbol? declaringSymbol,
        SymbolAccess access, 
        BitSet<SymbolModifier> modifiers)
        : base(name, declaringSymbol, access, modifiers)
    {
    }

    public virtual ImmutableList<Symbol> Members => 
        ImmutableList<Symbol>.Empty;

    /// <summary>
    /// Gets all the members that matches the predicate.
    /// </summary>
    public virtual void GetMembers(Func<Symbol, bool> predicate, List<Symbol> symbols)
    {
        if (Members.Count > 0)
        {
            foreach (var member in Members)
            {
                if (predicate(member))
                    symbols.Add(member);
            }
        }
    }

    private Dictionary<TextKey, ImmutableList<Symbol>>? _keyMap;

    /// <summary>
    /// Gets all the symbols with a name that matches the text range at an addition predicate.
    /// </summary>
    public virtual void GetMembers(string name, int start, int length, Func<Symbol, bool>? predicate, List<Symbol> symbols)
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
            if (predicate != null)
                symbols.AddRange(syms.Where(predicate));
            else
                symbols.AddRange(syms);
        }
    }

    /// <summary>
    /// Gets all the symbols with name that matches the text range.
    /// </summary>
    public virtual void GetMembers(string name, int start, int length, List<Symbol> symbols) =>
        GetMembers(name, start, length, null, symbols);

    /// <summary>
    /// Gets all the symbols that match the name and the predicate.
    /// </summary>
    public virtual void GetMembers(string name, Func<Symbol, bool>? predicate, List<Symbol> symbols) =>
        GetMembers(name, 0, name.Length, predicate, symbols);

    /// <summary>
    /// Gets all the symbols that match the name.
    /// </summary>
    public virtual void GetMembers(string name, List<Symbol> symbols) =>
        GetMembers(name, 0, name.Length, null, symbols);

    /// <summary>
    /// Gets the first symbol that matches the predicate.
    /// </summary>
    public virtual TSymbol? GetFirstMember<TSymbol>(string? name, Func<TSymbol, bool>? predicate)
        where TSymbol : Symbol
    {
        if (Members.Count > 0)
        {
            foreach (var member in Members)
            {
                if (member is TSymbol tmember 
                    && (name == null || member.Name == name)
                    && (predicate == null || predicate(tmember)))
                {
                    return tmember;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all the first symbol that matches the predicate.
    /// </summary>
    public Symbol? GetFirstMember(Func<Symbol, bool> predicate) =>
        GetFirstMember<Symbol>(null, predicate);

    /// <summary>
    /// Gets the first symbol with the specified name.
    /// </summary>
    public TSymbol? GetFirstMember<TSymbol>(string name)
        where TSymbol : Symbol =>
        GetFirstMember<TSymbol>(name, null);

    /// <summary>
    /// Gets the first symbol with the specified name.
    /// </summary>
    public Symbol? GetFirstMember(string name) =>
        GetFirstMember<Symbol>(name);
}
