namespace Parkour.Symbols;

public sealed class IndexerSymbol : MemberSymbol
{
    /// <summary>
    /// The type of the indexer's elements.
    /// </summary>
    public TypeSymbol ElementType => _lazyElementType.Value;
    private readonly Lazy<TypeSymbol> _lazyElementType;

    /// <summary>
    /// The method for the get accessor (if any).
    /// </summary>
    public MethodSymbol? GetMethod => _lazyGetMethod?.Value;
    private readonly Lazy<MethodSymbol>? _lazyGetMethod;

    /// <summary>
    /// The method for the set accessor (if any.)
    /// </summary>
    public MethodSymbol? SetMethod => _lazySetMethod?.Value;
    private readonly Lazy<MethodSymbol>? _lazySetMethod;

    /// <summary>
    /// Custom attributes for this indexer
    /// </summary>
    public override ImmutableList<AttributeInfo> Attributes =>
        _lazyAttributes?.Value ?? ImmutableList<AttributeInfo>.Empty;
    private readonly Lazy<ImmutableList<AttributeInfo>>? _lazyAttributes;

    /// <summary>
    /// The indexer definition without substituted type parameter references.
    /// </summary>
    public new IndexerSymbol? Definition => base.Definition as IndexerSymbol;

    public IndexerSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<TypeSymbol> fnElementType,
        Func<IndexerSymbol, MethodSymbol>? fnGetMethod,
        Func<IndexerSymbol, MethodSymbol>? fnSetMethod,
        Func<IndexerSymbol, ImmutableList<AttributeInfo>>? fnAttributes,
        IndexerSymbol? definition = null)
        : base(name, declaringSymbol, access, modifiers, definition)
    {
        _lazyElementType = new Lazy<TypeSymbol>(fnElementType, SpecialSymbols.CyclicDefinition);
        _lazyGetMethod = fnGetMethod != null
            ? new Lazy<MethodSymbol>(() => fnGetMethod(this))
            : null;
        _lazySetMethod = fnSetMethod != null
            ? new Lazy<MethodSymbol>(() => fnSetMethod(this))
            : null;
        _lazyAttributes = fnAttributes != null
            ? new Lazy<ImmutableList<AttributeInfo>>(() => fnAttributes(this))
            : null;
    }

    public override int DeclaredSymbolCount => 2;

    public override Symbol? GetDeclaredSymbol(int index) =>
        index switch
        {
            0 => GetMethod,
            1 => SetMethod,
            _ => null
        };

    public override int ReferencedSymbolCount => this.DeclaredSymbolCount + 1;

    public override Symbol? GetReferencedSymbol(int index)
    {
        if (index < this.DeclaredSymbolCount)
            return this.GetDeclaredSymbol(index);

        index -= this.DeclaredSymbolCount;

        return (index == 0) ? this.ElementType : null;
    }

    internal protected override Symbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new IndexerSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            () => context.Substitute(this.ElementType),
            this.GetMethod != null ? me => context.Substitute(this.GetMethod) : null,
            this.SetMethod != null ? me => context.Substitute(this.SetMethod) : null,
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(context)) : null
            );
    }
}
