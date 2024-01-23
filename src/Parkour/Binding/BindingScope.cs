namespace Parkour.Binding;
using Symbols;

public record struct BindingScope(ImmutableList<Symbol> Containers, ImmutableList<Symbol> Symbols)
{
    public ImmutableList<Symbol> Containers { get; init; } = Containers ?? ImmutableList<Symbol>.Empty;
    public ImmutableList<Symbol> Symbols { get; init; } = Symbols ?? ImmutableList<Symbol>.Empty;

    public static BindingScope Default =>
        new BindingScope(ImmutableList<Symbol>.Empty, ImmutableList<Symbol>.Empty);

    /// <summary>
    /// Add symbol's members
    /// </summary>
    public BindingScope AddSymbolMembers(IEnumerable<Symbol> containers) =>
        this with { Containers = (this.Containers ?? ImmutableList<Symbol>.Empty).AppendRange(containers) };

    /// <summary>
    /// Add symbol members
    /// </summary>
    public BindingScope AddSymbolMembers(Symbol symbol) =>
        AddSymbolMembers(new[] { symbol });

    /// <summary>
    /// Add symbols
    /// </summary>
    public BindingScope AddSymbols(IEnumerable<Symbol> symbols) =>
        this with { Symbols = (this.Symbols ?? ImmutableList<Symbol>.Empty).AppendRange(symbols) };

    /// <summary>
    /// Add symbol
    /// </summary>
    public BindingScope AddSymbol(Symbol symbol) =>
        AddSymbols(new[] { symbol });

    /// <summary>
    /// returns all the matching symbols
    /// </summary>
    public void FindSymbols<TSymbol>(Func<TSymbol, bool> fnMatch, List<TSymbol> list) where TSymbol : Symbol
    {
        // look at container members
        foreach (var container in this.Containers)
        {
            if (container is NamespaceOrTypeSymbol nsOrType)
            {
                foreach (var members in nsOrType.Members)
                {
                    if (members is TSymbol tsymbol && fnMatch(tsymbol))
                        list.Add(tsymbol);
                }
            }
        }

        // look at symbols
        for(int i = this.Symbols.Count - 1; i >= 0; i--)
        {
            var symbol = this.Symbols[i];
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
        foreach (var container in this.Containers)
        {
            if (container is NamespaceOrTypeSymbol nsOrType)
            {
                foreach (var member in nsOrType.Members)
                {
                    if (member is TSymbol tsymbol && fnMatch(tsymbol))
                        return tsymbol;
                }
            }
        }

        for (int i = this.Symbols.Count - 1; i >= 0; i--)
        {
            var symbol = this.Symbols[i];
            if (symbol is TSymbol tsymbol && fnMatch(tsymbol))
                return tsymbol;
        }

        return null;
    }
}
