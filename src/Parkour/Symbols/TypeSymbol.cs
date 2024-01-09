namespace Parkour.Symbols;

public class TypeSymbol : MemberSymbol
{
    public override SymbolAccess Access { get; }
    public override SymbolModifier Modifiers { get; }
    public override MemberSymbol? Container { get; }
    public TypeSymbol? DeclaringType => Container as TypeSymbol;

    private Func<ImmutableList<TypeSymbol>>? _fnTypeParameters;
    private ImmutableList<TypeSymbol>? _typeParameters;

    public ImmutableList<TypeSymbol> TypeParameters
    {
        get
        {
            if (_typeParameters == null && _fnTypeParameters is { } fn)
            {
                _fnTypeParameters = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _typeParameters, tmp, null);
            }

            return _typeParameters ?? ImmutableList<TypeSymbol>.Empty;
        }
    }

    public bool IsDefinition => this.GenericDefinition != null;
    public bool IsGeneric => this.TypeParameters.Count > 0;
    public bool IsConcrete => this.IsGeneric && !IsDefinition;

    public TypeSymbol? GenericDefinition { get; }

    private Func<ImmutableList<TypeSymbol>>? _fnBaseTypes;
    private ImmutableList<TypeSymbol>? _baseTypes;

    public ImmutableList<TypeSymbol> BaseTypes
    {
        get
        {
            if (_baseTypes == null && _fnBaseTypes is { } fn)
            {
                _fnBaseTypes = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _baseTypes, tmp, null);
            }

            return _baseTypes ?? ImmutableList<TypeSymbol>.Empty;
        }
    }

    private Func<TypeSymbol, ImmutableList<Symbol>>? _fnMembers;
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

    public Type? RuntimeType { get; }

    public TypeSymbol(
        string name,
        MemberSymbol? container,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<ImmutableList<TypeSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        TypeSymbol? genericDefinition,
        Type? runtimeType)
        : base(name)
    {
        Container = container;
        Access = access;
        Modifiers = modifiers;
        _fnBaseTypes = fnBaseTypes;
        _fnMembers = fnMembers;
        _fnTypeParameters = fnTypeParameters;
        this.GenericDefinition = genericDefinition;
        this.RuntimeType = runtimeType;
    }

    public TypeSymbol(
        string name,
        MemberSymbol? container,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<TypeSymbol> typeParameters,
        ImmutableList<TypeSymbol> baseTypes,
        ImmutableList<Symbol> members,
        TypeSymbol? genericDefinition,
        Type? runtimeType)
        : this(
              name,
              container,
              access,
              modifiers,
              () => typeParameters,
              () => baseTypes,
              me => members,
              genericDefinition,
              runtimeType)
    {
    }

    public TypeSymbol(string name, Type? runtimeType = null)
        : this(
            name, 
            container: null, 
            SymbolAccess.Public, 
            SymbolModifier.None,
            ImmutableList<TypeSymbol>.Empty,
            ImmutableList<TypeSymbol>.Empty,
            ImmutableList<Symbol>.Empty,
            genericDefinition: null,
            runtimeType)
    {
    }

    public TypeSymbol(Type runtimeType)
        : this(
            runtimeType.Name,
            runtimeType)
    {
    }
}
