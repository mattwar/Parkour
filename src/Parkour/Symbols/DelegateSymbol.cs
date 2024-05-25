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

    private DelegateSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>>? fnParameters,
        Func<TypeSymbol> fnReturnType,
        Func<TypeSymbol, ImmutableList<TypeParameterSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnTypeArguments,
        Func<ImmutableList<TypeSymbol>>? fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        TypeSymbol? constructedFrom)
        : base(
            name, 
            declaringSymbol, 
            access, 
            modifiers, 
            fnTypeParameters, 
            fnTypeArguments, 
            fnBaseTypes, 
            fnMembers, 
            fnAttributes: null,
            constructedFrom)
    {
        _lazyParameters = fnParameters != null
            ? new Lazy<ImmutableList<ParameterSymbol>>(() => fnParameters(this))
            : null;
        _lazyReturnType = new Lazy<TypeSymbol>(fnReturnType, SpecialSymbols.CyclicDefinition);
    }

    public DelegateSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            fnParameters,
            fnReturnType,
            null, null, null, null, null)
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
            SymbolAccess.Public,
            SymbolModifier.None,
            fnParameters,
            fnReturnType,
            null, null, null, null, null)
    {
    }

    internal protected override TypeSymbol Construct(ConstructionContext context)
    {
        var definition = this.ConstructedFrom ?? this;
        var subContext = context.CreateSubstitution(definition.TypeParameters);

        return new DelegateSymbol(
            this.Name,
            this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => subContext.Substitute(this.Parameters),
            () => subContext.Substitute(this.ReturnType),
            me => ImmutableList<TypeParameterSymbol>.Empty,
            () => context.TypeArguments,
            () => subContext.Substitute(this.BaseTypes),
            me => subContext.Substitute(this.Members, me),
            definition);
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
            me => context.Substitute(this.Parameters),
            () => context.Substitute(this.ReturnType),
            me => this.TypeParameters,
            () => context.Substitute(this.TypeArguments),
            () => context.Substitute(this.BaseTypes),
            me => context.Substitute(this.Members),
            this.ConstructedFrom ?? (this.IsConstructable ? this : null));
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
