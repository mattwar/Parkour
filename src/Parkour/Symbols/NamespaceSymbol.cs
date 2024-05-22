namespace Parkour.Symbols;

public class NamespaceSymbol : ContainerSymbol
{
    /// <summary>
    /// The members of the namespace.
    /// </summary>
    public override ImmutableList<Symbol> Members => _lazyMembers.Value;
    private readonly Lazy<ImmutableList<Symbol>> _lazyMembers;

    public NamespaceSymbol(
        string name, 
        Symbol? declaringSymbol,
        Func<NamespaceSymbol, ImmutableList<Symbol>> fnMembers)
        : base(name, declaringSymbol, SymbolAccess.Public, SymbolModifier.None)
    {
        _lazyMembers = new Lazy<ImmutableList<Symbol>>(() => fnMembers(this));
    }

    public override int DeclaredSymbolCount => this.Members.Count;
    public override Symbol? GetDeclaredSymbol(int index) => this.Members[index];
}