namespace Parkour;

public abstract class Compilation
{
    public abstract ImmutableList<ISourceDocument> Documents { get; }

    public virtual ISyntaxTree? GetSyntaxTree(ISourceDocument document)
    {
        return null;
    }

    /// <summary>
    /// Gets gammar annotations available at the position in the document.
    /// </summary>
    public virtual void GetAnnotations<TAnnotation>(
        ISourceDocument document, 
        int position, 
        Func<TAnnotation, bool>? filter,
        List<TAnnotation> annotations)
    {
    }

    /// <summary>
    /// Gets all the diagnositcs for the document.
    /// </summary>
    public virtual void GetDiagnostics(
        ISourceDocument document, 
        List<Diagnostic> diagnostics)
    {
    }

    /// <summary>
    /// Returns the symbols that are possible to be referenced at the position in the syntax tree.
    /// </summary>
    public virtual void GetSymbolsInScope(
        ISourceDocument document, 
        int position, 
        List<ISymbol> symbols)
    {
    }
}