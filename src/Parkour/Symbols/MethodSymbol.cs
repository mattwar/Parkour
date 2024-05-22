namespace Parkour.Symbols;

public class MethodSymbol : MemberSymbol
{
    /// <summary>
    /// <see cref="TypeParameters"/> for generic method definitions.
    /// </summary>
    public ImmutableList<TypeParameterSymbol> TypeParameters => _lazyTypeParameters.Value;
    private readonly Lazy<ImmutableList<TypeParameterSymbol>> _lazyTypeParameters;

    /// <summary>
    /// Type arguments for constructed generic methods
    /// </summary>
    public ImmutableList<TypeSymbol> TypeArguments => _lazyTypeArguments.Value;
    private readonly Lazy<ImmutableList<TypeSymbol>> _lazyTypeArguments;

    /// <summary>
    /// The parameters of this method.
    /// </summary>
    public ImmutableList<ParameterSymbol> Parameters => _lazyParameters.Value;
    private readonly Lazy<ImmutableList<ParameterSymbol>> _lazyParameters;

    /// <summary>
    /// The return type of this method.
    /// </summary>
    public TypeSymbol ReturnType => _lazyReturnType.Value;
    private readonly Lazy<TypeSymbol> _lazyReturnType;

    public MethodSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<MethodSymbol, ImmutableList<TypeParameterSymbol>> fnTypeParameters,
        Func<ImmutableList<TypeSymbol>> fnTypeArguments,
        Func<MethodSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType,
        MethodSymbol? constructedFrom)
        : base(name, declaringSymbol, access, modifiers)
    {
        _lazyTypeParameters = new Lazy<ImmutableList<TypeParameterSymbol>>(() => fnTypeParameters(this));
        _lazyTypeArguments = new Lazy<ImmutableList<TypeSymbol>>(fnTypeArguments);
        _lazyParameters = new Lazy<ImmutableList<ParameterSymbol>>(() => fnParameters(this));
        _lazyReturnType = new Lazy<TypeSymbol>(fnReturnType, SpecialSymbols.CyclicDefinition);
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
            this.ConstructedFrom ?? (this.IsConstructable ? this : null));
    }
}
