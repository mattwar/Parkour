namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// The result of binding a set of declarations and an optional expression.
/// </summary>
public abstract class SemanticBinding
{
    /// <summary>
    /// The elements after being bound.
    /// </summary>
    public abstract ImmutableList<SemanticElement> Elements { get; }

    /// <summary>
    /// The combined external and declared symbol table.
    /// </summary>
    public abstract SymbolTable Symbols { get; }

    /// <summary>
    /// All diagnostics determined during binding.
    /// </summary>
    public abstract ImmutableList<Diagnostic> Diagnostics { get; }
}
