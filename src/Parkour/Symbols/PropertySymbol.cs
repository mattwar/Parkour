using System.Reflection;

namespace Parkour.Symbols;

public sealed class PropertySymbol : MemberSymbol
{
    public TypeSymbol? DeclaringType { get; }
    public override MemberSymbol? Container => DeclaringType;
    public override SymbolAccess Access { get; }
    public override SymbolModifier Modifiers { get; }

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

    public MethodSymbol GetMethod
    {
        get
        {
            if (_getMethod == null && _fnGetMethod is { } fn)
            {
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _getMethod, tmp, null);
                _fnGetMethod = null;
            }

            return _getMethod!;
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

    public PropertyInfo? RuntimeProperty { get; }

    public PropertySymbol(
        string name,
        TypeSymbol? declaringType,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<TypeSymbol> fnPropertyType,
        Func<PropertySymbol, FieldSymbol>? fnBackingField,
        Func<PropertySymbol, MethodSymbol> fnGetMethod,
        Func<PropertySymbol, MethodSymbol>? fnSetMethod,
        PropertyInfo? runtimeProperty)
        : base(name)
    {
        DeclaringType = declaringType;
        Access = access;
        Modifiers = modifiers;
        _fnBackingField = fnBackingField;
        _fnPropertyType = fnPropertyType;
        _fnGetMethod = fnGetMethod;
        _fnSetMethod = fnSetMethod;
        RuntimeProperty = runtimeProperty;
    }
}
