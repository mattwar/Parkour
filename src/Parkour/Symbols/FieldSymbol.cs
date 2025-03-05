namespace Parkour.Symbols;

public sealed class FieldSymbol : MemberSymbol
{
    /// <summary>
    /// The field's type.
    /// </summary>
    public TypeSymbol Type => _lazyType.Value;
    private readonly Lazy<TypeSymbol> _lazyType;

    /// <summary>
    /// Custom attributes for this delegate
    /// </summary>
    public override ImmutableList<AttributeInfo> Attributes =>
        _lazyAttributes?.Value ?? ImmutableList<AttributeInfo>.Empty;
    private readonly Lazy<ImmutableList<AttributeInfo>>? _lazyAttributes;

    /// <summary>
    /// The constant value of the field (constant fields only).
    /// </summary>
    public object? ConstantValue { get; }

    /// <summary>
    /// The field definition without substituted type parameter references.
    /// </summary>
    public new FieldSymbol? Definition => base.Definition as FieldSymbol;

    public FieldSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<TypeSymbol> fnType,
        Func<FieldSymbol, ImmutableList<AttributeInfo>>? fnAttributes,
        object? constantValue = null,
        FieldSymbol? definition = null)
        : base(name, declaringSymbol, access, modifiers, definition)
    {
        _lazyType = new Lazy<TypeSymbol>(fnType, SpecialSymbols.CyclicDefinition);
        _lazyAttributes = fnAttributes != null
            ? new Lazy<ImmutableList<AttributeInfo>>(() => fnAttributes(this))
            : null;
        this.ConstantValue = constantValue;
    }

    public override int ReferencedSymbolCount => 0;

    public override Symbol? GetReferencedSymbol(int index)
    {
        return index == 0 ? this.Type : null;
    }

    internal protected override FieldSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new FieldSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            () => context.Substitute(this.Type),
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(context)) : null,
            this.ConstantValue,
            definition: this.Definition ?? this
            );
    }
}
