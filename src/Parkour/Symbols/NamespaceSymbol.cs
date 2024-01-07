namespace Parkour.Symbols;

public class NamespaceSymbol : Symbol
{
    private Func<ImmutableList<Symbol>>? _fnMembers;
    private ImmutableList<Symbol>? _members;

    public override ImmutableList<Symbol> Members
    {
        get
        {
            if (_members == null && _fnMembers != null)
            {
                _members = _fnMembers();
                _fnMembers = null;
            }

            return _members ?? ImmutableList<Symbol>.Empty;
        }
    }

    public NamespaceSymbol(string name, Func<ImmutableList<Symbol>> fnMembers)
        : base(name)
    {
        _fnMembers = fnMembers;
    }

    public NamespaceSymbol(string name, ImmutableList<Symbol> members)
        : base(name)
    {
        _members = members;
    }
}
