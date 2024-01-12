namespace Parkour.Symbols;

public static class SymbolExtensions
{
    /// <summary>
    /// Walks the tree of symbol declarations top-down
    /// </summary>
    public static void Walk(this Symbol? symbol, Action<Symbol> action)
    {
        if (symbol == null)
            return;

        action(symbol);

        switch (symbol)
        {
            case PropertySymbol p:               
                Walk(p.BackingField, action);
                Walk(p.GetMethod, action);
                Walk(p.SetMethod, action);
                break;

            case MethodSymbol m:
                WalkList(m.Parameters, action);
                break;

            case ConstructorSymbol c:
                WalkList(c.Parameters, action);
                break;

            case NamespaceSymbol n:
                WalkList(n.Members, action);
                break;

            case TypeSymbol t:
                WalkList(t.TypeParameters, action);
                WalkList(t.Members, action);
                break;

            default:
                break;
        }

        static void WalkList<TSymbol>(ImmutableList<TSymbol> symbols, Action<Symbol> action)
            where TSymbol : Symbol
        {
            foreach (var symbol in symbols)
            {
                Walk(symbol, action);
            }
        }
    }
}