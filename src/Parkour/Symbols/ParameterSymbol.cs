namespace Parkour.Symbols;

public sealed class ParameterSymbol : Symbol
{
    public Symbol? DeclaringSymbol { get; }

    private Func<TypeSymbol>? _fnParameterType;
    private TypeSymbol? _parameterType;

    public TypeSymbol ParameterType
    {
        get
        {
            if (_parameterType == null && _fnParameterType is { } fn)
            {
                _fnParameterType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _parameterType, tmp, null);
            }

            return _parameterType ?? SpecialSymbols.Unknown;
        }
    }

    public ParameterSymbol(
        string name, 
        Symbol? declaringSymbol,
        Func<TypeSymbol> fnParameterType)
        : base(name)
    {
        DeclaringSymbol = declaringSymbol;
        _fnParameterType = fnParameterType;
    }

    public ParameterSymbol(
        string name, 
        Symbol? declaringSymbol,
        TypeSymbol parameterType)
        : this(
              name,
              declaringSymbol,
              () => parameterType)
    {
    }

    public override int ReferenceCount => 1;
    public override Symbol? GetReference(int index) => index == 0 ? this.ParameterType : null;

    internal protected override Symbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new ParameterSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            () => context.Substitute(this.ParameterType));
    }
}
