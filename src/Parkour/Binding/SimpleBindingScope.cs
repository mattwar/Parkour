namespace Parkour.Binding;
using Symbols;

public class SimpleBindingScope : BindingScope
{
    private readonly ImmutableList<Symbol> _symbols;
    private readonly ImmutableList<ContainerSymbol> _containers;
    public override BindingScope? OuterScope { get; }

    public SimpleBindingScope(
        ImmutableList<Symbol>? symbols, 
        ImmutableList<ContainerSymbol>? containers,
        BindingScope? outerScope)
    {
        _symbols = symbols ?? ImmutableList<Symbol>.Empty;
        _containers = containers ?? ImmutableList<ContainerSymbol>.Empty;
        OuterScope = outerScope;
    }

    public static SimpleBindingScope Empty =
        new SimpleBindingScope(null, null, null);

    public override BindingScope AddMembers(ContainerSymbol container) =>
        new SimpleBindingScope(_symbols, _containers.Add(container), this.OuterScope);

    public override BindingScope AddMembers(IEnumerable<ContainerSymbol> containers) =>
        new SimpleBindingScope(_symbols, _containers.AddRange(containers), this.OuterScope);

    public override BindingScope AddSymbols(IEnumerable<Symbol> symbols) =>
        new SimpleBindingScope(_symbols.AddRange(symbols), _containers, this.OuterScope);

    public override BindingScope AddSymbol(Symbol symbol) =>
        new SimpleBindingScope(_symbols.Add(symbol), _containers, this.OuterScope);

    public override BindingScope NewScope() =>
        new SimpleBindingScope(null, null, this);

    public override void FindMatchingSymbols(
        string? name, 
        Func<Symbol, bool>? predicate, 
        List<Symbol> list)
    {
        // look at container members
        foreach (var container in _containers)
        {
            if (name != null)
            {
                container.GetHierarchyMembers(name, predicate, firstMatchesOnly: true, list);
            }
            else if (predicate != null)
            {
                container.GetHierarchyMembers(predicate, firstMatchesOnly: true, list);
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

    public override TSymbol FindFirstMatchingSymbol<TSymbol>(
        string? name, 
        Func<TSymbol, bool>? predicate)
    {
        foreach (var container in _containers)
        {
            var symbol = container.GetFirstHierarchyMember(name, predicate);
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
