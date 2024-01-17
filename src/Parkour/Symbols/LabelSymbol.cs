namespace Parkour.Symbols;

public sealed class LabelSymbol : Symbol
{
    public TypeSymbol Type { get; }

    public LabelSymbol(string name, TypeSymbol? type)
        : base(name)
    {
        Type = type ?? SpecialSymbols.Void;
    }

    public static string BreakLabelName = "break";
    public static string ContinueLabelName = "continue";
    public static string ReturnLabelName = "return";
}
