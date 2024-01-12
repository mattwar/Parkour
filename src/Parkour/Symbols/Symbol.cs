namespace Parkour.Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class Symbol
{
    private string DebugText => $"{GetType().Name}: {Name}";

    public string Name { get; }
    public virtual ImmutableList<Symbol> Members => ImmutableList<Symbol>.Empty;

    protected Symbol(string name)
    {
        Name = name;
    }

    public virtual void GetMembers(Func<Symbol, bool> fnMatch, List<Symbol> symbols)
    {
        if (Members.Count > 0)
        {
            foreach (var member in Members)
            {
                if (fnMatch(member))
                    symbols.Add(member);
            }
        }
    }

    public virtual void GetMembers(string name, int start, int length, Func<Symbol, bool>? fnMatch, List<Symbol> symbols) =>
        GetMembers(m => string.Compare(m.Name, 0, name, start, length) == 0 && (fnMatch == null || fnMatch(m)), symbols);

    public virtual void GetMembers(string name, int start, int length, List<Symbol> symbols) =>
        GetMembers(name, start, length, null, symbols);

    public virtual void GetMembers(string name, Func<Symbol, bool>? fnMatch, List<Symbol> symbols) =>
        GetMembers(name, 0, name.Length, fnMatch, symbols);

    public virtual void GetMembers(string name, List<Symbol> symbols) =>
        GetMembers(name, 0, name.Length, null, symbols);


    public virtual TSymbol? GetFirstMember<TSymbol>(Func<TSymbol, bool> fnMatch)
        where TSymbol : Symbol
    {
        if (Members.Count > 0)
        {
            foreach (var member in Members)
            {
                if (member is TSymbol tmember && fnMatch(tmember))
                    return tmember;
            }
        }

        return null;
    }

    public Symbol? GetFirstMember(Func<Symbol, bool> fnMatch) =>
        GetFirstMember<Symbol>(fnMatch);

    public TSymbol? GetFirstMember<TSymbol>(string name)
        where TSymbol : Symbol =>
        GetFirstMember<TSymbol>(m => m.Name == name);

    public Symbol? GetFirstMember(string name) =>
        GetFirstMember<Symbol>(m => m.Name == name);
}