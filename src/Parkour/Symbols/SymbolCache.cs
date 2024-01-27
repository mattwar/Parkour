using System.Runtime.CompilerServices;

namespace Parkour.Symbols;

/// <summary>
/// A class for caching symbols found in the global namespace.
/// </summary>
public class SymbolCache
{
    public NamespaceSymbol GlobalNamespace { get; }

    public NamespaceSymbol? _systemNs;
    public TypeSymbol? _typeType;
    private TypeSymbol? _booleanType;
    private TypeSymbol? _byteType;
    private TypeSymbol? _int16Type;
    private TypeSymbol? _int32Type;
    private TypeSymbol? _int64Type;
    private TypeSymbol? _singleType;
    private TypeSymbol? _doubleType;
    private TypeSymbol? _decimalType;
    private TypeSymbol? _charType;
    private TypeSymbol? _stringType;
    private TypeSymbol? _objectType;

    private static readonly ConditionalWeakTable<NamespaceSymbol, SymbolCache> _namespaceMap =
        new ConditionalWeakTable<NamespaceSymbol, SymbolCache>();

    private readonly ConditionalWeakTable<ImmutableList<Symbol>, GroupSymbol> _listToGroupMap =
        new ConditionalWeakTable<ImmutableList<Symbol>, GroupSymbol>();

    private readonly ConditionalWeakTable<ImmutableList<TypeSymbol>, UnionSymbol> _listToUnionMap =
        new ConditionalWeakTable<ImmutableList<TypeSymbol>, UnionSymbol>();

    private SymbolCache(NamespaceSymbol globalNamespace)
    {
        this.GlobalNamespace = globalNamespace;
    }

    /// <summary>
    /// Gets or creates the <see cref="SymbolCache"/> associated with the <see cref="NamespaceSymbol"/>
    /// </summary>
    public static SymbolCache From(NamespaceSymbol globalNamespace)
    {
        if (!_namespaceMap.TryGetValue(globalNamespace, out var commonSymbols))
        {
            commonSymbols = _namespaceMap.GetValue(globalNamespace, s => new SymbolCache(s));
        }

        return commonSymbols;
    }

    /// <summary>
    /// The type is not yet known.
    /// </summary>
    public TypeSymbol Unknown => SpecialSymbols.Unknown;

    /// <summary>
    /// The type of a namespace symbol.
    /// </summary>
    public TypeSymbol Namespace => SpecialSymbols.Namespace;

    /// <summary>
    /// The type of a null literal that has not infered another type.
    /// </summary>
    public TypeSymbol Null => SpecialSymbols.Null;

    /// <summary>
    /// The any type is used for parameters to indicate it can receive and type.
    /// </summary>
    public TypeSymbol Any => SpecialSymbols.Any;

    /// <summary>
    /// Indicates no value is returned.
    /// </summary>
    public TypeSymbol Void => SpecialSymbols.Void;

    /// <summary>
    /// Inidicates the expression does not return.
    /// Used for branches and throw expressions.
    /// </summary>
    public TypeSymbol DoesNotReturn => SpecialSymbols.DoesNotReturn;

    /// <summary>
    /// The System namespace.
    /// </summary>
    public NamespaceSymbol System => 
        _systemNs ??= GetSymbol<NamespaceSymbol>("System")!;

    /// <summary>
    /// The <see cref="System.Type"/> type.
    /// </summary>
    public TypeSymbol Type => 
        _typeType ??= GetOrCreateType(typeof(System.Type));

    /// <summary>
    /// The <see cref="System.Boolean"/> type.
    /// </summary>
    public TypeSymbol Boolean => 
        _booleanType ??= GetOrCreateType(typeof(System.Boolean));

    /// <summary>
    /// The <see cref="System.Byte"/> type.
    /// </summary>
    public TypeSymbol Byte => 
        _byteType ??= GetOrCreateType(typeof(System.Byte));

    /// <summary>
    /// The <see cref="System.Int16"/> type.
    /// </summary>
    public TypeSymbol Int16 => 
        _int16Type ??= GetOrCreateType(typeof(System.Int16));

