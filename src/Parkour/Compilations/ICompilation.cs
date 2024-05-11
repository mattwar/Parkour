namespace Parkour;

using Parsing;
using Semantics;

public interface ICompilation
{
    /// <summary>
    /// The documents that are part of the compilation.
    /// </summary>
    ImmutableList<ISourceDocument> Documents { get; }
}

public interface ISyntaxCompilation : ICompilation
{
    /// <summary>
    /// Returns the <see cref="ISyntaxTree"/> associated with the document.
    /// </summary>
    ISyntaxTree? GetSyntaxTree(ISourceDocument document);

    /// <summary>
    /// Returns the <see cref="IParsingContext"/> associated with the document.
    /// </summary>
    IParsingContext? GetParsingContext(ISourceDocument document);
}

public interface ISemanticCompilation : ICompilation
{
    /// <summary>
    /// Gets all the semantic diagnositcs for the document.
    /// </summary>
    ImmutableList<Diagnostic> GetSemanticDiagnostics(ISourceDocument document);

    /// <summary>
    /// Gets the semantic information associated with the text position within the document.
    /// </summary>
    SemanticInfo GetSemanticInfo(ISourceDocument document, int position);

    /// <summary>
    /// Returns the symbols that are possible to be referenced at the position in the syntax tree.
    /// </summary>
    ImmutableList<ISymbol> GetSymbolsInScope(SourceDocument document, int position);
}

public interface ISemanticElementCompilation : ICompilation
{
    /// <summary>
    /// Gets the root semantic element associated with the document.
    /// </summary>
    SemanticElement? GetSemanticElement(ISourceDocument document);
}