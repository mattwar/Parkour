using System.Reflection;

namespace Parkour.Symbols;

public class ConstructorSymbol : MemberSymbol
{
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


    public ConstructorInfo? RuntimeInfo { get; }

    public ConstructorSymbol(
        Symbol? declaringSymbol,
        SymbolAccess access, 
        SymbolModifier modifiers, 
        Func<ConstructorSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType,
        ConstructorInfo? runtimeInfo)
        : base("", declaringSymbol, access, modifiers)
    {
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
        RuntimeInfo = runtimeInfo;
    }

    public ConstructorSymbol(
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<ParameterSymbol> parameters,
        TypeSymbol returnType,
        ConstructorInfo? runtimeInfo)
        : this(
              declaringSymbol,
              access,
              modifiers,
              me => parameters,
              () => returnType,
              runtimeInfo)
    {
    }

    public override int DeclarationCount =>
        this.Parameters.Count;

    public override Symbol? GetDeclaration(int index) =>
        this.Parameters[index];

    internal protected override ConstructorSymbol Substitute(SubstitutionContext context)
    {
        return new ConstructorSymbol(
            this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => context.Substitute(this.Parameters),
            () => context.Substitute(this.ReturnType),
            null);
    }
}
