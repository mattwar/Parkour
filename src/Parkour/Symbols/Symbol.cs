namespace Parkour.Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class Symbol
    : ISymbol
{
    private string DebugText => $"{GetType().Name}: {Name}";

    public string Name { get; }

    protected Symbol(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The arity for generic types and methods.
    /// </summary>
    public virtual int Arity => 0;

    /// <summary>
    /// The number of symbols potentially declared by this symbol
    /// </summary>
    public virtual int DeclarationCount => 0;

    /// <summary>
    /// Gets the nth symbol declared by this symbol.
    /// </summary>
    public virtual Symbol? GetDeclaration(int index) => null;

    /// <summary>
    /// The number of potential symbols referenced directly by this system.
    /// </summary>
    public virtual int ReferenceCount => DeclarationCount;

    /// <summary>
    /// Gets the nth symbol referenced or declared by this symbol.
    /// </summary>
    public virtual Symbol? GetReference(int index) => GetDeclaration(index);

    /// <summary>
    /// True if the type can be constructed with generic type arguments.
    /// </summary>
    public virtual bool IsConstructable => false;

    /// <summary>
    /// Constructs a constructable type with the specified generic type arguements.
    /// </summary>
    internal protected virtual Symbol Construct(ConstructionContext context)
       => throw new InvalidOperationException("Cannot construct a non-constructable symbol.");

    /// <summary>
    /// Returns a new symbol with references to any type parameter to references to the corresponding type argument.
    /// </summary>
    internal protected virtual Symbol Substitute(SubstitutionContext context)
        => this;
}

public abstract class ConstructionContext
{
    /// <summary>
    /// The type arguments to construct with.
    /// </summary>
    public abstract ImmutableList<TypeSymbol> TypeArguments { get; }

    /// <summary>
    /// Create a subsitutation context to be used by the new symbol 
    /// to substitute references from the type parameters to the new type arguments.
    /// </summary>
    public abstract SubstitutionContext CreateSubstitution(ImmutableList<TypeParameterSymbol> typeParameters);
}

public abstract class SubstitutionContext
{
    /// <summary>
    /// Substitute this symbol or make substitutions in symbols referenced by this symbol.
    /// </summary>
    public abstract TSymbol Substitute<TSymbol>(TSymbol symbol)
        where TSymbol : Symbol;

    /// <summary>
    /// Substitute these symbols or make substitutions in symbols referenced by them.
    /// </summary>
    public abstract ImmutableList<TSymbol> Substitute<TSymbol>(ImmutableList<TSymbol> symbols)
        where TSymbol : Symbol;
}