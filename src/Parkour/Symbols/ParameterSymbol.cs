using System.Reflection;

namespace Parkour.Symbols;
using Analysis;

public sealed class ParameterSymbol : Symbol
{
    private Func<TypeSymbol>? _fnParameterType;
    private TypeSymbol? _parameterType;

    public TypeSymbol ParameterType
    {
        get
        {
            if (_parameterType == null && _fnParameterType is Func<TypeSymbol> fn)
            {
                _fnParameterType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _parameterType, tmp, null);
            }

            return _parameterType ?? CommonSymbols.Unknown;
        }
    }

    public ParameterInfo? RuntimeParameter { get; }

    public ParameterSymbol(string name, Func<TypeSymbol> fnParameterType, ParameterInfo? runtimeParameter = null)
        : base(name)
    {
        _fnParameterType = fnParameterType;
    }

    public ParameterSymbol(string name, TypeSymbol parameterType, ParameterInfo? runtimeParameter = null)
        : base(name)
    {
        _parameterType = parameterType;
        RuntimeParameter = runtimeParameter;
    }
}
