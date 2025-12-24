namespace Parkour.Symbols;

public class GlobalNamespaceSymbol : NamespaceSymbol
{
    public GlobalNamespaceSymbol(
        Func<GlobalNamespaceSymbol, ImmutableList<Symbol>> fnMembers)
        : base("", null, ns => fnMembers((GlobalNamespaceSymbol)ns))
    {
    }
}
