namespace Parkour.Symbols;

public sealed class FieldSymbol : MemberSymbol
{
    private Func<TypeSymbol>? _fnFieldType;
    private TypeSymbol? _fieldType;

    public TypeSymbol FieldType
    {
        get
        {
            if (_fieldType == null && _fnFieldType is { } fn)
            {
                _fnFieldType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _fieldType, tmp, null);
            }

            return _fieldType ?? SpecialSymbols.Unknown;
        }
    }

    public FieldSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<TypeSymbol> fnFieldType)
        : base(name, declaringSymbol, access, modifiers)
    {
        _fnFieldType = fnFieldType;
    }

    public FieldSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        TypeSymbol fieldType)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            () => fieldType)
    {
    }

    public override int ReferenceCount => 0;

    public override Symbol? GetReference(int index)
    {
        return index == 0 ? this.FieldType : null;
    }

    internal protected override FieldSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new FieldSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            () => context.Substitute(this.FieldType));
    }
}
