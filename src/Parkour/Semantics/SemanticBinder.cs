namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Binds semantic elements (declarations and expressions).
/// </summary>
public abstract class SemanticBinder
{
    /// <summary>
    /// Creates symbols for all declarations and
    /// rewrites expressions to include referenced symbols, result types and diagnostics.
    /// </summary>
    /// <param name="elements">The elements to be bound.</param>
    /// <param name="imports">The imported symbols.</param>
    public abstract SemanticBinding Bind(
        ImmutableList<SemanticElement> elements,
        SymbolTable imports
        );
}