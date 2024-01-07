namespace Parkour.Symbols;
using Analysis;

public sealed class TargetSymbol : Symbol
{
    public TypeSymbol Type { get; }

    public TargetSymbol(string name, TypeSymbol? type)
        : base(name)
    {
        Type = type ?? SymbolModel.Void;
    }
}
