using System.Reflection;

namespace Parkour.Symbols;
using Binding;

public sealed class ParameterSymbol : Symbol
{
    public Symbol? DeclaringSymbol { get; }

    private Func<TypeSymbol>? _fnParameterType;
    private TypeSymbol? _parameterType;

    public TypeSymbol ParameterType
    {
        get
        {
            if (_parameterType == null && _fnParameterType is { } fn)
            {
                _fnParameterType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _parameterType, tmp, null);
            }

            return _parameterType ?? SpecialSymbols.Unknown;
        }
    }

    public ParameterInfo? RuntimeParameter { get; }

    public ParameterSymbol(
        string name, 
        Symbol? declaringSymbol,
        Func<TypeSymbol> fnParameterType, 
        ParameterInfo? runtimeParameter)
        : base(name)
    {
        DeclaringSymbol = declaringSymbol;
        _fnParameterType = fnParameterType;
        RuntimeParameter = runtimeParameter;
    }

    public ParameterSymbol(
        string name, 
        Symbol? declaringSymbol,
        TypeSymbol parameterType, 
        ParameterInfo? runtimeParameter)
        : this(
              name,
              declaringSymbol,
              () => parameterType,
              runtimeParameter)
    {
    }
}
