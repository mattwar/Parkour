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

    public override int ReferenceCount =>
        this.Symbols.Count;

    public override Symbol? GetReference(int index)
    {
        if (index < this.Symbols.Count)
            return this.Symbols[index];
        return null;
    }
}
