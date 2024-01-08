using System.Reflection;

namespace Parkour.Symbols;
using Analysis;

public sealed class FieldSymbol : MemberSymbol
{
    private Func<TypeSymbol>? _fnFieldType;
    private TypeSymbol? _fieldType;

    public TypeSymbol FieldType 
    { 
        get
        {
            if (_fieldType == null && _fnFieldType is Func<TypeSymbol> fn)
            {
                _fnFieldType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _fieldType, tmp, null);
            }

            return _fieldType ?? CommonSymbols.Unknown;
        }
    }

    public FieldInfo? RuntimeField { get; }

    public FieldSymbol(
        string name, 
        Symbol? container, 
        SymbolAccess access, 
        SymbolModifier modifier, 
        Func<TypeSymbol> fnFieldType, 
        FieldInfo? runtimeField = null)
        : base(name, container, access, modifier)
    {
        _fnFieldType = fnFieldType;
        RuntimeField = runtimeField;
    }

    public FieldSymbol(
        string name,
        Symbol? container,
        SymbolAccess access,
        SymbolModifier modifier,
        TypeSymbol fieldType,
        FieldInfo? runtimeField = null)
        : base(name, container, access, modifier)
    {
        _fieldType = fieldType;
        RuntimeField = runtimeField;
    }
}
