namespace Parkour;

public interface ICompilation
{
    /// <summary>
    /// The documents that are part of the compilation.
    /// </summary>
    ImmutableList<ISourceDocument> Documents { get; }

    /// <summary>
    /// Returns the <see cref="ISyntaxTree"/> associated with the document.
    /// </summary>
    ISyntaxTree? GetSyntaxTree(ISourceDocument document);

    /// <summary>
    /// Returns the <see cref="IGrammarAnnotations"/> associated with the document
    /// at the specified position.
    /// </summary>
    ImmutableList<TAnnotation> GetGrammarAnnotations<TAnnotation>(
        ISourceDocument document,
        int position,
        Func<TAnnotation, bool>? filter = null);

    /// <summary>
    /// Gets all the diagnostics for the document.
    /// </summary>
    ImmutableList<Diagnostic> GetDiagnostics(ISourceDocument document);

    /// <summary>
    /// Gets the semantic information associated with the text position within the document.
    /// </summary>
    SemanticInfo GetSemanticInfo(ISourceDocument document, int position);

    /// <summary>
    /// Returns the symbols that are possible to be referenced at the position in the syntax tree.
    /// </summary>
    ImmutableList<ISymbol> GetSymbolsInScope(ISourceDocument document, int position);
}