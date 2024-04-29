namespace Parkour.Symbols;

public sealed class IndexerSymbol : MemberSymbol
{
    private Func<TypeSymbol>? _fnElementType;
    private TypeSymbol? _elementType;

    public TypeSymbol ElementType
    {
        get
        {
            if (_elementType == null && _fnElementType is { } fn)
            {
                _fnElementType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _elementType, tmp, null);
            }

            return _elementType!;
        }
    }

    private Func<IndexerSymbol, MethodSymbol>? _fnGetMethod;
    private MethodSymbol? _getMethod;

    public MethodSymbol? GetMethod
    {
        get
        {
            if (_getMethod == null && _fnGetMethod is { } fn)
            {
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _getMethod, tmp, null);
                _fnGetMethod = null;
            }

            return _getMethod;
        }
    }

    private Func<IndexerSymbol, MethodSymbol>? _fnSetMethod;
    private MethodSymbol? _setMethod;

    public MethodSymbol? SetMethod
    {
        get
        {
            if (_setMethod == null && _fnSetMethod is { } fn)
            {
                _fnSetMethod = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _setMethod, tmp, null);
            }

            return _setMethod;
        }
    }

    public IndexerSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<TypeSymbol> fnElementType,
        Func<IndexerSymbol, MethodSymbol>? fnGetMethod,
        Func<IndexerSymbol, MethodSymbol>? fnSetMethod)
        : base(name, declaringSymbol, access, modifiers)
    {
        _fnElementType = fnElementType;
        _fnGetMethod = fnGetMethod;
        _fnSetMethod = fnSetMethod;
    }

    public IndexerSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        TypeSymbol elementType,
        MethodSymbol? getMethod,
        MethodSymbol? setMethod)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            () => elementType,
            getMethod != null ? me => getMethod : null,
            setMethod != null ? me => setMethod : null)
    {
    }

    public override int DeclarationCount => 2;
    public override Symbol? GetDeclaration(int index) =>
        index switch
        {
            0 => GetMethod,
            1 => SetMethod,
            _ => null
        };

    internal protected override Symbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new IndexerSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            () => context.Substitute(this.ElementType),
            this.GetMethod != null
                ? me => context.Substitute(this.GetMethod)
                : null,
            this.SetMethod != null
                ? me => context.Substitute(this.SetMethod)
                : null);
    }
}
