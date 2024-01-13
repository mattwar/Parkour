namespace Parkour.Symbols;
using Binding;

public sealed class TargetSymbol : Symbol
{
    public TypeSymbol Type { get; }

    public TargetSymbol(string name, TypeSymbol? type)
        : base(name)
    {
        Type = type ?? SpecialSymbols.Void;
    }
}
