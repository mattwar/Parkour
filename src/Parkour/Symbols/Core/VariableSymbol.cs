namespace Parkour.Symbols;

public sealed class VariableSymbol : Symbol
{
    public TypeSymbol Type { get; }

    public VariableSymbol(string name, TypeSymbol type) : base(name)
    {
        Type = type;
    }

    public override int ReferencedSymbolCount => 1;
    public override Symbol? GetReferencedSymbol(int index) => index == 0 ? this.Type : null;
}
