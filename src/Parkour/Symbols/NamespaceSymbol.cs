namespace Parkour.Symbols;

public class NamespaceSymbol : ContainerSymbol
{
    private Func<NamespaceSymbol, ImmutableList<Symbol>>? _fnMembers;
    private ImmutableList<Symbol>? _members;

    public override ImmutableList<Symbol> Members
    {
        get
        {
            if (_members == null && _fnMembers is { } fn)
            {
                _fnMembers = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _members, tmp, null);
            }

            return _members ?? ImmutableList<Symbol>.Empty;
        }
    }

    public NamespaceSymbol(
        string name, 
        Symbol? declaringSymbol,
        Func<NamespaceSymbol, ImmutableList<Symbol>> fnMembers)
        : base(name, declaringSymbol, SymbolAccess.Public, SymbolModifier.None)
    {
        _fnMembers = fnMembers;
    }

    public override int DeclarationCount => this.Members.Count;
    public override Symbol? GetDeclaration(int index) => this.Members[index];
}

public class GlobalNamespaceSymbol : NamespaceSymbol
{
    public GlobalNamespaceSymbol(
        Func<GlobalNamespaceSymbol, ImmutableList<Symbol>> fnMembers)
        : base("", null, ns => fnMembers((GlobalNamespaceSymbol)ns))
    {
    }
}
