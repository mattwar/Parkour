namespace Parkour.Symbols;

public class MemberSymbol : Symbol
{
    public Symbol? Container { get; }
    public SymbolAccess Access { get; }
    public SymbolModifier Modifiers { get; }

    public bool IsStatic => (Modifiers & SymbolModifier.Static) != 0;

    public MemberSymbol(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier)
        : base(name)
    {
        Container = container;
        Access = access;
        Modifiers = modifier;
    }
}
