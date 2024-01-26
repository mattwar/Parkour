namespace Parkour.Symbols;

public static class SymbolExtensions
{
    /// <summary>
    /// Walks the tree of symbol declarations top-down
    /// </summary>
    public static void WalkDeclarations(this Symbol? symbol, Action<Symbol>? action)
    {
        if (symbol == null)
            return;

        action?.Invoke(symbol);

        for (int i = 0, n = symbol.DeclarationCount; i < n; i++)
        {
            var decl = symbol.GetDeclaration(i);
            WalkDeclarations(decl, action);
        }
    }
}