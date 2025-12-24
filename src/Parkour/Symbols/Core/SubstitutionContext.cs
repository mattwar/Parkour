namespace Parkour.Symbols;

public abstract class SubstitutionContext
{
    /// <summary>
    /// Substitute this symbol or make substitutions in symbols referenced by this symbol.
    /// </summary>
    public abstract TSymbol Substitute<TSymbol>(TSymbol symbol, Symbol? declaringSymbol = null)
        where TSymbol : Symbol;

    /// <summary>
    /// Substitute these symbols or make substitutions in symbols referenced by them.
    /// </summary>
    public abstract ImmutableList<TSymbol> Substitute<TSymbol>(ImmutableList<TSymbol> symbols, Symbol? declaringSymbol = null)
        where TSymbol : Symbol;
}