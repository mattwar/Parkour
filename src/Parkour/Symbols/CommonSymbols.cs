using System.Runtime.CompilerServices;

namespace Parkour.Symbols;

/// <summary>
/// A class for searching the global namespace for known types,
/// storing some of them for quick access.
/// </summary>
public class CommonSymbols
{
    public NamespaceSymbol GlobalNamespace { get; }

    private CommonSymbols(NamespaceSymbol globalNamespace)
    {
        this.GlobalNamespace = globalNamespace;
    }

    private static readonly ConditionalWeakTable<NamespaceSymbol, CommonSymbols> _namespaceMap =
        new ConditionalWeakTable<NamespaceSymbol, CommonSymbols>();

    public static CommonSymbols From(NamespaceSymbol globalNamespace)
    {
        if (!_namespaceMap.TryGetValue(globalNamespace, out var commonSymbols))
        {
            commonSymbols = _namespaceMap.GetValue(globalNamespace, s => new CommonSymbols(s));
        }

        return commonSymbols;
    }

    public static readonly TypeSymbol Unknown = new TypeSymbol("Unknown", typeof(object));
    public static readonly TypeSymbol Null = new TypeSymbol("Null", typeof(object));
    public static readonly TypeSymbol Any = new TypeSymbol("Any", typeof(object));
    public static readonly TypeSymbol Void = new TypeSymbol("Void", typeof(void));

    public NamespaceSymbol? _systemNs;
    public NamespaceSymbol System => _systemNs ??= 
        this.GlobalNamespace.GetFirstSymbolFromPath<NamespaceSymbol>("System")!;

    public TypeSymbol? _typeType;
    public TypeSymbol Type => _typeType ??= GetOrCreateType(typeof(System.Type));

    private TypeSymbol? _booleanType;
    public TypeSymbol Boolean => _booleanType ??= GetOrCreateType(typeof(System.Boolean));

    private TypeSymbol? _byteType;
    public TypeSymbol Byte => _byteType ??= GetOrCreateType(typeof(System.Byte));

    private TypeSymbol? _int16Type;
    public TypeSymbol Int16 => _int16Type ??= GetOrCreateType(typeof(System.Int16));

    private TypeSymbol? _int32Type;
    public TypeSymbol Int32 => _int32Type ??= GetOrCreateType(typeof(System.Int32));

    private TypeSymbol? _int64Type;
    public TypeSymbol Int64 => _int64Type ??= GetOrCreateType(typeof(System.Int64));

    private TypeSymbol? _singleType;
    public TypeSymbol Single => _singleType ??= GetOrCreateType(typeof(System.Single));

    private TypeSymbol? _doubleType;
    public TypeSymbol Double => _doubleType ??= GetOrCreateType(typeof(System.Double));

    private TypeSymbol? _decimalType;
    public TypeSymbol Decimal => _decimalType ??= GetOrCreateType(typeof(System.Decimal));

    private TypeSymbol? _charType;
    public TypeSymbol Char => _charType ??= GetOrCreateType(typeof(System.Char));

    private TypeSymbol? _stringType;
    public TypeSymbol String => _stringType ??= GetOrCreateType(typeof(System.String));

    private TypeSymbol? _objectType;
    public TypeSymbol Object => _objectType ??= GetOrCreateType(typeof(System.Object));

    /// <summary>
    /// Gets the <see cref="TypeSymbol"/> based on equivalent runtime type.
    /// </summary>
    public virtual TypeSymbol? GetType(Type type) =>
       this.GlobalNamespace.GetFirstSymbolFromPath<TypeSymbol>(type.FullName!)!;

    /// <summary>
    /// Gets or creates the <see cref="TypeSymbol"/> associated with the runtime type.
    /// If the type is not found in the global namespace, a proxy with no members is supplied.
    /// </summary>
    private TypeSymbol GetOrCreateType(Type type) =>
        GetType(type) ?? new TypeSymbol(type);

    private readonly ConditionalWeakTable<ImmutableList<Symbol>, GroupSymbol> _listToGroupMap =
        new ConditionalWeakTable<ImmutableList<Symbol>, GroupSymbol>();