    /// <summary>
    /// The <see cref="System.Int32"/> type.
    /// </summary>
    public TypeSymbol Int32 => 
        _int32Type ??= GetOrCreateType(typeof(System.Int32));

    /// <summary>
    /// The <see cref="System.Int64"/> type.
    /// </summary>
    public TypeSymbol Int64 => 
        _int64Type ??= GetOrCreateType(typeof(System.Int64));

    /// <summary>
    /// The <see cref="System.Single"/> type.
    /// </summary>
    public TypeSymbol Single => 
        _singleType ??= GetOrCreateType(typeof(System.Single));

    /// <summary>
    /// The <see cref="System.Double"/> type.
    /// </summary>
    public TypeSymbol Double => 
        _doubleType ??= GetOrCreateType(typeof(System.Double));

    /// <summary>
    /// The <see cref="System.Decimal"/> type.
    /// </summary>
    public TypeSymbol Decimal => 
        _decimalType ??= GetOrCreateType(typeof(System.Decimal));

    /// <summary>
    /// The <see cref="System.Char"/> type.
    /// </summary>
    public TypeSymbol Char => 
        _charType ??= GetOrCreateType(typeof(System.Char));

    /// <summary>
    /// The <see cref="System.String"/> type.
    /// </summary>
    public TypeSymbol String => 
        _stringType ??= GetOrCreateType(typeof(System.String));

    /// <summary>
    /// The <see cref="System.Object"/> type.
    /// </summary>
    public TypeSymbol Object => 
        _objectType ??= GetOrCreateType(typeof(System.Object));

    /// <summary>
    /// Gets the <see cref="TypeSymbol"/> based on equivalent runtime type.
    /// </summary>
    public virtual TypeSymbol? GetType(Type type) =>
       GetType(type.FullName!)!;

    /// <summary>
    /// Gets the type given the dotted path name: "System.String". 
    /// If multiple types with the same name are found, the first is returned.
    /// </summary>
    public virtual TypeSymbol? GetType(string dottedName) =>
        GetSymbol<TypeSymbol>(dottedName) as TypeSymbol;

    /// <summary>
    /// Get the symbol associated with the dotted path name: "Namespace.MyType.Method"
    /// If multiple symbols are found with the same name, the first is returned.
    /// </summary>
    public virtual TSymbol? GetSymbol<TSymbol>(string dottedName)
        where TSymbol : Symbol =>
        this.GlobalNamespace.GetFirstSymbolFromPath<TSymbol>(dottedName);

    /// <summary>
    /// Get the symbol associated with the dotted path name: "Namespace.MyType.Method"
    /// If multiple symbols are found with the same name, the first is returned.
    /// </summary>
    public virtual Symbol? GetSymbol(string dottedName) =>
        GetSymbol<Symbol>(dottedName);

    /// <summary>
    /// Gets all the symbols associated with the dotted path name.
    /// </summary>
    public virtual void GetSymbols(string dottedName, List<Symbol> symbols) =>
        this.GlobalNamespace.GetSymbolsFromPath(dottedName, symbols);

    /// <summary>
    /// Gets or creates the <see cref="TypeSymbol"/> associated with the runtime type.
    /// If the type is not found in the global namespace, a proxy with no members is supplied.
    /// </summary>
    private TypeSymbol GetOrCreateType(Type type) =>
        GetType(type) ?? new TypeSymbol(type);

    /// <summary>
    /// Gets an array of the specified element type.
    /// </summary>
    public virtual ArraySymbol GetArray(TypeSymbol elementType) =>
        new ArraySymbol(elementType);

