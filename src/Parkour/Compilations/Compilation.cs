namespace Parkour;

public abstract class Compilation
{
    /// <summary>
    /// The documents that are part of the compilation.
    /// </summary>
    public abstract ImmutableList<ISourceDocument> Documents { get; }

    /// <summary>
    /// Returns the <see cref="ISyntaxTree"/> associated with the document.
    /// </summary>
    public virtual ISyntaxTree? GetSyntaxTree(ISourceDocument document) =>
        null;

    /// <summary>
    /// Gets the semantic information associated with the text position within the document.
    /// </summary>
    public virtual SemanticInfo GetSemanticInfo(ISourceDocument document, int position) =>
        SemanticInfo.None;

    /// <summary>
    /// Gets parsing annotations available at the position of the document.
    /// </summary>
    public virtual ImmutableList<TAnnotation> GetAnnotations<TAnnotation>(
        ISourceDocument document, 
        int position, 
        Func<TAnnotation, bool>? filter = null)
        =>
        ImmutableList<TAnnotation>.Empty;

    /// <summary>
    /// Gets all the diagnositcs for the document.
    /// </summary>
    public virtual ImmutableList<Diagnostic> GetDiagnostics(ISourceDocument document) =>
        ImmutableList<Diagnostic>.Empty;

    /// <summary>
    /// Returns the symbols that are possible to be referenced at the position in the syntax tree.
    /// </summary>
    public virtual ImmutableList<ISymbol> GetSymbolsInScope(
        ISourceDocument document,
        int position)
        =>
        ImmutableList<ISymbol>.Empty;
}
