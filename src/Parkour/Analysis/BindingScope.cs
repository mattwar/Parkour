namespace Parkour.Analysis;
using Symbols;

public record struct BindingScope(ImmutableList<NamespaceSymbol> Namespaces, ImmutableList<Symbol> AmbientSymbols)
{
    public ImmutableList<NamespaceSymbol> Namespaces { get; init; } = Namespaces ?? ImmutableList<NamespaceSymbol>.Empty;
    public ImmutableList<Symbol> AmbientSymbols { get; init; } = AmbientSymbols ?? ImmutableList<Symbol>.Empty;

    public static BindingScope Default =>
        new BindingScope(ImmutableList<NamespaceSymbol>.Empty, ImmutableList<Symbol>.Empty);

    public BindingScope AddNamespaces(IEnumerable<NamespaceSymbol> namespaces) =>
        this with { Namespaces = (this.Namespaces ?? ImmutableList<NamespaceSymbol>.Empty).AppendRange(namespaces) };

    public BindingScope AddNamespaces(params NamespaceSymbol[] symbols) => 
        AddNamespaces((IEnumerable<NamespaceSymbol>)symbols);

    public BindingScope AddNamespace(NamespaceSymbol symbol) => AddNamespaces(symbol);

    public BindingScope AddAmbientSymbols(IEnumerable<Symbol> symbols) =>
        this with { AmbientSymbols = (this.AmbientSymbols ?? ImmutableList<Symbol>.Empty).AppendRange(symbols) };

    public BindingScope AddAmbientSymbols(params Symbol[] symbols) =>
        AddAmbientSymbols((IEnumerable<Symbol>)symbols);

    public BindingScope AddAmbientSymbol(Symbol symbol) =>
        this with { AmbientSymbols = this.AmbientSymbols.Append(symbol) };

    /// <summary>
    /// returns all the matching symbols
    /// </summary>
    public void FindSymbols<TSymbol>(Func<TSymbol, bool> fnMatch, List<TSymbol> list) where TSymbol : Symbol
    {
        // look in namespaces
        foreach (var ns in this.Namespaces)
        {
            foreach (var nsMember in ns.Members)
            {
                if (nsMember is TSymbol tsymbol && fnMatch(tsymbol))
                    list.Add(tsymbol);
            }
        }

        // look in ambient symbols
        for(int i = this.AmbientSymbols.Count - 1; i >= 0; i--)
        {
            var symbol = this.AmbientSymbols[i];
            if (symbol is TSymbol tsymbol && fnMatch(tsymbol))
                list.Add(tsymbol);
        }
    }

    /// <summary>
    /// returns the most recent matching symbols
    /// </summary>
    public TSymbol? FindSymbol<TSymbol>(Func<TSymbol, bool> fnMatch) 
        where TSymbol : Symbol
    {
        foreach (var ns in this.Namespaces)
        {
            foreach (var nsMember in ns.Members)
            {
                if (nsMember is TSymbol tsymbol && fnMatch(tsymbol))
                    return tsymbol;
            }
        }

        for (int i = this.AmbientSymbols.Count - 1; i >= 0; i--)
        {
            var symbol = this.AmbientSymbols[i];
            if (symbol is TSymbol tsymbol && fnMatch(tsymbol))
                return tsymbol;
        }

        return null;
    }
}
