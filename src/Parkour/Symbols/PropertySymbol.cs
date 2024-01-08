using System.Reflection;

namespace Parkour.Symbols;

public sealed class PropertySymbol : MemberSymbol
{
    private Func<TypeSymbol>? _fnPropertyType;
    private TypeSymbol? _propertyType;

    public TypeSymbol PropertyType 
    { 
        get
        {
            if (_propertyType == null && _fnPropertyType is Func<TypeSymbol> fn)
            {
                _fnPropertyType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _propertyType, tmp, null);
            }

            return _propertyType!;
        }
    }

    private Func<Symbol, MethodSymbol>? _fnGetMethod;
    private MethodSymbol? _getMethod;

    public MethodSymbol GetMethod
    {
        get
        {
            if (_getMethod == null && _fnGetMethod is Func<Symbol, MethodSymbol> fn)
            {
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _getMethod, tmp, null);
                _fnGetMethod = null;
            }

            return _getMethod!;
        }
    }

    private Func<Symbol, MethodSymbol?>? _fnSetMethod;
    private MethodSymbol? _setMethod;

    public MethodSymbol? SetMethod
    {
        get
        {
            if (_setMethod == null && _fnSetMethod is Func<Symbol, MethodSymbol?> fn)
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
        Symbol? container, 
        SymbolAccess access, 
        SymbolModifier modifiers, 
        Func<TypeSymbol> fnPropertyType, 
        Func<Symbol, MethodSymbol> fnGetMethod,
        Func<Symbol, MethodSymbol?> fnSetMethod,
        PropertyInfo? runtimeProperty = null)
        : base(name, container, access, modifiers)
    {
        _fnPropertyType = fnPropertyType;
        _fnGetMethod = fnGetMethod;
        _fnSetMethod = fnSetMethod;
        RuntimeProperty = runtimeProperty;
    }

    public PropertySymbol(
        string name,
        Symbol? container,
        SymbolAccess access,
        SymbolModifier modifiers,
        TypeSymbol propertyType,
        PropertyInfo? runtimeProperty = null)
        : base(name, container, access, modifiers)
    {
        _propertyType = propertyType;
        RuntimeProperty = runtimeProperty;
    }
}
