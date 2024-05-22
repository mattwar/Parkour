namespace Parkour.Symbols;

public sealed class FieldSymbol : MemberSymbol
{
    /// <summary>
    /// The field's type.
    /// </summary>
    public TypeSymbol Type => _lazyType.Value;
    private readonly Lazy<TypeSymbol> _lazyType;

    /// <summary>
    /// The constant value of the field (constant fields only).
    /// </summary>
    public object? ConstantValue { get; }

    public FieldSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<TypeSymbol> fnType,
        object? constantValue = null)
        : base(name, declaringSymbol, access, modifiers)
    {
        _lazyType = new Lazy<TypeSymbol>(fnType, SpecialSymbols.CyclicDefinition);
        this.ConstantValue = constantValue;
    }

    public FieldSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        TypeSymbol type,
        object? constantValue = null)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            () => type,
            constantValue)
    {
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
            () => context.Substitute(this.Type));
    }
}
