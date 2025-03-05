namespace Parkour.Symbols;

public class AliasSymbol : ContainerSymbol
{
    public ContainerSymbol AliasedSymbol { get; }

    public override ImmutableList<Symbol> Members => 
        AliasedSymbol.Members;

    public AliasSymbol(
        string name,
        ContainerSymbol aliasedSymbol)
        : base(name, null, SymbolAccess.Public, SymbolModifier.None, definition: null)
    {
        AliasedSymbol = aliasedSymbol;
    }

    public override int DeclaredSymbolCount => 0;
    public override Symbol? GetDeclaredSymbol(int index) => null;
}
