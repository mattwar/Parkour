using System.Diagnostics.CodeAnalysis;

namespace Parkour.Symbols;

/// <summary>
/// Manages accessing and constructing symbols
/// </summary>
public abstract class SymbolTable
{
    /// <summary>
    /// The root namespace for the symbols declared by the table.
    /// </summary>
    public abstract GlobalNamespaceSymbol GlobalNamespace { get; }

    /// <summary>
    /// Creates a new <see cref="SymbolTable"/> instance.
    /// </summary>
    protected SymbolTable()
    {
    }

    /// <summary>
    /// Creates a new <see cref="SymbolTable"/> with the specified symbols.
    /// </summary>
    public abstract SymbolTable WithSymbols(GlobalNamespaceSymbol symbols);

    /// <summary>
    /// Creates a new <see cref="SymbolTable"/> that includes the additional symbols.
    /// </summary>
    public virtual SymbolTable AddSymbols(GlobalNamespaceSymbol additionalSymbols)
    {
        if (additionalSymbols == this.GlobalNamespace)
            return this;

        var combined = CombinedSymbols.Create([this.GlobalNamespace, additionalSymbols]);
        return WithSymbols(combined);
    }

    #region Getting TypeSymbols from Runtime Types

    /// <summary>
    /// Gets the <see cref="TypeSymbol"/> for the equivalent runtime type.
    /// </summary>
    public virtual bool TryGetTypeSymbol(Type type, [NotNullWhen(true)] out TypeSymbol? typeSymbol)
    {
        if (type.IsArray
            && type.GetElementType() is Type elementType
            && TryGetTypeSymbol(elementType, out var elementTypeSymbol))
        {
            typeSymbol = GetArray(elementTypeSymbol);
            return true;
        }
        else if (type.IsConstructedGenericType
            && TryGetTypeSymbol(type.GetGenericTypeDefinition(), out var definitionSymbol)
            && TryGetTypeSymbols(type.GetGenericArguments(), out var typeArgSymbols))
        {
            typeSymbol = GetConstructed(definitionSymbol, typeArgSymbols);
            return true;
        }
        else if (type.FullName != null
            && TryGetTypeSymbol(type.FullName, out var declaredType))
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
    /// Gets the <see cref="TypeSymbol"/> for the equivalent runtime type.
    /// </summary>
    public TypeSymbol GetTypeSymbol(Type type) =>
       TryGetTypeSymbol(type, out var typeSymbol)
            ? typeSymbol
            : throw new InvalidOperationException($"type {type.FullName ?? type.Name} not found");

    /// <summary>
    /// Gets the type symbols for the list of runtime types.
    /// </summary>
    public bool TryGetTypeSymbols(IEnumerable<Type> types, [NotNullWhen(true)] out ImmutableList<TypeSymbol>? typeSymbols)
    {
        var list = new List<TypeSymbol>();

        foreach (var type in types)
        {
            if (!TryGetTypeSymbol(type, out var typeSymbol))
            {
                typeSymbols = null;
                return false;
            }

            list.Add(typeSymbol);
        }

        typeSymbols = list.ToImmutableList();
        return true;
    }

    /// <summary>
    /// Gets the type symbols for the list of runtime types.
    /// </summary>
    public ImmutableList<TypeSymbol> GetTypeSymbols(IEnumerable<Type> types)
    {
        return types.Select(GetTypeSymbol).ToImmutableList();
    }

    #endregion

    #region Getting Runtime types from Symbols

    /// <summary>
    /// Gets the runtime type for a type symbol
    /// </summary>
    public virtual bool TryGetType(TypeSymbol typeSymbol, [NotNullWhen(true)] out Type? type)
    {
        // quick translation of common known types
        type = 
            typeSymbol == this.Object ? typeof(object)
            : typeSymbol == SpecialSymbols.Null ? typeof(object)
            : typeSymbol == SpecialSymbols.Unknown ? typeof(object)
            : typeSymbol == this.Void ? typeof(void)
            : typeSymbol == SpecialSymbols.DoesNotReturn ? typeof(void)
            : typeSymbol == this.Boolean ? typeof(bool)
            : typeSymbol == this.Byte ? typeof(byte)
            : typeSymbol == this.SByte ? typeof(sbyte)
            : typeSymbol == this.Int16 ? typeof(short)
            : typeSymbol == this.UInt16 ? typeof(ushort)
            : typeSymbol == this.Int32 ? typeof(int)
            : typeSymbol == this.UInt32 ? typeof(uint)
            : typeSymbol == this.Int64 ? typeof(long)
            : typeSymbol == this.UInt64 ? typeof(ulong)
            : typeSymbol == this.Single ? typeof(float)
            : typeSymbol == this.Double ? typeof(double)
            : typeSymbol == this.Decimal ? typeof(Decimal)
            : typeSymbol == this.String ? typeof(string)
            : null;
        return type != null;
    }

    #endregion

    #region Symbols from dotted paths

    /// <summary>
    /// Get the declared symbol given the dotted path to the symbol from the root of the global namespace.
    /// If multiple symbols are found with the same full name, the first is returned.
    /// </summary>
    public abstract bool TryGetSymbol<TSymbol>(
        string dottedPath,
        [NotNullWhen(true)] out TSymbol? symbol)
        where TSymbol : Symbol;

    /// <summary>
    /// Get the declared symbol given the dotted path to the symbol from the root of the global namespace.
    /// If multiple symbols are found with the same full name, the first is returned.
    /// </summary>
    public bool TryGetSymbol(string dottedPath, [NotNullWhen(true)] out Symbol? symbol) =>
        TryGetSymbol<Symbol>(dottedPath, out symbol);

    /// <summary>
    /// Get the declared symbol given the dotted path to the symbol from the root of the global namespace.
    /// If multiple symbols are found with the same full name, the first is returned.
    /// </summary>
    public TSymbol GetSymbol<TSymbol>(string dottedPath)
        where TSymbol : Symbol =>
        TryGetSymbol<TSymbol>(dottedPath, out var symbol)
            ? symbol
            : throw new InvalidOperationException($"symbol {dottedPath} not found");

    /// <summary>
    /// Get the declared symbol using the dotted path from the root of the global namespace.
    /// If multiple symbols are found with the same name, the first is returned.
    /// </summary>
    public Symbol GetSymbol(string dottedPath) =>
        GetSymbol<Symbol>(dottedPath);

    /// <summary>
    /// Gets the declared type given the dotted pa th from the root of the global namespace.
    /// If multiple types with the same name are found, the first is returned.
    /// </summary>
    public TypeSymbol GetTypeSymbol(string dottedPath) =>
        TryGetTypeSymbol(dottedPath, out var type)
            ? type
            : throw new InvalidOperationException($"type {dottedPath} not found");

    /// <summary>
    /// Gets the declared type given the dotted path from the root of the global namespace.
    /// If multiple types with the same full name are found, the first is returned.
    /// </summary>
    public bool TryGetTypeSymbol(string dottedPath, [NotNullWhen(true)] out TypeSymbol? type) =>
        TryGetSymbol(dottedPath, out type);

    #endregion

    #region Constructing Arrays

    /// <summary>
    /// Gets an array of the specified element type and dimensions.
    /// </summary>
    public abstract ArraySymbol GetArray(TypeSymbol elementType, int dimensions = 1);

    #endregion

    #region Constructing Groups

    /// <summary>
    /// Gets the group when multiple distinct symbols (or none) are specified, 
    /// otherwise returns the one distinct symbol.
    /// </summary>
    public abstract Symbol GetGroup(IEnumerable<Symbol> symbols);

    #endregion

    #region Constructing Generic Types and Members

    /// <summary>
    /// Gets or constructs a constructable symbol with the specified type arguments.
    /// </summary>
    public abstract TSymbol GetConstructed<TSymbol>(
        TSymbol constructableSymbol,
        ImmutableList<TypeSymbol> typeArguments)
        where TSymbol : Symbol;

    #endregion

    #region Common Symbols found in symbol tree
    /// <summary>
    /// The System namespace.
    /// </summary>
    public NamespaceSymbol System =>
        _systemNs ??= GetSymbol<NamespaceSymbol>("System")!;

    /// <summary>
    /// Indicates no value is returned.
    /// </summary>
    public TypeSymbol Void => 
        _voidType ??= GetOrCreateCommonType(typeof(void));

    /// <summary>
    /// The System.Type type.
    /// </summary>
    public TypeSymbol Type =>
        _typeType ??= GetOrCreateCommonType(typeof(System.Type));

    /// <summary>
    /// The Syste.Boolean type.
    /// </summary>
    public TypeSymbol Boolean =>
        _booleanType ??= GetOrCreateCommonType(typeof(System.Boolean));

    /// <summary>
    /// The System.Byte type.
    /// </summary>
    public TypeSymbol Byte =>
        _byteType ??= GetOrCreateCommonType(typeof(System.Byte));

    /// <summary>
    /// The System.SByte type.
    /// </summary>
    public TypeSymbol SByte =>
        _sbyteType ??= GetOrCreateCommonType(typeof(System.SByte));

    /// <summary>
    /// The System.Int16 type.
    /// </summary>
    public TypeSymbol Int16 =>
        _int16Type ??= GetOrCreateCommonType(typeof(System.Int16));

    /// <summary>
    /// The System.UInt16 type.
    /// </summary>
    public TypeSymbol UInt16 =>
        _uint16Type ??= GetOrCreateCommonType(typeof(System.UInt16));

    /// <summary>
    /// The System.Int32 type.
    /// </summary>
    public TypeSymbol Int32 =>
        _int32Type ??= GetOrCreateCommonType(typeof(System.Int32));

    /// <summary>
    /// The System.UInt32 type.
    /// </summary>
    public TypeSymbol UInt32 =>
        _uint32Type ??= GetOrCreateCommonType(typeof(System.UInt32));

    /// <summary>
    /// The System.Int64 type.
    /// </summary>
    public TypeSymbol Int64 =>
        _int64Type ??= GetOrCreateCommonType(typeof(System.Int64));

    /// <summary>
    /// The System.UInt64 type.
    /// </summary>
    public TypeSymbol UInt64 =>
        _uint64Type ??= GetOrCreateCommonType(typeof(System.UInt64));

    /// <summary>
    /// The System.Single type.
    /// </summary>
    public TypeSymbol Single =>
        _singleType ??= GetOrCreateCommonType(typeof(System.Single));

    /// <summary>
    /// The System.Double type.
    /// </summary>
    public TypeSymbol Double =>
        _doubleType ??= GetOrCreateCommonType(typeof(System.Double));

    /// <summary>
    /// The System.Decimal type.
    /// </summary>
    public TypeSymbol Decimal =>
        _decimalType ??= GetOrCreateCommonType(typeof(System.Decimal));

    /// <summary>
    /// The System.Char type.
    /// </summary>
    public TypeSymbol Char =>
        _charType ??= GetOrCreateCommonType(typeof(System.Char));

    /// <summary>
    /// The System.String type.
    /// </summary>
    public TypeSymbol String =>
        _stringType ??= GetOrCreateCommonType(typeof(System.String));

    /// <summary>
    /// The System.Object type.
    /// </summary>
    public TypeSymbol Object =>
        _objectType ??= GetOrCreateCommonType(typeof(System.Object));

    /// <summary>
    /// Gets or creates the <see cref="TypeSymbol"/> for the equivalent runtime type.
    /// If the type is not found, a proxy with no members is supplied.
    /// </summary>
    protected TypeSymbol GetOrCreateCommonType(Type type) =>
        TryGetTypeSymbol(type, out var typeSymbol)
            ? typeSymbol
            : new ClassSymbol(type.Name);

    private NamespaceSymbol? _systemNs;
    private TypeSymbol? _typeType;
    private TypeSymbol? _voidType; 
    private TypeSymbol? _booleanType;
    private TypeSymbol? _byteType;
    private TypeSymbol? _sbyteType;
    private TypeSymbol? _int16Type;
    private TypeSymbol? _uint16Type;
    private TypeSymbol? _int32Type;
    private TypeSymbol? _uint32Type;
    private TypeSymbol? _int64Type;
    private TypeSymbol? _uint64Type;
    private TypeSymbol? _singleType;
    private TypeSymbol? _doubleType;
    private TypeSymbol? _decimalType;
    private TypeSymbol? _charType;
    private TypeSymbol? _stringType;
    private TypeSymbol? _objectType;
    #endregion

    #region Operators

    private ImmutableList<OperatorSymbol>? _operators;

    /// <summary>
    /// All the symbols of the known operators
    /// </summary>
    public virtual ImmutableList<OperatorSymbol> Operators
    {
        get
        {
            if (_operators == null)
            {
                var tmp = OperatorSymbols.From(this).Default;
                Interlocked.CompareExchange(ref _operators, tmp, null);
            }

            return _operators;
        }
    }

    private ImmutableDictionary<Operator, ImmutableList<OperatorSymbol>>? _kindToOperatorMap;

    /// <summary>
    /// Gets the operators for the specific operator kind.
    /// </summary>
    public ImmutableList<OperatorSymbol> GetOperators(Operator op)
    {
        if (_kindToOperatorMap == null)
        {
            var tmp = this.Operators
                .GroupBy(op => op.Operator)
                .ToImmutableDictionary(g => g.Key, g => g.ToImmutableList());
            Interlocked.CompareExchange(ref _kindToOperatorMap, tmp, null);
        }

        return _kindToOperatorMap.TryGetValue(op, out var operators)
            ? operators
            : ImmutableList<OperatorSymbol>.Empty;
    }

    #endregion
}
