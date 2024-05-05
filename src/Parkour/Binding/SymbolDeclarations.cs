using System.Runtime.CompilerServices;

namespace Parkour.Binding;

using Semantics;
using Symbols;

public static class SymbolDeclarations
{
    /// <summary>
    /// Gets the declarations associated with a symbol.
    /// </summary>
    public static ImmutableList<Declaration> GetBoundDeclarations(this Symbol symbol)
    {
        _symbolToDeclarationMap.TryGetValue(symbol, out var info);
        return info?.Declarations ?? ImmutableList<Declaration>.Empty;
    }

    /// <summary>
    /// Associates a declaration with a symbol.
    /// </summary>
    public static void AddBoundDeclaration(Symbol symbol, Declaration declaration)
    {
        var info = _symbolToDeclarationMap.GetValue(
            symbol, 
            _symbol => new SymbolDeclarationInfo()
            );

        ImmutableInterlocked.Update(
            ref info.Declarations,
            (_list, _decl) => _list.Add(_decl),
            declaration
            );
    }

    private class SymbolDeclarationInfo
    {
        public ImmutableList<Declaration> Declarations
            = ImmutableList<Declaration>.Empty;
    }

    private static readonly ConditionalWeakTable<Symbol, SymbolDeclarationInfo> _symbolToDeclarationMap =
        new ConditionalWeakTable<Symbol, SymbolDeclarationInfo>();

}