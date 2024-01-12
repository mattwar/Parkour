namespace Parkour;

/// <summary>
/// A <see cref="ISyntaxElement"/> that represents a token (terminal node) of the syntax tree.
/// </summary>
public interface ISyntaxToken : ISyntaxElement
{
    /// <summary>
    /// The text of the token.
    /// </summary>
    public string Text { get; }
}