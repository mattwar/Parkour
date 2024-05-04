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
    /// Gets the <see cref="TypeSymbol"/> for the equivalent runtime type.
    /// </summary>
    public override bool TryGetType(Type type, [NotNullWhen(true)] out TypeSymbol typeSymbol)
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
    /// Get the declared symbol with the full name.
    /// If multiple symbols are found with the same full name, the first is returned.
    /// </summary>
    public override bool TryGetSymbol<TSymbol>(string dottedName, [NotNullWhen(true)] out TSymbol symbol)
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

    /// <summary>
    /// Gets an array of the specified element type.
    /// </summary>
    public override ArraySymbol GetArray(TypeSymbol elementType)
    {
        if (!_symbolToArrayMap.TryGetValue(elementType, out var arrayType))
        {
            arrayType = _symbolToArrayMap.GetValue(elementType, CreateArraySymbol);
        }

        return arrayType;
    }

    /// <summary>
    /// Cache for arrays
    /// </summary>
    private ConditionalWeakTable<TypeSymbol, ArraySymbol> _symbolToArrayMap =
        new ConditionalWeakTable<TypeSymbol, ArraySymbol>();

    protected virtual ArraySymbol CreateArraySymbol(TypeSymbol elementType)
    {
        return new ArraySymbol(
            GetSymbol("System"),
            fnElementType: () => elementType,
            dimensions: 1,
            isSZArray: true,
            fnBaseTypes: () => [
                GetType("System.Array"),
                GetType("System.Collections.IEnumerable"),
                GetType("System.Collections.IList"),
                GetConstructed(GetType("System.Collections.Generic.IEnumerable`1"), [elementType]),
                GetConstructed(GetType("System.Collections.Generic.IList`1"), [elementType]),
                GetConstructed(GetType("System.Collections.Generic.IReadOnlyList`1"), [elementType])
                ],
            fnMembers: me => [], // TODO: add array members
            constructedFrom: null
            );
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
}