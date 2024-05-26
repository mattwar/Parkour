namespace Parkour.Symbols;

public class ConstructorSymbol : MemberSymbol
{
    /// <summary>
    /// The constructor parameters.
    /// </summary>
    public ImmutableList<ParameterSymbol> Parameters =>
        _lazyParameters?.Value ?? ImmutableList<ParameterSymbol>.Empty;
    private readonly Lazy<ImmutableList<ParameterSymbol>>? _lazyParameters;

    /// <summary>
    /// Custom attributes for this constructor
    /// </summary>
    public override ImmutableList<AttributeInfo> Attributes =>
        _lazyAttributes?.Value ?? ImmutableList<AttributeInfo>.Empty;
    private readonly Lazy<ImmutableList<AttributeInfo>>? _lazyAttributes;

    /// <summary>
    /// The type that the constructor constructs.
    /// </summary>
    public TypeSymbol ConstructedType => (TypeSymbol)this.DeclaringSymbol!;

    public ConstructorSymbol(
        TypeSymbol declaringType,
        SymbolAccess access, 
        BitSet<SymbolModifier> modifiers, 
        Func<ConstructorSymbol, ImmutableList<ParameterSymbol>>? fnParameters,
        Func<ConstructorSymbol, ImmutableList<AttributeInfo>>? fnAttributes)
        : base(
            modifiers.Contains(SymbolModifier.Static) ? ".cctor" : ".ctor", 
            declaringType, 
            access, 
            modifiers)
    {
        _lazyParameters = fnParameters != null
            ? new Lazy<ImmutableList<ParameterSymbol>>(() => fnParameters(this))
            : null;
        _lazyAttributes = fnAttributes != null
            ? new Lazy<ImmutableList<AttributeInfo>>(() => fnAttributes(this))
            : null;
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
            this.Parameters.Count > 0 ? me => context.Substitute(this.Parameters, me) : null,
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(context)) : null
            );
    }
}
