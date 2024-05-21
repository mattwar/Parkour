namespace Parkour.Binding;

using Semantics;
using Symbols;

/// <summary>
/// Binds semantic elements (declarations and expressions).
/// </summary>
public abstract class SemanticBinder
{
    /// <summary>
    /// Creates symbols for all declarations,
    /// rewrites expressions to include referenced symbols, result types and diagnostics.
    /// </summary>
    public abstract SemanticBinding Bind(
        ImmutableList<SemanticElement> elements,
        SymbolTable symbols);
}