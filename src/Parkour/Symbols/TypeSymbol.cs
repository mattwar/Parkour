namespace Parkour.Symbols;

public class TypeSymbol : ContainerSymbol
{
    private Func<TypeSymbol, ImmutableList<TypeParameterSymbol>>? _fnTypeParameters;
    private ImmutableList<TypeParameterSymbol>? _typeParameters;
    private Func<ImmutableList<TypeSymbol>>? _fnTypeArguments;
    private ImmutableList<TypeSymbol>? _typeArguments;
    private Func<ImmutableList<TypeSymbol>>? _fnBaseTypes;
    private ImmutableList<TypeSymbol>? _baseTypes;
    private Func<TypeSymbol, ImmutableList<Symbol>>? _fnMembers;
    private ImmutableList<Symbol>? _members;

    public TypeSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<TypeSymbol, ImmutableList<TypeParameterSymbol>> fnTypeParameters,
        Func<ImmutableList<TypeSymbol>> fnTypeArguments,
        Func<ImmutableList<TypeSymbol>> fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>> fnMembers,
        TypeSymbol? constructedFrom)
        : base(name, declaringSymbol, access, modifiers)
    {
        _fnTypeParameters = fnTypeParameters;
        _fnTypeArguments = fnTypeArguments;
        _fnBaseTypes = fnBaseTypes;
        _fnMembers = fnMembers;
        ConstructedFrom = constructedFrom;
    }

    public TypeSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<TypeParameterSymbol> typeParameters,
        ImmutableList<TypeSymbol> typeArguments,
        ImmutableList<TypeSymbol> baseTypes,
        ImmutableList<Symbol> members,
        TypeSymbol? constructedFrom)
        : this(
              name,
              declaringSymbol,
              access,
              modifiers,
              me => typeParameters,
              () => typeArguments,
              () => baseTypes,
              me => members,
              constructedFrom)
    {
    }

    public TypeSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            ImmutableList<TypeParameterSymbol>.Empty,
            ImmutableList<TypeSymbol>.Empty,
            ImmutableList<TypeSymbol>.Empty,
            ImmutableList<Symbol>.Empty,
            constructedFrom: null)
    {
    }

    public TypeSymbol(string name)
        : this(
            name,
            declaringSymbol: null,
            SymbolAccess.Public,
            SymbolModifier.None)
    {
    }

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
                var tmp = fn(this);
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
    public bool IsGeneric => 
        this.TypeParameters.Count > 0
        || this.TypeArguments.Count > 0;

    /// <summary>
    /// True if this type is a generic type definition.
    /// </summary>
    public bool IsDefinition => 
        IsGeneric && this.TypeArguments.Count == 0;

    /// <summary>
    /// True if this type is a constructed generic type.
    /// </summary>
    public bool IsConstructed => 
        IsGeneric && this.TypeArguments.Count > 0;

    /// <summary>
    /// The type this type is constructed from.
    /// </summary>
    public TypeSymbol? ConstructedFrom { get; }

    /// <summary>
    /// True if the type is an interface
    /// </summary>
    public virtual bool IsInterface => false;

    public virtual bool IsValueType => false;

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

    public override int Arity =>
        IsConstructed
            ? this.TypeArguments.Count
            : this.TypeParameters.Count;

    public override int DeclarationCount =>
        this.TypeParameters.Count + this.Members.Count;

    public override Symbol? GetDeclaration(int index) =>
        index < this.TypeParameters.Count
            ? this.TypeParameters[index]
            : this.Members[index - this.TypeParameters.Count];

    public override bool IsConstructable =>
        this.IsGeneric;
        
    internal protected override TypeSymbol Construct(ConstructionContext context)
    {
        var definition = this.ConstructedFrom ?? this;
        var subContext = context.CreateSubstitution(definition.TypeParameters);

        return new TypeSymbol(
            this.Name,
            this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => ImmutableList<TypeParameterSymbol>.Empty,
            () => context.TypeArguments,
            () => subContext.Substitute(this.BaseTypes),
            me => subContext.Substitute(this.Members, me),
            definition);
    }

    internal protected override TypeSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new TypeSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => this.TypeParameters,
            () => context.Substitute(this.TypeArguments),
            () => context.Substitute(this.BaseTypes),
            me => context.Substitute(this.Members),
            this.ConstructedFrom);
    }
}