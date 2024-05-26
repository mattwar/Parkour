namespace Parkour.Symbols;

public class MethodSymbol : MemberSymbol
{
    /// <summary>
    /// <see cref="TypeParameters"/> for generic method definitions.
    /// </summary>
    public ImmutableList<TypeParameterSymbol> TypeParameters =>
        _lazyTypeParameters?.Value ?? ImmutableList<TypeParameterSymbol>.Empty;
    private readonly Lazy<ImmutableList<TypeParameterSymbol>>? _lazyTypeParameters;

    /// <summary>
    /// Type arguments for constructed generic methods
    /// </summary>
    public ImmutableList<TypeSymbol> TypeArguments => 
        _lazyTypeArguments?.Value ?? ImmutableList<TypeSymbol>.Empty;
    private readonly Lazy<ImmutableList<TypeSymbol>>? _lazyTypeArguments;

    /// <summary>
    /// The parameters of this method.
    /// </summary>
    public ImmutableList<ParameterSymbol> Parameters =>
        _lazyParameters?.Value ?? ImmutableList<ParameterSymbol>.Empty;
    private readonly Lazy<ImmutableList<ParameterSymbol>>? _lazyParameters;

    /// <summary>
    /// The return type of this method.
    /// </summary>
    public TypeSymbol ReturnType => _lazyReturnType.Value;
    private readonly Lazy<TypeSymbol> _lazyReturnType;

    /// <summary>
    /// Custom attributes for this method
    /// </summary>
    public override ImmutableList<AttributeInfo> Attributes =>
        _lazyAttributes?.Value ?? ImmutableList<AttributeInfo>.Empty;
    private readonly Lazy<ImmutableList<AttributeInfo>>? _lazyAttributes;

    public MethodSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<MethodSymbol, ImmutableList<TypeParameterSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnTypeArguments,
        Func<MethodSymbol, ImmutableList<ParameterSymbol>>? fnParameters,
        Func<TypeSymbol> fnReturnType,
        Func<MethodSymbol, ImmutableList<AttributeInfo>>? fnAttributes,
        MethodSymbol? constructedFrom)
        : base(name, declaringSymbol, access, modifiers)
    {
        _lazyTypeParameters = fnTypeParameters != null
            ? new Lazy<ImmutableList<TypeParameterSymbol>>(() => fnTypeParameters(this))
            : null;
        _lazyTypeArguments = fnTypeArguments != null
            ? new Lazy<ImmutableList<TypeSymbol>>(fnTypeArguments)
            : null;
        _lazyParameters = fnParameters != null
            ? new Lazy<ImmutableList<ParameterSymbol>>(() => fnParameters(this))
            : null;
        _lazyReturnType = new Lazy<TypeSymbol>(fnReturnType, SpecialSymbols.CyclicDefinition);
        _lazyAttributes = fnAttributes != null
            ? new Lazy<ImmutableList<AttributeInfo>>(() => fnAttributes(this))
            : null;
        ConstructedFrom = constructedFrom;
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


    public override bool IsConstructable =>
        this.IsGeneric;

    public override int Arity =>
        this.TypeParameters.Count > 0 ? this.TypeParameters.Count
        : this.TypeArguments.Count > 0 ? this.TypeArguments.Count
        : 0;

    public override int DeclaredSymbolCount =>
        this.TypeParameters.Count + this.Parameters.Count;

    public override Symbol? GetDeclaredSymbol(int index)
    {
        if (index < this.TypeParameters.Count)
            return this.TypeParameters[index];

        index -= this.TypeParameters.Count;

        if (index <= this.Parameters.Count)
            return this.Parameters[index];

        return null;
    }

    public override int ReferencedSymbolCount => 
        this.DeclaredSymbolCount + 1;

    public override Symbol? GetReferencedSymbol(int index)
    {
        if (index < this.DeclaredSymbolCount)
            return GetDeclaredSymbol(index);
        
        index -= this.DeclaredSymbolCount;
        
        if (index == 0)
            return this.ReturnType;

        return null;
    }

    internal protected override MethodSymbol Construct(ConstructionContext context)
    {
        var definition = this.ConstructedFrom ?? this;
        var subContext = context.CreateSubstitution(definition.TypeParameters);

        return new MethodSymbol(
            this.Name,
            this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            fnTypeParameters: null,
            () => context.TypeArguments,
            this.Parameters.Count > 0 ? me => subContext.Substitute(this.Parameters, me) : null,
            () => subContext.Substitute(this.ReturnType),
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(subContext)) : null,
            definition
            );
    }

    internal protected override MethodSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new MethodSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            this.TypeParameters.Count > 0 ? me => this.TypeParameters : null,
            this.TypeArguments.Count > 0 ? () => context.Substitute(this.TypeArguments) : null,
            this.Parameters.Count > 0 ? me => context.Substitute(this.Parameters) : null,
            () => context.Substitute(this.ReturnType),
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(context)) : null,
            this.ConstructedFrom ?? (this.IsConstructable ? this : null)
            );
    }
}
