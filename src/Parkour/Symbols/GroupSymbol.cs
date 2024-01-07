using Parkour;

namespace Parkour.Symbols;

public sealed class GroupSymbol : TypeSymbol
{
    public ImmutableList<Symbol> Symbols { get; }

    internal GroupSymbol(ImmutableList<Symbol> symbols)
        : base($"Group({string.Join(", ", symbols.Select(t => t.Name))})")
    {
        Symbols = symbols;
    }
}
