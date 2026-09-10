namespace Parkour;

/// <summary>
/// Represents a single element (token or node) of the syntax tree.
/// </summary>
public interface ISyntaxElement
{
    /// <summary>
    /// The starting position in the source document of the first character of the first token.
    /// </summary>
    int TextStart { get; }

    /// <summary>
    /// The number of characters from the start of the first token to the end of the last token in the document.
    /// </summary>
    int TextLength { get; }

    /// <summary>
    /// The position after the end of the text
    /// </summary>
    int TextEnd => TextStart + TextLength;
}