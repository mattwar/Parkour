namespace Parkour.Symbols;

public sealed class PropertySymbol : MemberSymbol
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

            return _type!;
        }
    }

    private Func<PropertySymbol, FieldSymbol>? _fnBackingField;
    private FieldSymbol? _backingField;

    public FieldSymbol? BackingField
    {
        get
        {
            if (_backingField == null && _fnBackingField is { } fn)
            {
                _fnBackingField = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _backingField, tmp, null);
            }

            return _backingField;
        }
    }

    private Func<PropertySymbol, MethodSymbol>? _fnGetMethod;
    private MethodSymbol? _getMethod;

    public MethodSymbol? GetMethod
    {
        get
        {
            if (_getMethod == null && _fnGetMethod is { } fn)
            {
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _getMethod, tmp, null);
                _fnGetMethod = null;
            }

            return _getMethod;
        }
    }

    private Func<PropertySymbol, MethodSymbol>? _fnSetMethod;
    private MethodSymbol? _setMethod;

    public MethodSymbol? SetMethod
    {
        get
        {
            if (_setMethod == null && _fnSetMethod is { } fn)
            {
                _fnSetMethod = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _setMethod, tmp, null);
            }

            return _setMethod;
        }
    }

    public PropertySymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<TypeSymbol> fnType,
        Func<PropertySymbol, FieldSymbol>? fnBackingField,
        Func<PropertySymbol, MethodSymbol>? fnGetMethod,
        Func<PropertySymbol, MethodSymbol>? fnSetMethod)
        : base(name, declaringSymbol, access, modifiers)
    {
        _fnBackingField = fnBackingField;
        _fnType = fnType;
        _fnGetMethod = fnGetMethod;
        _fnSetMethod = fnSetMethod;
    }

    public PropertySymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        TypeSymbol type,
        FieldSymbol? backingField,
        MethodSymbol? getMethod,
        MethodSymbol? setMethod)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            () => type,
            backingField != null ? me => backingField : null,
            getMethod != null ? me => getMethod : null,
            setMethod != null ? me => setMethod : null)
    {
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
            this.BackingField != null
                ? me => context.Substitute(this.BackingField)
                : null,
            this.GetMethod != null
                ? me => context.Substitute(this.GetMethod)
                : null,
            this.SetMethod != null
                ? me => context.Substitute(this.SetMethod)
                : null);
    }
}
