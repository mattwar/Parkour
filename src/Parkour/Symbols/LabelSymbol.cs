namespace Parkour.Symbols;

public sealed class LabelSymbol : Symbol
{
    public TypeSymbol Type { get; }

    public LabelSymbol(string name, TypeSymbol? type)
        : base(name)
    {
        Type = type ?? SpecialSymbols.Void;
    }

    public LabelSymbol(string name)
        : this(name, SpecialSymbols.Void)
    {
    }

    public override int ReferencedSymbolCount => 1;
    public override Symbol? GetReferencedSymbol(int index) => index == 0 ? this.Type : null;

    public static string BreakLabelName = "break";
    public static string ContinueLabelName = "continue";
    public static string ReturnLabelName = "return";
}
