namespace Parkour.Symbols;

public sealed class PropertySymbol : MemberSymbol
{
    private Func<TypeSymbol>? _fnPropertyType;
    private TypeSymbol? _propertyType;

    public TypeSymbol PropertyType 
    { 
        get
        {
            if (_propertyType == null && _fnPropertyType is { } fn)
            {
                _fnPropertyType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _propertyType, tmp, null);
            }

            return _propertyType!;
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
        Func<TypeSymbol> fnPropertyType,
        Func<PropertySymbol, FieldSymbol>? fnBackingField,
        Func<PropertySymbol, MethodSymbol>? fnGetMethod,
        Func<PropertySymbol, MethodSymbol>? fnSetMethod)
        : base(name, declaringSymbol, access, modifiers)
    {
        _fnBackingField = fnBackingField;
        _fnPropertyType = fnPropertyType;
        _fnGetMethod = fnGetMethod;
        _fnSetMethod = fnSetMethod;
    }

    public PropertySymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        TypeSymbol propertyType,
        FieldSymbol? backingField,
        MethodSymbol? getMethod,
        MethodSymbol? setMethod)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            () => propertyType,
            backingField != null ? me => backingField : null,
            getMethod != null ? me => getMethod : null,
            setMethod != null ? me => setMethod : null)
    {
    }

    public override int DeclarationCount => 3;
    public override Symbol? GetDeclaration(int index) =>
        index switch
        {
            0 => GetMethod,
            1 => SetMethod,
            2 => BackingField,
            _ => null
        };

    public override int ReferenceCount => this.DeclarationCount + 1;
    public override Symbol? GetReference(int index)
    {
        if (index < this.DeclarationCount)
            return this.GetDeclaration(index);
        index -= this.DeclarationCount;
        return index == 0 ? this.PropertyType : null;
    }

    internal protected override Symbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new PropertySymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            () => context.Substitute(this.PropertyType),
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
