using System.Reflection;

namespace Parkour.Symbols;

public class ConstructorSymbol : MemberSymbol
{
    public TypeSymbol? DeclaringType { get; }
    public override MemberSymbol? Container => this.DeclaringType;
    public override SymbolAccess Access { get; }
    public override SymbolModifier Modifiers { get; }
    public TypeSymbol ReturnType => this.DeclaringType ?? SpecialSymbols.Unknown;
    public MethodBase? RuntimeMethod { get; }

    private Func<ConstructorSymbol, ImmutableList<ParameterSymbol>>? _fnParameters;
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

    public ConstructorSymbol(
        TypeSymbol? declaringType,
        SymbolAccess access, 
        SymbolModifier modifiers, 
        Func<ConstructorSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        MethodBase? runtimeMethod)
        : base("")
    {
        DeclaringType = declaringType;
        Access = access;
        Modifiers = modifiers;
        _fnParameters = fnParameters;
        RuntimeMethod = runtimeMethod;
    }
}
