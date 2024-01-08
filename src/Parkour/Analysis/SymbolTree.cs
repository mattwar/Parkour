namespace Parkour.Analysis;
using Symbols;

public class SymbolTree
{
    private Func<SymbolTree, NamespaceSymbol>? _fnRoot;
    private NamespaceSymbol? _root;

    public NamespaceSymbol GlobalNamespace
    {
        get
        {
            if (_root == null && _fnRoot != null)
            {
                var gn = _fnRoot(this);
                Interlocked.CompareExchange(ref _root, gn, null);
                _fnRoot = null;
            }

            return _root!;
        }
    }

    public SymbolTree(Func<SymbolTree, NamespaceSymbol> fnRoot)
    {
        _fnRoot = fnRoot;
    }

    public SymbolTree(NamespaceSymbol globalNamespace)
    {
        _root = globalNamespace;
    }
}
