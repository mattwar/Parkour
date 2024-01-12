namespace Parkour;
using Symbols;

public abstract class Compilation
{
    public abstract ImmutableList<ISourceDocument> Documents { get; }
    public abstract NamespaceSymbol GlobalNamespace { get; }

    /// <summary>
    /// Gets all the diagnositcs for the document.
    /// </summary>
    public virtual void GetDiagnostics(ISourceDocument document, List<Diagnostic> diagnostics)
    {
    }

    /// <summary>
    /// Returns the symbols that are possible to be referenced at the position in the syntax tree.
    /// </summary>
    public virtual void GetSymbolsInScope(ISourceDocument document, int position, List<ISymbol> symbols)
    {
    }
}