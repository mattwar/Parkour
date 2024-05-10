namespace Parkour.Symbols;

public sealed class FieldSymbol : MemberSymbol
{
    private Func<TypeSymbol>? _fnType;
    private TypeSymbol? _type;

    public TypeSymbol Type
    {
        get
        {
            if (_type == null && _fnType is { } fn)
            {
                _fnType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _type, tmp, null);
            }

            return _type ?? SpecialSymbols.Unknown;
        }
    }

    public FieldSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<TypeSymbol> fnType)
        : base(name, declaringSymbol, access, modifiers)
    {
        _fnType = fnType;
    }

    public FieldSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        TypeSymbol type)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            () => type)
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