    private readonly ConditionalWeakTable<ImmutableList<TypeSymbol>, UnionSymbol> _listToUnionMap =
        new ConditionalWeakTable<ImmutableList<TypeSymbol>, UnionSymbol>();

    /// <summary>
    /// Gets an array of the specified element type.
    /// </summary>
    public virtual ArraySymbol GetArray(TypeSymbol elementType) =>
        new ArraySymbol(elementType);

    /// <summary>
    /// Gets a list of the specified element type.
    /// </summary>
    public virtual ListSymbol GetList(TypeSymbol elementType) =>
        new ListSymbol(elementType);

    /// <summary>
    /// Gets the union or individual type from a list of types.
    /// </summary>
    public virtual TypeSymbol GetUnion(IEnumerable<TypeSymbol> types)
    {
        if (!types.Any())
            return CommonSymbols.Void;

        if (types is IReadOnlyList<TypeSymbol> roTypes
            && roTypes.Count == 1)
        {
            return roTypes[0];
        }

        var immutableTypes = types as ImmutableList<TypeSymbol>;
        if (immutableTypes != null
            && _listToUnionMap.TryGetValue(immutableTypes, out var union))
        {
            return union;
        }

        types = FlattenUnions(types).ToList();

        var hasUnknown = types.Any(t => t == CommonSymbols.Unknown);
        var hasAny = types.Any(t => t == CommonSymbols.Any);
        var hasVoid = types.Any(t => t == CommonSymbols.Void);
        var hasNull = types.Any(t => t == CommonSymbols.Null);

        var canonicalTypes = types
            .Where(t =>
                t == CommonSymbols.Unknown
                || t == CommonSymbols.Any && !hasUnknown
                || t == CommonSymbols.Null && !hasUnknown
                || t == CommonSymbols.Void && !hasUnknown
                || (!hasUnknown || !hasAny))
            .DistinctBy(t => t, TypeEqualityComparer.Instance)
            .OrderBy(t => t.Name)
            .ToImmutableList();

        if (canonicalTypes.Count == 1)
            return canonicalTypes[0];

        union = _listToUnionMap.GetValue(canonicalTypes, _newTypes => new UnionSymbol(_newTypes));

        // also associate union with original list, if it was immutable
        if (immutableTypes != null)
        {
            _listToUnionMap.GetValue(immutableTypes, _ => union);
        }

        return union;

        static IEnumerable<TypeSymbol> FlattenUnions(IEnumerable<TypeSymbol> types)
        {
            foreach (var type in types)
            {
                if (type is UnionSymbol union)
                {
                    foreach (var unionType in union.Types)
                        yield return unionType;
                }

                yield return type;
            }
        }
    }

    /// <summary>
    /// Gets the union or individual type from a list of types.
    /// </summary>
    public virtual TypeSymbol GetUnion(params TypeSymbol[] types) =>
        GetUnion((IEnumerable<TypeSymbol>)types);

    /// <summary>
    /// Gets the group or individual symbol for the specified symbols.
    /// </summary>
    public virtual Symbol GetGroup(IEnumerable<Symbol> symbols)
    {
        if (!symbols.Any())
            return CommonSymbols.Void;

        if (symbols is IReadOnlyList<Symbol> roSymbols
            && roSymbols.Count == 1)
        {
            return roSymbols[0];
        }

        var immutableSymbols = symbols as ImmutableList<Symbol>;
        if (immutableSymbols != null
            && _listToGroupMap.TryGetValue(immutableSymbols, out var group))
        {
            return group;
        }

        var canonicalSymbols = symbols
            .DistinctBy(s => s, SymbolEqualityComparer.Instance)
            .OrderBy(s => s.Name)
            .ToImmutableList();

        if (canonicalSymbols.Count == 1)
            return canonicalSymbols[0];

        group = _listToGroupMap.GetValue(canonicalSymbols, _symbols => new GroupSymbol(_symbols));

        // also associate union with original list, if it was immutable
        if (immutableSymbols != null)
        {
            _listToGroupMap.GetValue(immutableSymbols, _ => group);
        }

        return group;
    }

    /// <summary>
    /// Gets the group or individual symbol for the specified symbols.
    /// </summary>
    public virtual Symbol GetGroup(params Symbol[] symbols) =>
        GetGroup((IEnumerable<Symbol>)symbols);
}