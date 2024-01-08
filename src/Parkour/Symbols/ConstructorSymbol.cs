using System.Reflection;

namespace Parkour.Symbols;
using Analysis;

public class ConstructorSymbol : MemberSymbol
{
    public ImmutableList<ParameterSymbol> Parameters { get; }
    public TypeSymbol ReturnType { get; }
    public MethodBase? RuntimeMethod { get; }

    public ConstructorSymbol(Symbol? container, SymbolAccess access, SymbolModifier modifier, ImmutableList<ParameterSymbol> parameters, TypeSymbol? returnType = null, MethodBase? runtimeMethod = null)
        : base("", container, access, modifier)
    {
        Parameters = parameters;
        ReturnType = returnType ?? CommonSymbols.Unknown;
        RuntimeMethod = runtimeMethod;
    }
}
