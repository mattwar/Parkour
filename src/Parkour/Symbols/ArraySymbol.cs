namespace Parkour.Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public sealed class ArraySymbol : TypeSymbol
{
    private string DebugText => $"{GetType().Name}: {ElementType.FullName}[]";

    /// <summary>
    /// The element type of the array.
    /// </summary>
    public TypeSymbol ElementType => _lazyElementType.Value;
    private readonly Lazy<TypeSymbol> _lazyElementType;

    /// <summary>
    /// The dimensions (rank) of the array.
    /// </summary>
    public int Dimensions => IsSZArray ? 1 : _dimensions;
    private readonly int _dimensions;

    /// <summary>
    /// True if the array is single dimension with lower bound of zero.
    /// </summary>
    public bool IsSZArray => _dimensions == 0;

    public override bool IsArray => true;

    public ArraySymbol(
        Symbol? declaringSymbol,
        Func<TypeSymbol> fnElementType,
        int dimensions,
        bool isSZArray,
        Func<ImmutableList<TypeSymbol>>? fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        TypeSymbol? constructedFrom)
        : base(
            "Array", 
            declaringSymbol, 
            SymbolAccess.Public,
            SymbolModifier.None,
            fnTypeParameters: null, 
            fnTypeArguments: null, 
            fnBaseTypes, 
            fnMembers, 
            fnAttributes: null,
            constructedFrom)
    {
        _lazyElementType = new Lazy<TypeSymbol>(fnElementType, SpecialSymbols.CyclicDefinition);
        _dimensions = isSZArray ? 0 : dimensions;
    }

    public override int ReferencedSymbolCount =>
        this.DeclaredSymbolCount + 1;

    public override Symbol? GetReferencedSymbol(int index)
    {
        if (index < this.DeclaredSymbolCount)
            return this.GetDeclaredSymbol(index);

        index -= this.DeclaredSymbolCount;

        if (index == 0)
            return this.ElementType;

        return null;
    }

    internal protected override ArraySymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new ArraySymbol(
            declaringSymbol ?? this.DeclaringSymbol,
            () => context.Substitute(this.ElementType),
            this.Dimensions,
            this.IsSZArray,
            () => context.Substitute(this.BaseTypes),
            me => context.Substitute(this.Members),
            this.ConstructedFrom);
    }

    protected override string GetFullName()
    {
        var elementName = this.ElementType.FullName;
        if (this.Dimensions == 1)
            return $"{elementName}[]";
        return $"{elementName}[{new String(',', this.Dimensions - 1)}]";
    }
}
