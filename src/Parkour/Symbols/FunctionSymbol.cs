namespace Parkour.Symbols;

public class FunctionSymbol : TypeSymbol
{
    private Func<FunctionSymbol, ImmutableList<ParameterSymbol>>? _fnParameters;
    private ImmutableList<ParameterSymbol>? _parameters;

    public ImmutableList<ParameterSymbol> Parameters
    {
        get
        {
            if (_parameters == null && _fnParameters is { } fn)
            {
                _fnParameters = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _parameters, tmp, null);
            }

            return _parameters ?? ImmutableList<ParameterSymbol>.Empty;
        }
    }

    private Func<TypeSymbol>? _fnReturnType;
    private TypeSymbol? _returnType;

    public TypeSymbol ReturnType
    {
        get
        {
            if (_returnType == null && _fnReturnType is { } fn)
            {
                _fnReturnType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _returnType, tmp, null);
            }

            return _returnType ?? SpecialSymbols.Unknown;
        }
    }

    public FunctionSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<FunctionSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType)
        : base(
            name,
            declaringSymbol,
            access,
            modifiers)
    {
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
    }

    public FunctionSymbol(
        string name,
        Symbol? declaringSymbol,
        Func<FunctionSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType)
        : base(
            name,
            declaringSymbol,
            SymbolAccess.Public,
            SymbolModifier.None)
    {
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
    }

    public override int DeclarationCount =>
        this.Parameters.Count;

    public override Symbol? GetDeclaration(int index) =>
        this.Parameters[index];
}
