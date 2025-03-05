namespace Parkour.Symbols;

public sealed class PropertySymbol : MemberSymbol
{
    /// <summary>
    /// The type of the property.
    /// </summary>
    public TypeSymbol Type => _lazyType.Value;
    private readonly Lazy<TypeSymbol> _lazyType;

    /// <summary>
    /// The symbol of the optional backing field.
    /// </summary>
    public FieldSymbol? BackingField => _lazyBackingField?.Value;
    private readonly Lazy<FieldSymbol>? _lazyBackingField;

    /// <summary>
    /// The method symbol of the get accessor.
    /// </summary>
    public MethodSymbol? GetMethod => _lazyGetMethod?.Value;
    private readonly Lazy<MethodSymbol>? _lazyGetMethod;

    /// <summary>
    /// The method symbol of the set accessor.
    /// </summary>
    public MethodSymbol? SetMethod => _lazySetMethod?.Value;
    private readonly Lazy<MethodSymbol>? _lazySetMethod;

    /// <summary>
    /// Custom attributes for this property
    /// </summary>
    public override ImmutableList<AttributeInfo> Attributes =>
        _lazyAttributes?.Value ?? ImmutableList<AttributeInfo>.Empty;
    private readonly Lazy<ImmutableList<AttributeInfo>>? _lazyAttributes;

    /// <summary>
    /// The definition of the property without substituted type parameter references.
    /// </summary>
    public new PropertySymbol? Definition => base.Definition as PropertySymbol;

    public PropertySymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<TypeSymbol> fnType,
        Func<PropertySymbol, FieldSymbol>? fnBackingField,
        Func<PropertySymbol, MethodSymbol>? fnGetMethod,
        Func<PropertySymbol, MethodSymbol>? fnSetMethod,
        Func<PropertySymbol, ImmutableList<AttributeInfo>>? fnAttributes,
        PropertySymbol? definition = null)
        : base(name, declaringSymbol, access, modifiers, definition)
    {
        _lazyType = new Lazy<TypeSymbol>(fnType, SpecialSymbols.CyclicDefinition);
        _lazyBackingField = fnBackingField != null
            ? new Lazy<FieldSymbol>(() => fnBackingField(this))
            : null;
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

    public override int DeclaredSymbolCount => 3;
    public override Symbol? GetDeclaredSymbol(int index) =>
        index switch
        {
            0 => GetMethod,
            1 => SetMethod,
            2 => BackingField,
            _ => null
        };

    public override int ReferencedSymbolCount => this.DeclaredSymbolCount + 1;
    public override Symbol? GetReferencedSymbol(int index)
    {
        if (index < this.DeclaredSymbolCount)
            return this.GetDeclaredSymbol(index);
        index -= this.DeclaredSymbolCount;
        return index == 0 ? this.Type : null;
    }

    internal protected override Symbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new PropertySymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            () => context.Substitute(this.Type),
            this.BackingField != null ? me => context.Substitute(this.BackingField) : null,
            this.GetMethod != null ? me => context.Substitute(this.GetMethod) : null,
            this.SetMethod != null ? me => context.Substitute(this.SetMethod) : null,
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(context)) : null,
            this.Definition ?? this
            );
    }
}
