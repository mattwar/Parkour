namespace Parkour.Symbols;

public sealed class ParameterSymbol : Symbol
{
    public Symbol? DeclaringSymbol { get; }

    public BitSet<SymbolModifier> Modifiers { get; }

    /// <summary>
    /// The type of the parameter.
    /// </summary>
    public TypeSymbol Type => _lazyType.Value;
    private readonly Lazy<TypeSymbol> _lazyType;

    /// <summary>
    /// Custom attributes for this parameter
    /// </summary>
    public override ImmutableList<AttributeInfo> Attributes =>
        _lazyAttributes?.Value ?? ImmutableList<AttributeInfo>.Empty;
    private readonly Lazy<ImmutableList<AttributeInfo>>? _lazyAttributes;

    /// <summary>
    /// True if this type parameter is the original definition without type parameter substitution.
    /// </summary>
    public bool IsDefinition => this.Definition == null;

    /// <summary>
    /// The parameter without type parameter substitution.
    /// </summary>
    public ParameterSymbol? Definition { get; }

    public ParameterSymbol(
        string name, 
        Symbol? declaringSymbol,
        BitSet<SymbolModifier> modifiers,
        Func<TypeSymbol> fnType,
        Func<ParameterSymbol, ImmutableList<AttributeInfo>>? fnAttributes,
        ParameterSymbol? definition = null)
        : base(name)
    {
        this.DeclaringSymbol = declaringSymbol;
        this.Modifiers = modifiers;
        this.Definition = definition;
        _lazyType = new Lazy<TypeSymbol>(fnType, SpecialSymbols.CyclicDefinition);
        _lazyAttributes = fnAttributes != null
            ? new Lazy<ImmutableList<AttributeInfo>>(() => fnAttributes(this))
            : null;
    }

    public ParameterSymbol(
        string name, 
        Symbol? declaringSymbol,
        TypeSymbol type)
        : this(
              name,
              declaringSymbol,
              SymbolModifier.None,
              () => type,
              fnAttributes: null,
              definition: null)
    {
    }

    public override int ReferencedSymbolCount => 1;
    public override Symbol? GetReferencedSymbol(int index) => index == 0 ? this.Type : null;

    internal protected override Symbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new ParameterSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Modifiers,
            () => context.Substitute(this.Type),
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(context)) : null,
            this.Definition ?? this
            );
    }
}
