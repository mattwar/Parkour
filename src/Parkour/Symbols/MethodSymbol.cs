namespace Parkour.Symbols;

public class MethodSymbol : MemberSymbol
{
    private Func<MethodSymbol, ImmutableList<TypeParameterSymbol>>? _fnTypeParameters;
    private ImmutableList<TypeParameterSymbol>? _typeParameters;
    private Func<ImmutableList<TypeSymbol>>? _fnTypeArguments;
    private ImmutableList<TypeSymbol>? _typeArguments;
    private Func<MethodSymbol, ImmutableList<ParameterSymbol>>? _fnParameters;
    private ImmutableList<ParameterSymbol>? _parameters;
    private Func<TypeSymbol>? _fnReturnType;
    private TypeSymbol? _returnType;

    public MethodSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<MethodSymbol, ImmutableList<TypeParameterSymbol>> fnTypeParameters,
        Func<ImmutableList<TypeSymbol>> fnTypeArguments,
        Func<MethodSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType,
        MethodSymbol? constructedFrom)
        : base(name, declaringSymbol, access, modifiers)
    {
        _fnTypeParameters = fnTypeParameters;
        _fnTypeArguments = fnTypeArguments;
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
        ConstructedFrom = constructedFrom;
    }

    public MethodSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<TypeParameterSymbol> typeParameters,
        ImmutableList<TypeSymbol> typeArguments,
        ImmutableList<ParameterSymbol> parameters,
        TypeSymbol returnType,
        MethodSymbol? constructedFrom)
        : this(
              name,
              declaringSymbol,
              access,
              modifiers,
              me => typeParameters,
              () => typeArguments,
              me => parameters,
              () => returnType,
              constructedFrom)
    {
    }

    /// <summary>
    /// <see cref="TypeParameters"/> for generic method definitions.
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
    /// Type arguments for constructed generic methods
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
    /// True if the method is generic (definition or constructed)
    /// </summary>
    public bool IsGeneric =>
        this.TypeParameters.Count > 0
        || this.TypeArguments.Count > 0;

    /// <summary>
    /// True if the method is a generic method definition.
    /// </summary>
    public bool IsDefinition => 
        IsGeneric && this.TypeArguments.Count == 0;

    /// <summary>
    /// True if the method is a constructed generic method.
    /// </summary>
    public bool IsConstructed => 
        IsGeneric && this.TypeArguments.Count > 0;

    /// <summary>
    /// The method this method is constructed from.
    /// </summary>
    public MethodSymbol? ConstructedFrom { get; }

    /// <summary>
    /// The parameters of this method.
    /// </summary>
    public ImmutableList<ParameterSymbol> Parameters
    {
        get
        {
            if (_parameters == null && _fnParameters is { } fn)
            {
                _fnParameters = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _parameters, tmp, null);
            }

            return _parameters!;
        }
    }

    /// <summary>
    /// The return type of this method.
    /// </summary>
    public TypeSymbol ReturnType
    {
        get
        {
            if (_returnType == null && _fnReturnType is { } fn)
            {
                _fnReturnType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _returnType, tmp, null);
            }

            return _returnType!;
        }
    }

    public override int Arity =>
        this.TypeParameters.Count > 0 ? this.TypeParameters.Count
        : this.TypeArguments.Count > 0 ? this.TypeArguments.Count
        : 0;

    public override int DeclarationCount =>
        this.TypeParameters.Count + this.Parameters.Count;

    public override Symbol? GetDeclaration(int index) =>
        index < this.TypeParameters.Count
            ? this.TypeParameters[index]
            : this.Parameters[index = this.TypeParameters.Count];

    public override bool IsConstructable =>
        this.IsGeneric;

    internal protected override MethodSymbol Construct(ConstructionContext context)
    {
        var definition = this.ConstructedFrom ?? this;
        var subContext = context.CreateSubstitution(definition.TypeParameters);

        return new MethodSymbol(
            this.Name,
            this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => ImmutableList<TypeParameterSymbol>.Empty,
            () => context.TypeArguments,
            me => subContext.Substitute(this.Parameters, me),
            () => subContext.Substitute(this.ReturnType),
            definition);
    }

    internal protected override MethodSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new MethodSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => this.TypeParameters,
            () => context.Substitute(this.TypeArguments),
            me => context.Substitute(this.Parameters),
            () => context.Substitute(this.ReturnType),
            this.ConstructedFrom);
    }
}
