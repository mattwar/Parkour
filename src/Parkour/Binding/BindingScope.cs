namespace Parkour.Binding;
using Symbols;

public abstract class BindingScope
{
    /// <summary>
    /// A default binding scope.
    /// </summary>
    public static BindingScope Default =
        new SimpleBindingScope(null, null, null);

    /// <summary>
    /// An additional outer scope
    /// </summary>
    public abstract BindingScope? OuterScope { get; }

    /// <summary>
    /// Creates a new instance of the current scope with the symbols added.
    /// </summary>
    public abstract BindingScope AddSymbols(IEnumerable<Symbol> symbols);

    /// <summary>
    /// Creates a new instance of the current scope with the symbol added.
    /// </summary>
    public abstract BindingScope AddSymbol(Symbol symbol);

    /// <summary>
    /// Creates a new instance of the current scope with the members of the container symbol added.
    /// </summary>
    public abstract BindingScope AddMembers(ContainerSymbol container);

    /// <summary>
    /// Creates a new instance of the current scope with the members of the container symbols added.
    /// </summary>
    public abstract BindingScope AddMembers(IEnumerable<ContainerSymbol> containers);

    /// <summary>
    /// Creates a new scope with this scope as its outer scope.
    /// </summary>
    public abstract BindingScope NewScope();

    /// <summary>
    /// Finds all the matching symbols in the current scope
    /// </summary>
    public abstract void FindMatchingSymbols(string? name, Func<Symbol, bool>? predicate, List<Symbol> list);

    /// <summary>
    /// Finds the most recently added matching symbol in the current scope
    /// </summary>
    public abstract TSymbol? FindMatchingSymbol<TSymbol>(string? name, Func<TSymbol, bool>? predicate)
        where TSymbol : Symbol;

    /// <summary>
    /// Finds all the matching symbols in the specified scopes.
    /// </summary>
    public void FindMatchingSymbols(string? name, Func<Symbol, bool>? predicate, List<Symbol> symbols, FindScope findScope)
    {
        var originalCount = symbols.Count;

        var scope = this;
        while (scope != null)
        {
            FindMatchingSymbols(name, predicate, symbols);

            switch (findScope)
            {
                case FindScope.Current:
                default:
                    return;
                case FindScope.First:
                    if (symbols.Count > originalCount)
                        return;
                    break;
                case FindScope.All:
                    break;
            }

            scope = scope.OuterScope;
        }
    }
}

public enum FindScope
{
    /// <summary>
    /// Finds symbols in the current scope only
    /// </summary>
    Current,

    /// <summary>
    /// Finds symbols in the first scope with any matches
    /// </summary>
    First,

    /// <summary>
    /// Finds symbols in all scopes with matches
    /// </summary>
    All
}
