using System.Reflection;

namespace Parkour.Symbols;

public sealed class Property : MemberSymbol
{
    public TypeSymbol PropertyType { get; }
    public PropertyInfo? RuntimeProperty { get; }

    public Property(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, TypeSymbol propertyType, PropertyInfo? runtimeProperty = null)
        : base(name, container, access, modifier)
    {
        PropertyType = propertyType;
        RuntimeProperty = runtimeProperty;
    }
}
