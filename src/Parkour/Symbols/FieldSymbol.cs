using System.Reflection;

namespace Parkour.Symbols;

public sealed class FieldSymbol : MemberSymbol
{
    public TypeSymbol FieldType { get; }
    public FieldInfo? RuntimeField { get; }

    public FieldSymbol(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, TypeSymbol fieldType, FieldInfo? runtimeField = null)
        : base(name, container, access, modifier)
    {
        FieldType = fieldType;
        RuntimeField = runtimeField;
    }
}
