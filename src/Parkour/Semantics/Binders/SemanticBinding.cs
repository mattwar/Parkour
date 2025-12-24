namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// The result of binding a set of declarations and an optional expression.
/// </summary>
public class SemanticBinding
{
    /// <summary>
    /// The elements after being bound.
    /// </summary>
    public ImmutableList<SemanticElement> BoundElements { get; }

    /// <summary>
    /// The imported symbols.
    /// </summary>
    public SymbolTable ImportedSymbols { get; }

    /// <summary>
    /// The combined imported and declared symbols.
    /// </summary>
    public SymbolTable CombinedSymbols { get; }

    public SemanticBinding(
        ImmutableList<SemanticElement> boundElements,
        SymbolTable importedSymbols,
        SymbolTable combinedSymbols)
    {
        this.BoundElements = boundElements;
        this.ImportedSymbols = importedSymbols;
        this.CombinedSymbols = combinedSymbols;
    }
}