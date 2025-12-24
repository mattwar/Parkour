namespace Parkour.Symbols;

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
