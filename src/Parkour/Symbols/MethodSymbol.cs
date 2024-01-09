using System.Reflection;

namespace Parkour.Symbols;

public class MethodSymbol : MemberSymbol
{
    public MemberSymbol? DeclaringSymbol { get; }
    public override MemberSymbol? Container => DeclaringSymbol;
    public override SymbolAccess Access { get; }
    public override SymbolModifier Modifiers { get; }

    private Func<ImmutableList<TypeSymbol>>? _fnTypeParameters;
    private ImmutableList<TypeSymbol>? _typeParameters;

    public ImmutableList<TypeSymbol> TypeParameters
    {
        get
        {
            if (_typeParameters == null && _fnTypeParameters is { } fn)
            {
                _fnTypeParameters = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _typeParameters, tmp, null);
            }

            return _typeParameters ?? ImmutableList<TypeSymbol>.Empty;
        }
    }

    private Func<MethodSymbol>? _fnGenericDefinition;
    private MethodSymbol? _genericDefinition;

    public MethodSymbol? GenericDefinition 
    { 
        get
        {
            if (_genericDefinition == null && _fnGenericDefinition is { } fn)
            {
                _fnGenericDefinition = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _genericDefinition, tmp, null);
            }

            return _genericDefinition;
        }
    }

    public bool IsGeneric => this.TypeParameters.Count > 0;
    public bool IsDefinition => this.GenericDefinition != null;
    public bool IsConcrete => this.IsGeneric && !IsDefinition;

    private Func<MethodSymbol, ImmutableList<ParameterSymbol>>? _fnParameters;
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

            return _parameters!;
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

            return _returnType!;
        }
    }

    public MethodBase? RuntimeMethod { get; }

    public MethodSymbol(
        string name,
        MemberSymbol? declaringSymbol, 
        SymbolAccess access, 
        SymbolModifier modifiers, 
        Func<ImmutableList<TypeSymbol>>? fnTypeParameters,
        Func<MethodSymbol, ImmutableList<ParameterSymbol>>? fnParameters, 
        Func<TypeSymbol>? fnReturnType, 
        Func<MethodSymbol>? fnGenericDefinition,
        MethodBase? runtimeMethod)
        : base(name)
    {
        DeclaringSymbol = declaringSymbol;
        Access = access;
        Modifiers = modifiers;
        _fnTypeParameters = fnTypeParameters;
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
        _fnGenericDefinition = fnGenericDefinition;
        RuntimeMethod = runtimeMethod;
    }

    public MethodSymbol(
        string name,
        MemberSymbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifier,
        ImmutableList<TypeSymbol> typeParameters,
        ImmutableList<ParameterSymbol> parameters,
        TypeSymbol returnType,
        MethodSymbol? genericDefinition,
        MethodBase? runtimeMethod)
        : this(
              name, 
              declaringSymbol, 
              access, 
              modifier, 
              () => typeParameters, 
              me => parameters, 
              () => returnType, 
              genericDefinition != null ? () => genericDefinition : null,
              runtimeMethod)
    {
    }
}
