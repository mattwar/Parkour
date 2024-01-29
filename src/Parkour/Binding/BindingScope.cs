namespace Parkour.Binding;
using Symbols;

public abstract class BindingScope
{
    /// <summary>
    /// A default binding scope.
    /// </summary>
    public static BindingScope Default =
        new DefaultBindingScope(null, null, null);

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

public class DefaultBindingScope : BindingScope
{
    private readonly ImmutableList<Symbol> _symbols;
    private readonly ImmutableList<ContainerSymbol> _containers;
    public override BindingScope? OuterScope { get; }

    public DefaultBindingScope(
        ImmutableList<Symbol>? symbols, 
        ImmutableList<ContainerSymbol>? containers,
        BindingScope? outerScope)
    {
        _symbols = symbols ?? ImmutableList<Symbol>.Empty;
        _containers = containers ?? ImmutableList<ContainerSymbol>.Empty;
        OuterScope = outerScope;
    }

    public override BindingScope AddMembers(ContainerSymbol container) =>
        new DefaultBindingScope(_symbols, _containers.Add(container), this.OuterScope);

    public override BindingScope AddMembers(IEnumerable<ContainerSymbol> containers) =>
        new DefaultBindingScope(_symbols, _containers.AddRange(containers), this.OuterScope);

    public override BindingScope AddSymbols(IEnumerable<Symbol> symbols) =>
        new DefaultBindingScope(_symbols.AddRange(symbols), _containers, this.OuterScope);

    public override BindingScope AddSymbol(Symbol symbol) =>
        new DefaultBindingScope(_symbols.Add(symbol), _containers, this.OuterScope);

    public override BindingScope NewScope() =>
        new DefaultBindingScope(null, null, this);

    public override void FindMatchingSymbols(string? name, Func<Symbol, bool>? predicate, List<Symbol> list)
    {
        // look at container members
        foreach (var container in _containers)
        {
            if (name != null)
            {
                container.GetMembers(name, predicate, list);
            }
            else if (predicate != null)
            {
                container.GetMembers(predicate, list);
            }
            else
            {
                list.AddRange(container.Members);
            }
        }

        // look at symbols
        for (int i = _symbols.Count - 1; i >= 0; i--)
        {
            var symbol = _symbols[i];
            if ((name == null || symbol.Name == name)
                && (predicate == null || predicate(symbol)))
            {
                list.Add(symbol);
            }
        }
    }

    public override TSymbol FindMatchingSymbol<TSymbol>(string? name, Func<TSymbol, bool>? predicate) 
    {
        foreach (var container in _containers)
        {
            var symbol = container.GetFirstMember(name, predicate);
            if (symbol != null)
                return symbol;
        }

        for (int i = _symbols.Count - 1; i >= 0; i--)
        {
            var symbol = _symbols[i];
            if (symbol is TSymbol tsymbol 
                && (name == null || symbol.Name == name)
                && (predicate == null || predicate(tsymbol)))
            {
                return tsymbol;
            }
        }

        return null!;
    }
}
