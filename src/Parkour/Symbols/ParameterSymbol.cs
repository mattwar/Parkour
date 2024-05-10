namespace Parkour.Symbols;

public sealed class ParameterSymbol : Symbol
{
    public Symbol? DeclaringSymbol { get; }

    private Func<TypeSymbol>? _fnType;
    private TypeSymbol? _type;

    public TypeSymbol Type
    {
        get
        {
            if (_type == null && _fnType is { } fn)
            {
                _fnType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _type, tmp, null);
            }

            return _type ?? SpecialSymbols.Unknown;
        }
    }

    public ParameterSymbol(
        string name, 
        Symbol? declaringSymbol,
        Func<TypeSymbol> fnType)
        : base(name)
    {
        DeclaringSymbol = declaringSymbol;
        _fnType = fnType;
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
