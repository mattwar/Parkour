namespace Parkour.Symbols;

public class TypeSymbol : NamespaceOrTypeSymbol
{
    public override SymbolAccess Access { get; }
    public override SymbolModifier Modifiers { get; }
    public override MemberSymbol? Container { get; }
    public TypeSymbol? DeclaringType => Container as TypeSymbol;

    private Func<ImmutableList<TypeParameterSymbol>>? _fnTypeParameters;
    private ImmutableList<TypeParameterSymbol>? _typeParameters;
    private Func<ImmutableList<TypeSymbol>>? _fnTypeArguments;
    private ImmutableList<TypeSymbol>? _typeArguments;
    private Func<ImmutableList<TypeSymbol>>? _fnBaseTypes;
    private ImmutableList<TypeSymbol>? _baseTypes;
    private Func<TypeSymbol, ImmutableList<Symbol>>? _fnMembers;
    private ImmutableList<Symbol>? _members;

    /// <summary>
    /// The type parameters for this generic type definition.
    /// </summary>
    public ImmutableList<TypeParameterSymbol> TypeParameters
    {
        get
        {
            if (_typeParameters == null && _fnTypeParameters is { } fn)
            {
                _fnTypeParameters = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _typeParameters, tmp, null);
            }

            return _typeParameters ?? ImmutableList<TypeParameterSymbol>.Empty;
        }
    }

    /// <summary>
    /// The type arguments for this constructed generic type.
    /// </summary>
    public ImmutableList<TypeSymbol> TypeArguments
    {
        get
        {
            if (_typeArguments == null && _fnTypeArguments is { } fn)
            {
                _fnTypeArguments = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _typeArguments, tmp, null);
            }

            return _typeArguments ?? ImmutableList<TypeSymbol>.Empty;
        }
    }

    /// <summary>
    /// True if this type is a generic type definition or a constructed generic type.
    /// </summary>
    public bool IsGeneric => IsDefinition || IsConstructed;

    /// <summary>
    /// True if this type is a generic type definition.
    /// </summary>
    public bool IsDefinition => this.TypeParameters.Count > 0;

    /// <summary>
    /// True if this type is a constructed generic type.
    /// </summary>
    public bool IsConstructed => this.TypeArguments.Count > 0;

    /// <summary>
    /// The generic type definition of this constructed generic type.
    /// </summary>
    public TypeSymbol? Definition { get; }

    /// <summary>
    /// The base type and interfaces of this type.
    /// </summary>
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

    /// <summary>
    /// The members of this type.
    /// </summary>
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
        Func<ImmutableList<TypeParameterSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnTypeArguments,
        Func<ImmutableList<TypeSymbol>>? fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        TypeSymbol? genericDefinition,
        Type? runtimeType)
        : base(name)
    {
        Container = container;
        Access = access;
        Modifiers = modifiers;
        _fnTypeParameters = fnTypeParameters;
        _fnTypeArguments = fnTypeArguments;
        _fnBaseTypes = fnBaseTypes;
        _fnMembers = fnMembers;
        this.Definition = genericDefinition;
        this.RuntimeType = runtimeType;
    }

    public TypeSymbol(
        string name,
        MemberSymbol? container,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<TypeParameterSymbol> typeParameters,
        ImmutableList<TypeSymbol> typeArguments,
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
              () => typeArguments,
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
            ImmutableList<TypeParameterSymbol>.Empty,
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

    public override int DeclarationCount =>
        this.TypeArguments.Count + this.Members.Count;

    public override Symbol? GetDeclaration(int index) =>
        index < this.TypeArguments.Count
            ? this.TypeArguments[index]
            : this.Members[index - this.TypeArguments.Count];
}
