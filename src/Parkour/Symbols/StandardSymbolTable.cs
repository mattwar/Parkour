using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Parkour.Symbols;

/// <summary>
/// Manages accessing and caching symbols
/// </summary>
public class StandardSymbolTable : SymbolTable
{
    /// <summary>
    /// The root namespace for the symbols declared by the table.
    /// </summary>
    public override GlobalNamespaceSymbol GlobalNamespace { get; }

    /// <summary>
    /// Creates a new <see cref="SymbolTable"/> instance.
    /// </summary>
    public StandardSymbolTable(GlobalNamespaceSymbol globalNamespace)
    {
        this.GlobalNamespace = globalNamespace;
    }

    public override SymbolTable WithSymbols(GlobalNamespaceSymbol symbols)
    {
        return new StandardSymbolTable(symbols);
    }

    /// <summary>
    /// Get the declared symbol with the full name.
    /// If multiple symbols are found with the same full name, the first is returned.
    /// </summary>
    public override bool TryGetSymbol<TSymbol>(
        string dottedPath,
        [NotNullWhen(true)] out TSymbol symbol)
    {
        var tmp = this.GetFirstSymbolFromPath<TSymbol>(dottedPath);
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

    /// <summary>
    /// Gets the symbol corresponding the symbol's full dotted name. (ie System.Int32)
    /// Returns the first symbol if more than one symbol with the same name and type exists.
    /// Returns null if no symbols with the name and type exists.
    /// </summary>
    protected virtual TSymbol? GetFirstSymbolFromPath<TSymbol>(
        string dottedPath)
        where TSymbol : Symbol
    {
        var symbols = _symbolListPool.AllocateFromPool();
        try
        {
            GetSymbolsFromPath(dottedPath, symbols);
            return symbols.OfType<TSymbol>().FirstOrDefault();
        }
        finally
        {
            _symbolListPool.ReturnToPool(symbols);
        }
    }

    /// <summary>
    /// Gets the symbol corresponding the symbol's full dotted name. (ie System.Int32)
    /// Returns the first symbol if more than one symbol with the same name exists.
    /// Returns null if no symbols with the name exists.
    /// </summary>
    protected Symbol? GetFirstSymbolFromPath(string dottedPath) =>
        GetFirstSymbolFromPath<Symbol>(dottedPath);

    private static readonly char[] _namePathSplitChars = new[] { '.', '+', '`' };

    /// <summary>
    /// Gets all the symbols that can be reached from the global namespace using the dotted path.
    /// Typically this returns 1 or 0, but may return more if there are multiple symbols with the same name.
    /// </summary>
    protected virtual void GetSymbolsFromPath(string dottedPath, List<Symbol> symbols)
    {
        var containers = _symbolListPool.AllocateFromPool();
        var results = _symbolListPool.AllocateFromPool();
        try
        {
            // containers start as just the global namespace, but may include more if multiple same named symbols exist
            containers.Add(this.GlobalNamespace);
            var nameStart = 0;

            while (nameStart < dottedPath.Length)
            {
                int nameEnd;
                int nameLength;

                var nextSplit = dottedPath.IndexOfAny(_namePathSplitChars, nameStart);

                // if quoted name ignore split character
                if (dottedPath[nameStart] == '[')
                {
                    nameStart++;
                    var nextClose = dottedPath.IndexOf(']', nameStart);
                    if (nextClose > nameStart)
                    {
                        nameLength = nextClose - nameStart;
                        nameEnd = nextClose + 1;
                        nextSplit = nameEnd;
                    }
                    else
                    {
                        nameLength = nextSplit - nameStart;
                        nameEnd = nextSplit;
                    }
                }
                else
                {
                    nameEnd = nextSplit > nameStart ? nextSplit : dottedPath.Length;
                    nameLength = nameEnd - nameStart;
                }

                // check for arity following name
                var arity = 0;
                if (nameEnd < dottedPath.Length && dottedPath[nameEnd] == '`')
                {
                    var arityEnd = nameEnd + 1;
                    while (arityEnd < dottedPath.Length && char.IsDigit(dottedPath[arityEnd]))
                    {
                        arityEnd++;
                    }

                    var aritySpan = dottedPath.AsSpan().Slice(nameEnd + 1, arityEnd - (nameEnd + 1));
                    int.TryParse(aritySpan, out arity);

                    nameEnd = arityEnd;
                }

                results.Clear();
                GetSymbolsInContainers(dottedPath, nameStart, nameLength, containers, results);
                RemoveArityMismatch(results, arity);
                containers.Clear();
                containers.AddRange(results);

                nameStart = nameEnd;

                if (nameEnd < dottedPath.Length
                    && _namePathSplitChars.Contains(dottedPath[nameEnd]))
                {
                    nameStart++;
                }
            }

            // we might get here if dotted path ends in a dot (so just return last set of results)
            symbols.AddRange(results);
        }
        finally
        {
            _symbolListPool.ReturnToPool(containers);
            _symbolListPool.ReturnToPool(results);
        }

        static void GetSymbolsInContainers(string dottedName, int start, int length, List<Symbol> containers, List<Symbol> result)
        {
            foreach (var container in containers)
            {
                if (container is ContainerSymbol nsOrType)

                    // find all items with matching name from all containers
                    nsOrType.GetMembers(dottedName, start, length, result);
            }
        }

        static void RemoveArityMismatch(List<Symbol> symbols, int arity)
        {
            for (int i = symbols.Count - 1; i >= 0; i--)
            {
                if (symbols[i].Arity != arity)
                    symbols.RemoveAt(i);
            }
        }
    }

    private static readonly ObjectPool<List<Symbol>> _symbolListPool =
        new ObjectPool<List<Symbol>>(() => new List<Symbol>(), list => list.Clear());

    /// <summary>
    /// Gets an array of the specified element type.
    /// </summary>
    public override ArraySymbol GetArray(TypeSymbol elementType, int dimensions = 1)
    {
        if (!_elementTypeToArrayInfoMap.TryGetValue(elementType, out var arrayInfo))
        {
            arrayInfo = _elementTypeToArrayInfoMap.GetValue(elementType, _ => new ArrayInfo());
        }

        if (dimensions == 1)
        {
            if (arrayInfo.szArray == null)
            {
                var tmp = CreateArraySymbol(elementType, 1);
                Interlocked.CompareExchange(ref arrayInfo.szArray, tmp, null);
            }

            return arrayInfo.szArray;
        }
        else
        {
            if (!arrayInfo.dimensionsToArrayMap.TryGetValue(dimensions, out var mdArray))
            {
                var tmp = CreateArraySymbol(elementType, dimensions);
                mdArray = ImmutableInterlocked.GetOrAdd(ref arrayInfo.dimensionsToArrayMap, dimensions, tmp);
            }

            return mdArray;
        }
    }

    /// <summary>
    /// Cache for SZ arrays
    /// </summary>
    private ConditionalWeakTable<TypeSymbol, ArrayInfo> _elementTypeToArrayInfoMap =
        new ConditionalWeakTable<TypeSymbol, ArrayInfo>();

    private class ArrayInfo
    {
        public ArraySymbol? szArray;

        public ImmutableDictionary<int, ArraySymbol> dimensionsToArrayMap =
            ImmutableDictionary<int, ArraySymbol>.Empty;
    }

    protected virtual ArraySymbol CreateArraySymbol(TypeSymbol elementType, int dimensions)
    {
        if (dimensions == 1)
        {
            return new ArraySymbol(
                GetSymbol("System"),
                fnElementType: () => elementType,
                dimensions: 1,
                isSZArray: true,
                fnBaseTypes: () => [
                    GetTypeSymbol("System.Array"),
                GetTypeSymbol("System.Collections.IEnumerable"),
                GetTypeSymbol("System.Collections.IList"),
                GetConstructed(GetTypeSymbol("System.Collections.Generic.IEnumerable`1"), [elementType]),
                GetConstructed(GetTypeSymbol("System.Collections.Generic.IList`1"), [elementType]),
                GetConstructed(GetTypeSymbol("System.Collections.Generic.IReadOnlyList`1"), [elementType])
                    ],
                fnMembers: me => [], // TODO: add array members
                constructedFrom: null
                );
        }
        else
        {
            return new ArraySymbol(
                GetSymbol("System"),
                fnElementType: () => elementType,
                dimensions: dimensions,
                isSZArray: false,
                fnBaseTypes: () => [
                    GetTypeSymbol("System.Array"),
                    ],
                fnMembers: me => [], // TODO: add array members
                constructedFrom: null
                );
        }
    }

    /// <summary>
    /// Cache for groups
    /// </summary>
    private readonly ConditionalWeakTable<ImmutableList<Symbol>, GroupSymbol> _listToGroupMap =
        new ConditionalWeakTable<ImmutableList<Symbol>, GroupSymbol>();

    /// <summary>
    /// Gets the group when multiple distinct symbols (or none) are specified, 
    /// otherwise returns the one distinct symbol.
    /// </summary>
    public override Symbol GetGroup(IEnumerable<Symbol> symbols)
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
    public Symbol GetGroup(params Symbol[] symbols) =>
        GetGroup((IEnumerable<Symbol>)symbols);

    /// <summary>
    /// Gets or constructs a constructable symbol with the specified type arguments.
    /// </summary>
    public override TSymbol GetConstructed<TSymbol>(
        TSymbol constructableSymbol,
        ImmutableList<TypeSymbol> typeArguments)
    {
        if (!constructableSymbol.IsConstructable)
            throw new InvalidOperationException("Cannot construct non-constructable symbol.");

        if (!_constructedSymbolMap.TryGetValue(constructableSymbol, out var constructedSymbolInfo))
        {
            constructedSymbolInfo = _constructedSymbolMap.GetOrCreateValue(constructableSymbol);
        }

        if (!constructedSymbolInfo.TypeArgumentsToConstructedSymbolMap.TryGetValue(typeArguments, out var constructedSymbol))
        {
            var context = new ConsContext(this, typeArguments);
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
        public SymbolTable Cache { get; }
        public override ImmutableList<TypeSymbol> TypeArguments { get; }

        public ConsContext(SymbolTable cache, ImmutableList<TypeSymbol> typeArguments)
        {
            this.Cache = cache;
            this.TypeArguments = typeArguments;
        }

        public override SubstitutionContext CreateSubstitution(ImmutableList<TypeParameterSymbol> typeParameters)
        {
            return new SubContext(this.Cache, typeParameters, this.TypeArguments);
        }
    }

    private class SubContext : SubstitutionContext
    {
        public SymbolTable Cache { get; }

        private readonly ImmutableList<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableList<TypeSymbol> _typeArguments;
        private Dictionary<Symbol, Symbol> _substitutions;

        public SubContext(
            SymbolTable cache,
            ImmutableList<TypeParameterSymbol> typeParameters,
            ImmutableList<TypeSymbol> typeArguments)
        {
            if (typeParameters.Count != typeArguments.Count)
                throw new ArgumentException("The number of type parameters does not match the number of type arguments.");

            this.Cache = cache;
            _typeParameters = typeParameters;
            _typeArguments = typeArguments;
            _substitutions = new Dictionary<Symbol, Symbol>();
        }

        public override TSymbol Substitute<TSymbol>(TSymbol symbol, Symbol? declaringSymbol)
        {
            if (_substitutions.TryGetValue(symbol, out var sub))
                return (TSymbol)sub;

            if (symbol is TypeParameterSymbol tp)
            {
                var index = _typeParameters.IndexOf(tp);
                if (index >= 0)
                {
                    return (TSymbol)(Symbol)_typeArguments[index];
                }
                else
                {
                    return symbol;
                }
            }
            else if (CanSubstitute(symbol))
            {
                sub = symbol.Substitute(this, declaringSymbol);
                _substitutions.Add(symbol, sub);
                return (TSymbol)sub;
            }
            else
            {
                return symbol;
            }
        }

        public override ImmutableList<TSymbol> Substitute<TSymbol>(ImmutableList<TSymbol> symbols, Symbol? declaringSymbol)
        {
            List<TSymbol>? newList = null;

            for (int i = 0; i < symbols.Count; i++)
            {
                var symbol = symbols[i];
                var sub = Substitute(symbol, declaringSymbol);
                if (sub != symbol || newList != null)
                {
                    if (newList == null)
                    {
                        newList = [.. symbols.Take(i)];
                    }

                    newList.Add(sub);
                }
            }

            return newList != null ? newList.ToImmutableList() : symbols;
        }

        /// <summary>
        /// Returns true if substitution is possible for this symbol.
        /// </summary>
        public bool CanSubstitute(Symbol symbol)
        {
            // member must be generic or declaring ancestor must be generic
            // otherwise there would be no type parameters to have refered to.

            if (symbol.Arity > 0)
                return true;

            if (symbol is NamespaceSymbol)
                return false;

            if (symbol is MemberSymbol ms && ms.DeclaringSymbol != null)
                return CanSubstitute(ms.DeclaringSymbol);

            if (symbol is ParameterSymbol ps && ps.DeclaringSymbol != null)
                return CanSubstitute(ps.DeclaringSymbol);

            return false;
        }
    }

    /// <summary>
    /// Returns the name without the arity value.
    /// </summary>
    protected static string StripArity(string name) =>
        StripArity(name, out _);

    /// <summary>
    /// Returns the name without the arity and arity value.
    /// </summary>
    protected static string StripArity(string name, out int arity)
    {
        var arityStart = name.IndexOf('`');
        if (arityStart > 0)
        {
            int.TryParse(name.Substring(arityStart + 1), out arity);
            return name.Substring(0, arityStart);
        }

        arity = 0;
        return name;
    }
}