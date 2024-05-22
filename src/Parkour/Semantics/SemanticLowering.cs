namespace Parkour.Semantics;

using Symbols;

public class SemanticLowering : SemanticBinding
{
    public SemanticLowering(
        ImmutableList<SemanticElement> elements,
        SymbolTable importedSymbols,
        SymbolTable combinedSymbols)
        : base(
            elements,
            importedSymbols,
            combinedSymbols)
    {
    }
}