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

    public TypeSymbol ConstructedType => (TypeSymbol)this.DeclaringSymbol!;

    public ConstructorSymbol(
        TypeSymbol declaringType,
        SymbolAccess access, 
        SymbolModifier modifiers, 
        Func<ConstructorSymbol, ImmutableList<ParameterSymbol>> fnParameters)
        : base(
            (modifiers & SymbolModifier.Static) != 0 ? ".cctor" : ".ctor", 
            declaringType, 
            access, 
            modifiers)
    {
        _fnParameters = fnParameters;
    }

    public ConstructorSymbol(
        TypeSymbol declaringType,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<ParameterSymbol> parameters,
        TypeSymbol returnType)
        : this(
              declaringType,
              access,
              modifiers,
              me => parameters)
    {
    }

    public override int DeclarationCount =>
        this.Parameters.Count;

    public override Symbol? GetDeclaration(int index) =>
        this.Parameters[index];

    internal protected override ConstructorSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new ConstructorSymbol(
            declaringSymbol as TypeSymbol ?? this.ConstructedType,
            this.Access,
            this.Modifiers,
            me => context.Substitute(this.Parameters, me));
    }
}