    /// <summary>
    /// Gets the union or individual type from a list of types.
    /// </summary>
    public virtual TypeSymbol GetUnion(IEnumerable<TypeSymbol> types)
    {
        if (!types.Any())
            return Void;

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

        var hasUnknown = types.Any(t => t == Unknown);
        var hasAny = types.Any(t => t == Any);
        var hasVoid = types.Any(t => t == Void);
        var hasNull = types.Any(t => t == Null);

        var canonicalTypes = types
            .Where(t =>
                t == Unknown
                || t == Any && !hasUnknown
                || t == Null && !hasUnknown
                || t == Void && !hasUnknown
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
            return Void;

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

    /// <summary>
    /// Gets or constructs a constructable symbol with the specified type arguments.
    /// </summary>
    public TSymbol GetOrConstruct<TSymbol>(
        TSymbol constructableSymbol, 
        ImmutableList<TypeSymbol> typeArguments)
        where TSymbol : Symbol
    {
        Symbol? constructedSymbol = null;

        if (!constructableSymbol.IsConstructable)
            throw new InvalidOperationException("Cannot construct non-constructable symbol.");

        if (!_constructedSymbolMap.TryGetValue(constructableSymbol, out var constructedSymbolInfo))
        {
            constructedSymbolInfo = _constructedSymbolMap.GetOrCreateValue(constructableSymbol);
        }

        if (!constructedSymbolInfo.TypeArgumentsToConstructedSymbolMap.TryGetValue(typeArguments, out constructedSymbol))
        {
            var context = new ConsContext(typeArguments);           
            var tmp = constructableSymbol.Construct(context);

            constructedSymbol = ImmutableInterlocked.GetOrAdd(
                ref constructedSymbolInfo.TypeArgumentsToConstructedSymbolMap,
                typeArguments,
                tmp);
        }

        return (TSymbol)constructedSymbol;
    }

    private class ConstructedSymbolInfo
    {
        public ImmutableDictionary<ImmutableList<TypeSymbol>, Symbol> TypeArgumentsToConstructedSymbolMap =
            ImmutableDictionary<ImmutableList<TypeSymbol>, Symbol>.Empty
            .WithComparers(TypeListEqualityComparer.Instance);
    }

    private ConditionalWeakTable<Symbol, ConstructedSymbolInfo> _constructedSymbolMap =
        new ConditionalWeakTable<Symbol, ConstructedSymbolInfo>();

    private class ConsContext : ConstructionContext
    {
        public override ImmutableList<TypeSymbol> TypeArguments { get; }

        public ConsContext(ImmutableList<TypeSymbol> typeArguments)
        {
            this.TypeArguments = typeArguments;
        }

        public override SubstitutionContext CreateSubstitution(ImmutableList<TypeParameterSymbol> typeParameters)
        {
            return new SubContext(typeParameters, this.TypeArguments);
        }
    }

    private class SubContext : SubstitutionContext
    {
        private readonly ImmutableList<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableList<TypeSymbol> _typeArguments;
        private Dictionary<Symbol, Symbol> _substitutions;

        public SubContext(
            ImmutableList<TypeParameterSymbol> typeParameters, 
            ImmutableList<TypeSymbol> typeArguments)
        {
            if (typeParameters.Count != typeArguments.Count)
                throw new ArgumentException("The number of type parameters does not match the number of type arguments.");

            _typeParameters = typeParameters;
            _typeArguments = typeArguments;
            _substitutions = new Dictionary<Symbol, Symbol>();
        }

        public override TSymbol Substitute<TSymbol>(TSymbol symbol, Symbol? declaringSymbol)
        {
            if (!_substitutions.TryGetValue(symbol, out var sub))
            {
                if (symbol is TypeParameterSymbol tp)
                {
                    var index = _typeParameters.IndexOf(tp);
                    if (index >= 0)
                    {
                        sub = _typeArguments[index];
                    }
                    else
                    {
                        sub = symbol;
                    }
                }
                else 
                {
                    sub = symbol.Substitute(this, declaringSymbol);
                    _substitutions.Add(symbol, sub);
                }
            }

            return (TSymbol)sub;
        }

        public override ImmutableList<TSymbol> Substitute<TSymbol>(ImmutableList<TSymbol> symbols, Symbol? declaringSymbol)
        {
            List<TSymbol>? newList = null;

            for (int i = 0; i < symbols.Count; i++)
            {
                var symbol = symbols[i];
                var sub = (TSymbol)symbol.Substitute(this, declaringSymbol);
                if (sub != symbol || newList != null)
                {
                    if (newList == null)
                    {
                        newList = [..symbols.Take(i)];
                    }

                    newList.Add(sub);
                }
            }

            return newList != null ? newList.ToImmutableList() : symbols;
        }
    }
}