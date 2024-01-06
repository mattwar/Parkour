namespace Parkour.Semantics;

public interface IBindingScope<TScope>
{
    abstract static TScope Default { get; }

    TScope AddAmbientSymbols(IEnumerable<Symbol> symbols);
    TScope AddAmbientSymbols(params Symbol[] symbols) => AddAmbientSymbols((IEnumerable<Symbol>)symbols);
    TScope AddAmbientSymbol(Symbol symbol) => AddAmbientSymbols(symbol);
    TScope WithPathSymbol(Symbol symbol);

    void FindSymbols<TSymbol>(Func<TSymbol, bool> fnMatch, List<TSymbol> list) 
        where TSymbol : Symbol;

    void FindSymbols(Func<Symbol, bool> fnMatch, List<Symbol> list) =>
        FindSymbols<Symbol>(fnMatch, list);

    TSymbol? FindSymbol<TSymbol>(Func<TSymbol, bool> fnMatch) 
        where TSymbol : Symbol;

    Symbol? FindSymbol(Func<Symbol, bool> fnMatch) =>
        FindSymbol<Symbol>(fnMatch);

    Symbol? FindSymbol(string name) =>
        FindSymbol(s => s.Name == name);
}

public record struct SimpleBindingScope(ImmutableList<Symbol> AmbientSymbols, Symbol? PathSymbol)
    : IBindingScope<SimpleBindingScope>
{
    public static SimpleBindingScope Default =>
        new SimpleBindingScope(ImmutableList<Symbol>.Empty, null);

    public SimpleBindingScope AddAmbientSymbol(Symbol symbol) =>
        this with { AmbientSymbols = this.AmbientSymbols.Append(symbol), PathSymbol = null };

    public SimpleBindingScope AddAmbientSymbols(IEnumerable<Symbol> symbols) =>
        this with { AmbientSymbols = this.AmbientSymbols.AppendRange(symbols), PathSymbol = null };

    public SimpleBindingScope WithPathSymbol(Symbol symbol) =>
        this with { PathSymbol = symbol, AmbientSymbols = ImmutableList<Symbol>.Empty };

    /// <summary>
    /// returns all the matching symbols
    /// </summary>
    public void FindSymbols<TSymbol>(Func<TSymbol, bool> fnMatch, List<TSymbol> list) where TSymbol : Symbol
    {
        if (this.PathSymbol != null)
        {
            foreach (var member in this.PathSymbol.Members)
            {
                if (member is TSymbol tmember && fnMatch(tmember))
                    list.Add(tmember);
            }
        }
        else
        {
            for(int i = this.AmbientSymbols.Count - 1; i >= 0; i--)
            {
                var symbol = this.AmbientSymbols[i];
                if (symbol is TSymbol tsymbol && fnMatch(tsymbol))
                    list.Add(tsymbol);
            }
        }
    }

    /// <summary>
    /// returns the most recent matching symbols
    /// </summary>
    public TSymbol? FindSymbol<TSymbol>(Func<TSymbol, bool> fnMatch) 
        where TSymbol : Symbol
    {
        if (this.PathSymbol != null)
        {
            foreach (var member in this.PathSymbol.Members)
            {
                if (member is TSymbol tmember && fnMatch(tmember))
                    return tmember;
            }
        }
        else
        {
            for (int i = this.AmbientSymbols.Count - 1; i >= 0; i--)
            {
                var symbol = this.AmbientSymbols[i];
                if (symbol is TSymbol tsymbol && fnMatch(tsymbol))
                    return tsymbol;
            }
        }

        return null;
    }
}
