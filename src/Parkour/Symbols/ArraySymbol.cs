namespace Parkour.Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public sealed class ArraySymbol : TypeSymbol
{
    private string DebugText => $"{GetType().Name}: {ElementType.FullName}[]";

    private Func<TypeSymbol>? _fnElementType;
    private TypeSymbol? _elementType;
    private readonly int _dimensions;

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

    public int Dimensions => IsSZArray ? 1 : _dimensions;

    /// <summary>
    /// True if the array is single dimension with lower bound of zero.
    /// </summary>
    public bool IsSZArray => _dimensions == 0;

    public override bool IsArray => true;

    public ArraySymbol(Func<TypeSymbol> fnElementType, int dimensions = 1, bool isSzArray = true)
        : base("Array")
    {
        _fnElementType = fnElementType;
        _elementType = null;
        _dimensions = isSzArray ? 0 : dimensions;
    }

    public ArraySymbol(TypeSymbol elementType, int dimensions = 1, bool isSzArray = true) 
        : base("Array")
    {
        _elementType = elementType;
        _fnElementType = null;
        _dimensions = isSzArray ? 0 : dimensions;
    }

    internal protected override ArraySymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new ArraySymbol(
            () => context.Substitute(this.ElementType),
            _dimensions);
    }
}
