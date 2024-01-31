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

    public ConstructorSymbol(
        Symbol? declaringSymbol,
        SymbolAccess access, 
        SymbolModifier modifiers, 
        Func<ConstructorSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType)
        : base(
            (modifiers & SymbolModifier.Static) != 0 ? ".cctor" : ".ctor", 
            declaringSymbol, 
            access, 
            modifiers)
    {
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
    }

    public ConstructorSymbol(
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<ParameterSymbol> parameters,
        TypeSymbol returnType)
        : this(
              declaringSymbol,
              access,
              modifiers,
              me => parameters,
              () => returnType)
    {
    }

    public override int DeclarationCount =>
        this.Parameters.Count;

    public override Symbol? GetDeclaration(int index) =>
        this.Parameters[index];

    internal protected override ConstructorSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new ConstructorSymbol(
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => context.Substitute(this.Parameters, me),
            () => declaringSymbol as TypeSymbol ?? this.ReturnType);
    }
}
