using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Parkour.Symbols;

/// <summary>
/// A class for caching symbols found in the global namespace.
/// </summary>
public class SymbolCache
{
    /// <summary>
    /// The global namespace for all declared and external symbols.
    /// </summary>
    public GlobalNamespaceSymbol GlobalNamespace { get; }

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

    private static readonly ConditionalWeakTable<GlobalNamespaceSymbol, SymbolCache> _namespaceMap =
        new ConditionalWeakTable<GlobalNamespaceSymbol, SymbolCache>();

    private readonly ConditionalWeakTable<ImmutableList<Symbol>, GroupSymbol> _listToGroupMap =
        new ConditionalWeakTable<ImmutableList<Symbol>, GroupSymbol>();

    private SymbolCache(GlobalNamespaceSymbol globalNamespace)
    {
        this.GlobalNamespace = globalNamespace;
    }

    /// <summary>
    /// Gets or creates the <see cref="SymbolCache"/> associated with the <see cref="NamespaceSymbol"/>
    /// </summary>
    public static SymbolCache From(GlobalNamespaceSymbol globalNamespace)
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
    /// Gets or creates the <see cref="TypeSymbol"/> for the equivalent runtime type.
    /// If the type is not found, a proxy with no members is supplied.
    /// </summary>
    private TypeSymbol GetOrCreateType(Type type) =>
        TryGetType(type, out var typeSymbol)
            ? typeSymbol
            : new TypeSymbol(type.Name);

    /// <summary>
    /// Gets the <see cref="TypeSymbol"/> for the equivalent runtime type.
    /// </summary>
    public virtual TypeSymbol GetType(Type type) =>
       TryGetType(type, out var typeSymbol)
            ? typeSymbol
            : throw new InvalidOperationException($"type {type.FullName ?? type.Name} not found");

    /// <summary>
    /// Gets the <see cref="TypeSymbol"/> for the equivalent runtime type.
    /// </summary>
    public virtual bool TryGetType(Type type, [NotNullWhen(true)] out TypeSymbol typeSymbol)
    {
        if (type.IsArray 
            && type.GetElementType() is Type elementType
            && TryGetType(elementType, out var elementTypeSymbol))
        {
            typeSymbol = GetArray(elementTypeSymbol);
            return true;
        }
        else if (type.IsConstructedGenericType
            && TryGetType(type.GetGenericTypeDefinition(), out var definitionSymbol)
            && TryGetTypes(type.GetGenericArguments(), out var typeArgSymbols))
        {
            typeSymbol = GetConstructed(definitionSymbol, typeArgSymbols);
            return true;
        }
        else if (type.FullName != null
            && TryGetType(type.FullName, out var declaredType))
        {
            typeSymbol = declaredType;
            return true;
        }
        else
        {
            typeSymbol = null!;
            return false;
        }
    }

    /// <summary>
    /// Gets the type symbols for the list of runtime types.
    /// </summary>
    public bool TryGetTypes(IReadOnlyList<Type> types, [NotNullWhen(true)] out ImmutableList<TypeSymbol> typeSymbols)
    {
        var list = new List<TypeSymbol>();

        foreach (var type in types)
        {
            if (!TryGetType(type, out var typeSymbol))
            {
                typeSymbols = null!;
                return false;
            }

            list.Add(typeSymbol);
        }

        typeSymbols = list.ToImmutableList();
        return true;
    }

    /// <summary>
    /// Gets the declared type given its full name.
    /// If multiple types with the same name are found, the first is returned.
    /// </summary>
    public TypeSymbol GetType(string dottedName) =>
        TryGetType(dottedName, out var type)
            ? type
            : throw new InvalidOperationException($"type {dottedName} not found");

    /// <summary>
    /// Gets the declared type given the type's full name.
    /// If multiple types with the same full name are found, the first is returned.
    /// </summary>
    public virtual bool TryGetType(string dottedName, [NotNullWhen(true)] out TypeSymbol type) =>
        TryGetSymbol(dottedName, out type);

    /// <summary>
    /// Get the declared symbol given the symbol's full name.
    /// If multiple symbols are found with the same full name, the first is returned.
    /// </summary>
    public virtual TSymbol GetSymbol<TSymbol>(string dottedName)
        where TSymbol : Symbol =>
        TryGetSymbol<TSymbol>(dottedName, out var symbol)
            ? symbol
            : throw new InvalidOperationException($"symbol {dottedName} not found");

    /// <summary>
    /// Get the declared symbol with the full name.
    /// If multiple symbols are found with the same name, the first is returned.
    /// </summary>
    public virtual Symbol GetSymbol(string dottedName) =>
        GetSymbol<Symbol>(dottedName);

    /// <summary>
    /// Get the declared symbol with the full name.
    /// If multiple symbols are found with the same full name, the first is returned.
    /// </summary>
    public virtual bool TryGetSymbol<TSymbol>(string dottedName, [NotNullWhen(true)] out TSymbol symbol)
        where TSymbol : Symbol
    {
        var tmp = this.GlobalNamespace.GetFirstSymbolFromPath<TSymbol>(dottedName);
        if (tmp != null)
        {
            symbol = tmp;
            return true;
        }
        else
        {
            symbol = default!;
            return false;
        }
    }

    private ConditionalWeakTable<TypeSymbol, ArraySymbol> _symbolToArrayMap =
        new ConditionalWeakTable<TypeSymbol, ArraySymbol>();

    /// <summary>
    /// Gets an array of the specified element type.
    /// </summary>
    public virtual ArraySymbol GetArray(TypeSymbol elementType)
    {
        if (!_symbolToArrayMap.TryGetValue(elementType, out var arrayType))
        {
            arrayType = _symbolToArrayMap.GetValue(elementType, _et => new ArraySymbol(_et));
        }

        return arrayType;
    }

    /// <summary>
    /// Gets the group when multiple distinct symbols (or none) are specified, 
    /// otherwise returns the one distinct symbol.
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
    public TSymbol GetConstructed<TSymbol>(
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

#if false
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
#endif
}