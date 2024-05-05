
namespace Parkour.Symbols;

public sealed class UnionSymbol : TypeSymbol
{
    public ImmutableList<TypeSymbol> Types { get; }

    internal UnionSymbol(ImmutableList<TypeSymbol> types)
        : base($"Union({string.Join(" | ", types.Select(t => t.Name))})")
    {
        Types = types;
    }

    public override int ReferencedSymbolCount => this.Types.Count;
    public override Symbol? GetReferencedSymbol(int index) => index < this.Types.Count ? this.Types[index] : null;
}
