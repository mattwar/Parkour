namespace Parkour.Semantics;

using Symbols;

public class SemanticLowering
{
    /// <summary>
    /// The elements after being bound.
    /// </summary>
    public ImmutableList<SemanticElement> LoweredElements { get; }

    /// <summary>
    /// The imported symbols.
    /// </summary>
    public SymbolTable ImportedSymbols { get; }

    /// <summary>
    /// The combined imported and declared symbols.
    /// </summary>
    public SymbolTable CombinedSymbols { get; }

    public SemanticLowering(
        ImmutableList<SemanticElement> elements,
        SymbolTable importedSymbols,
        SymbolTable combinedSymbols)
    {
        this.LoweredElements = elements;
        this.ImportedSymbols = importedSymbols;
        this.CombinedSymbols = combinedSymbols;
    }
}