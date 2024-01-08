using System.Reflection;

namespace Parkour.Symbols;
using Analysis;

public class FunctionSymbol : TypeSymbol
{
    public ImmutableList<ParameterSymbol> Parameters { get; }
    public TypeSymbol ReturnType { get; }
    public MethodBase? RuntimeMethod { get; }

    public FunctionSymbol(string name, ImmutableList<ParameterSymbol> parameters, TypeSymbol? returnType, MethodBase? runtimeMethod = null)
        : base(name)
    {
        Parameters = parameters;
        ReturnType = returnType ?? CommonSymbols.Unknown;
        RuntimeMethod = runtimeMethod;
    }

    public FunctionSymbol WithName(string name)
    {
        if (Name == null)
            return this;
        return new FunctionSymbol(name, Parameters, ReturnType, RuntimeMethod);
    }
}
