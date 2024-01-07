using System.Reflection;

namespace Parkour.Symbols;
using Analysis;

public sealed class ParameterSymbol : Symbol
{
    private readonly Func<TypeSymbol>? _fnParameterType;
    public ParameterInfo? RuntimeParameter { get; }

    private TypeSymbol? _parameterType;
    public TypeSymbol ParameterType
    {
        get
        {
            if (_parameterType == null)
            {
                _parameterType = _fnParameterType != null ? _fnParameterType() : SymbolModel.Unknown;
            }

            return _parameterType;
        }
    }

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
