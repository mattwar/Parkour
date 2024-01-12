using System.Reflection;

namespace Parkour.Symbols;
using Binding;

public sealed class FieldSymbol : MemberSymbol
{
    public TypeSymbol? DeclaringType { get; }
    public override MemberSymbol? Container => this.DeclaringType;
    public override SymbolAccess Access { get; }
    public override SymbolModifier Modifiers { get; }

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

            return _fieldType ?? CommonSymbols.Unknown;
        }
    }

    public FieldInfo? RuntimeField { get; }

    public FieldSymbol(
        string name, 
        TypeSymbol? declaringType, 
        SymbolAccess access, 
        SymbolModifier modifiers, 
        Func<TypeSymbol> fnFieldType, 
        FieldInfo? runtimeField)
        : base(name)
    {
        DeclaringType = declaringType;
        Access = access;
        Modifiers = modifiers;
        _fnFieldType = fnFieldType;
        RuntimeField = runtimeField;
    }

    public FieldSymbol(
        string name,
        TypeSymbol? declaringType,
        SymbolAccess access,
        SymbolModifier modifiers,
        TypeSymbol fieldType,
        FieldInfo? runtimeField)
        : this(name, declaringType, access, modifiers, () => fieldType, runtimeField)
    {
    }
}
