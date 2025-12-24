namespace Parkour.Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class Symbol
    : ISymbol
{
    private string DebugText => $"{GetType().Name}: {Name}";

    public string Name { get; }

    public virtual string FullName => Name;

    string ISymbol.Kind 
    {
        get
        {
            var typeName = this.GetType().Name;
            return typeName.EndsWith("Symbol")
                ? typeName.Substring(0, typeName.Length - 6)
                : typeName;
        }
    }

    public virtual ImmutableList<AttributeInfo> Attributes =>
        ImmutableList<AttributeInfo>.Empty;

    protected Symbol(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The arity for generic types and methods.
    /// </summary>
    public virtual int Arity => 0;

    /// <summary>
    /// True if the type can be constructed with generic type arguments.
    /// </summary>
    public virtual bool IsConstructable => false;

    /// <summary>
    /// The number of symbols potentially declared by this symbol
    /// </summary>
    public virtual int DeclaredSymbolCount => 0;

    /// <summary>
    /// Gets the nth symbol declared by this symbol.
    /// </summary>
    public virtual Symbol? GetDeclaredSymbol(int index) => null;

    /// <summary>
    /// The number of potential symbols referenced or declared by this symbol.
    /// </summary>
    public virtual int ReferencedSymbolCount => 
        DeclaredSymbolCount;

    /// <summary>
    /// Gets the nth symbol referenced or declared by this symbol.
    /// </summary>
    public virtual Symbol? GetReferencedSymbol(int index) => 
        GetDeclaredSymbol(index);

    /// <summary>
    /// Constructs a constructable type with the specified generic type arguements.
    /// </summary>
    internal protected virtual Symbol Construct(ConstructionContext context)
       => throw new InvalidOperationException("Cannot construct a non-constructable symbol.");

    /// <summary>
    /// Returns a new symbol with references to any type parameter to references to the corresponding type argument.
    /// </summary>
    internal protected virtual Symbol Substitute(SubstitutionContext context, Symbol? declaringSymbol) =>
        this;
}
