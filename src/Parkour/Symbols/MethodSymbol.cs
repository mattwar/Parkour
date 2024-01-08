using System.Reflection;

namespace Parkour.Symbols;

public class MethodSymbol : MemberSymbol
{
    private Func<Symbol, ImmutableList<ParameterSymbol>>? _fnParameters;
    private ImmutableList<ParameterSymbol>? _parameters;

    public ImmutableList<ParameterSymbol> Parameters
    {
        get
        {
            if (_parameters == null && _fnParameters is Func<Symbol, ImmutableList<ParameterSymbol>> fn)
            {
                _fnParameters = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _parameters, tmp, null);
            }

            return _parameters!;
        }
    }

    private Func<TypeSymbol>? _fnReturnType;
    private TypeSymbol? _returnType;

    public TypeSymbol ReturnType
    {
        get
        {
            if (_returnType == null && _fnReturnType is Func<TypeSymbol> fn)
            {
                _fnReturnType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _returnType, tmp, null);
            }

            return _returnType!;
        }
    }

    public MethodBase? RuntimeMethod { get; }

    public MethodSymbol(
        string name, 
        Symbol? container, 
        SymbolAccess access, 
        SymbolModifier modifier, 
        Func<Symbol, ImmutableList<ParameterSymbol>>? fnParameters, 
        Func<TypeSymbol>? fnReturnType, 
        MethodBase? runtimeMethod = null)
        : base(name, container, access, modifier)
    {
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
        RuntimeMethod = runtimeMethod;
    }

    public MethodSymbol(
        string name,
        Symbol? container,
        SymbolAccess access,
        SymbolModifier modifier,
        ImmutableList<ParameterSymbol> parameters,
        TypeSymbol returnType,
        MethodBase? runtimeMethod = null)
        : base(name, container, access, modifier)
    {
        _parameters = parameters;
        _returnType = returnType;
        RuntimeMethod = runtimeMethod;
    }
}
