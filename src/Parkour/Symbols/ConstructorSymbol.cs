namespace Parkour.Symbols;

public class ConstructorSymbol : MemberSymbol
{
    /// <summary>
    /// The constructor parameters.
    /// </summary>
    public ImmutableList<ParameterSymbol> Parameters => _lazyParameters.Value;
    private readonly Lazy<ImmutableList<ParameterSymbol>> _lazyParameters;

    /// <summary>
    /// The type that the constructor constructs.
    /// </summary>
    public TypeSymbol ConstructedType => (TypeSymbol)this.DeclaringSymbol!;

    public ConstructorSymbol(
        TypeSymbol declaringType,
        SymbolAccess access, 
        BitSet<SymbolModifier> modifiers, 
        Func<ConstructorSymbol, ImmutableList<ParameterSymbol>> fnParameters)
        : base(
            modifiers.Contains(SymbolModifier.Static) ? ".cctor" : ".ctor", 
            declaringType, 
            access, 
            modifiers)
    {
        _lazyParameters = new Lazy<ImmutableList<ParameterSymbol>>(() => fnParameters(this));
    }

    public override int DeclaredSymbolCount =>
        this.Parameters.Count;

    public override Symbol? GetDeclaredSymbol(int index) =>
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
