namespace Parkour.Symbols;

public class TypeSymbol : MemberSymbol
{
    private readonly Func<TypeSymbol>? _fnBaseType;
    private TypeSymbol? _baseType;
    private readonly Func<TypeSymbol, ImmutableList<Symbol>>? _fnMembers;
    private ImmutableList<Symbol>? _members;

    public TypeSymbol? BaseType =>
        _baseType ??= _fnBaseType != null ? _fnBaseType() : null;

    public override ImmutableList<Symbol> Members =>
        _members ??= _fnMembers != null ? _fnMembers(this) : ImmutableList<Symbol>.Empty;

    public Type? RuntimeType { get; }

    public TypeSymbol(
        string name,
        Symbol? container,
        SymbolAccess access,
        SymbolModifier modifier,
        Func<TypeSymbol>? fnBaseType,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        Type? runtimeType = null)
        : base(name, container, access, modifier)
    {
        _fnBaseType = fnBaseType;
        _baseType = null;
        _fnMembers = fnMembers;
        _members = null;
        RuntimeType = runtimeType;
    }

    public TypeSymbol(string name, Type? runtimeType = null)
        : this(name, container: null, SymbolAccess.Public, SymbolModifier.None, fnBaseType: null, fnMembers: null, runtimeType)
    {
    }
}
