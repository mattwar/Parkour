using System.Reflection;

namespace Parkour.Symbols;

public class LambdaSymbol : TypeSymbol
{
    private Func<LambdaSymbol, ImmutableList<ParameterSymbol>>? _fnParameters;
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

    public MethodBase? RuntimeMethod { get; }

    public LambdaSymbol(
        string name, 
        Func<LambdaSymbol, ImmutableList<ParameterSymbol>> fnParameters, 
        Func<TypeSymbol> fnReturnType, 
        MethodBase? runtimeMethod)
        : base(name)
    {
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
        RuntimeMethod = runtimeMethod;
    }

    public override int DeclarationCount =>
        this.Parameters.Count;

    public override Symbol? GetDeclaration(int index) =>
        this.Parameters[index];
}
