namespace Parkour.Symbols;

public sealed class ArraySymbol : TypeSymbol
{
    private Func<TypeSymbol>? _fnElementType;
    private TypeSymbol? _elementType;

    public TypeSymbol ElementType
    {
        get
        {
            if (_elementType == null && _fnElementType is { } fn)
            {
                _fnElementType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _elementType, tmp, null);
            }

            return _elementType!;
        }
    }

    public ArraySymbol(Func<TypeSymbol> fnElementType)
        : base($"Array")
    {
        _fnElementType = fnElementType;
        _elementType = null;
    }

    public ArraySymbol(TypeSymbol elementType) 
        : base($"Array")
    {
        _elementType = elementType;
        _fnElementType = null;
    }

    internal protected override ArraySymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new ArraySymbol(
            () => context.Substitute(this.ElementType));
    }
}
