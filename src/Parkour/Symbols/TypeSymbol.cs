namespace Parkour.Symbols;

public class TypeSymbol : MemberSymbol
{
    private Func<ImmutableList<TypeSymbol>>? _fnBaseTypes;
    private ImmutableList<TypeSymbol>? _baseTypes;

    public ImmutableList<TypeSymbol> BaseTypes
    {
        get
        {
            if (_baseTypes == null && _fnBaseTypes is Func<ImmutableList<TypeSymbol>> fn)
            {
                _fnBaseTypes = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _baseTypes, tmp, null);
            }

            return _baseTypes ?? ImmutableList<TypeSymbol>.Empty;
        }
    }

    private Func<Symbol, ImmutableList<Symbol>>? _fnMembers;
    private ImmutableList<Symbol>? _members;

    public override ImmutableList<Symbol> Members
    {
        get
        {
            if (_members == null && _fnMembers is Func<Symbol, ImmutableList<Symbol>> fn)
            {
                _fnMembers = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _members, tmp, null);
            }

            return _members ?? ImmutableList<Symbol>.Empty;
        }
    }

    public Type? RuntimeType { get; }

    public TypeSymbol(
        string name,
        Symbol? container,
        SymbolAccess access,
        SymbolModifier modifier,
        Func<ImmutableList<TypeSymbol>> fnBaseTypes,
        Func<Symbol, ImmutableList<Symbol>> fnMembers,
        Type? runtimeType = null)
        : base(name, container, access, modifier)
    {
        _fnBaseTypes = fnBaseTypes;
        _fnMembers = fnMembers;
        RuntimeType = runtimeType;
    }

    public TypeSymbol(
        string name,
        Symbol? container,
        SymbolAccess access,
        SymbolModifier modifier,
        ImmutableList<TypeSymbol> baseTypes,
        ImmutableList<Symbol> members,
        Type? runtimeType = null)
        : base(name, container, access, modifier)
    {
        _baseTypes = baseTypes;
        _members = members;
        RuntimeType = runtimeType;
    }

    public TypeSymbol(string name, Type? runtimeType = null)
        : this(
            name, 
            container: null, 
            SymbolAccess.Public, 
            SymbolModifier.None, 
            baseTypes: ImmutableList<TypeSymbol>.Empty, 
            members: ImmutableList<Symbol>.Empty, 
            runtimeType)
    {
    }

    public TypeSymbol(Type runtimeType)
        : this(
            runtimeType.Name,
            container: null,
            SymbolAccess.Public,
            SymbolModifier.None,
            baseTypes: ImmutableList<TypeSymbol>.Empty,
            members: ImmutableList<Symbol>.Empty,
            runtimeType)
    {
    }
}
