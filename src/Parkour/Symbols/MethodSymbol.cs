using System.Reflection;

namespace Parkour.Symbols;
using Analysis;

public class MethodSymbol : MemberSymbol
{
    public ImmutableList<ParameterSymbol> Parameters { get; }
    public TypeSymbol ReturnType { get; }
    public MethodBase? RuntimeMethod { get; }

    public MethodSymbol(string name, Symbol? container, SymbolAccess access, SymbolModifier modifier, ImmutableList<ParameterSymbol> parameters, TypeSymbol? returnType = null, MethodBase? runtimeMethod = null)
        : base(name, container, access, modifier)
    {
        Parameters = parameters;
        ReturnType = returnType ?? SymbolModel.Unknown;
        RuntimeMethod = runtimeMethod;
    }
}
