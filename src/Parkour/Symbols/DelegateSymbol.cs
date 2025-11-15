namespace Parkour.Symbols;

public class DelegateSymbol : TypeSymbol
{
    /// <summary>
    /// The delegate parameters.
    /// </summary>
    public ImmutableList<ParameterSymbol> Parameters => 
        _lazyParameters?.Value ?? ImmutableList<ParameterSymbol>.Empty;
    private readonly Lazy<ImmutableList<ParameterSymbol>>? _lazyParameters;

    /// <summary>
    /// The delegate's return type.
    /// </summary>
    public TypeSymbol ReturnType => _lazyReturnType.Value;
    private readonly Lazy<TypeSymbol> _lazyReturnType;

    /// <summary>
    /// Custom attributes for this delegate
    /// </summary>
    public override ImmutableList<AttributeInfo> Attributes =>
        _lazyAttributes?.Value ?? ImmutableList<AttributeInfo>.Empty;
    private readonly Lazy<ImmutableList<AttributeInfo>>? _lazyAttributes;

    /// <summary>
    /// The definition of the delegate without substituted type parameters.
    /// </summary>
    public new DelegateSymbol? Definition => base.Definition as DelegateSymbol;

    private DelegateSymbol(
        string name,
        Symbol? declaringSymbol,
        Access access,
        BitSet<Modifier> modifiers,
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>>? fnParameters,
        Func<TypeSymbol> fnReturnType,
        Func<TypeSymbol, ImmutableList<TypeParameterSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnTypeArguments,
        Func<ImmutableList<TypeSymbol>>? fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes,
        TypeSymbol? definition = null)
        : base(
            name, 
            declaringSymbol, 
            access, 
            modifiers, 
            fnTypeParameters, 
            fnTypeArguments, 
            fnBaseTypes, 
            fnMembers, 
            fnAttributes,
            definition)
    {
        _lazyParameters = fnParameters != null
            ? new Lazy<ImmutableList<ParameterSymbol>>(() => fnParameters(this))
            : null;
        _lazyReturnType = new Lazy<TypeSymbol>(fnReturnType, SpecialSymbols.CyclicDefinition);
        _lazyAttributes = fnAttributes != null
            ? new Lazy<ImmutableList<AttributeInfo>>(() => fnAttributes(this))
            : null;
    }

    public DelegateSymbol(
        string name,
        Symbol? declaringSymbol,
        Access access,
        BitSet<Modifier> modifiers,
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>>? fnParameters,
        Func<TypeSymbol> fnReturnType,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            fnParameters,
            fnReturnType,
            fnTypeParameters: null, 
            fnTypeArguments: null, 
            fnBaseTypes: null, 
            fnMembers: null, 
            fnAttributes,
            null)
    {
    }

    public DelegateSymbol(
        string name,
        Symbol? declaringSymbol,
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType)
        : this(
            name,
            declaringSymbol,
            Access.Public,
            Modifier.None,
            fnParameters,
            fnReturnType,
            fnAttributes: null)
    {
    }

    internal protected override TypeSymbol Construct(ConstructionContext context)
    {
        var definition = this.Definition ?? this;
        var subContext = context.CreateSubstitution(definition.TypeParameters);

        return new DelegateSymbol(
            this.Name,
            this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            this.Parameters.Count > 0 ? me => subContext.Substitute(this.Parameters) : null,
            () => subContext.Substitute(this.ReturnType),
            fnTypeParameters: null,
            () => context.TypeArguments,
            this.BaseTypes.Count > 0 ? () => subContext.Substitute(this.BaseTypes) : null,
            this.Members.Count > 0 ? me => subContext.Substitute(this.Members, me) : null,
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(subContext)) : null,
            definition
            );
    }

    internal protected override TypeSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        var newDeclaringSymbol =
            declaringSymbol ?? this.DeclaringSymbol;

        return new DelegateSymbol(
            this.Name,
            newDeclaringSymbol,
            this.Access,
            this.Modifiers,
            this.Parameters.Count > 0 ? me => context.Substitute(this.Parameters) : null,
            () => context.Substitute(this.ReturnType),
            this.TypeParameters.Count > 0 ? me => this.TypeParameters : null,
            this.TypeArguments.Count > 0 ? () => context.Substitute(this.TypeArguments) : null,
            this.BaseTypes.Count > 0 ? () => context.Substitute(this.BaseTypes) : null,
            this.Members.Count > 0 ? me => context.Substitute(this.Members) : null,
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(context)) : null,
            this.Definition ?? this
            );
    }

    public override int DeclaredSymbolCount =>
        this.Parameters.Count;

    public override Symbol? GetDeclaredSymbol(int index) =>
        this.Parameters[index];

    public override int ReferencedSymbolCount =>
        this.DeclaredSymbolCount + 1;

    public override Symbol? GetReferencedSymbol(int index)
    {
        if (index <= this.DeclaredSymbolCount)
            return this.GetDeclaredSymbol(index);

        index -= this.DeclaredSymbolCount;

        if (index == 0)
            return this.ReturnType;

        return null;
    }
}
