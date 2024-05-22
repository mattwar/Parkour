namespace Parkour.Symbols;

public sealed class ParameterSymbol : Symbol
{
    public Symbol? DeclaringSymbol { get; }

    /// <summary>
    /// The type of the parameter.
    /// </summary>
    public TypeSymbol Type => _lazyType.Value;
    private readonly Lazy<TypeSymbol> _lazyType;

    public ParameterSymbol(
        string name, 
        Symbol? declaringSymbol,
        Func<TypeSymbol> fnType)
        : base(name)
    {
        DeclaringSymbol = declaringSymbol;
        _lazyType = new Lazy<TypeSymbol>(fnType, SpecialSymbols.CyclicDefinition);
    }

    public ParameterSymbol(
        string name, 
        Symbol? declaringSymbol,
        TypeSymbol type)
        : this(
              name,
              declaringSymbol,
              () => type)
    {
    }

    public override int ReferencedSymbolCount => 1;
    public override Symbol? GetReferencedSymbol(int index) => index == 0 ? this.Type : null;

    internal protected override Symbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new ParameterSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            () => context.Substitute(this.Type));
    }
}
