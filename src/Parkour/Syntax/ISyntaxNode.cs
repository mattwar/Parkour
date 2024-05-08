namespace Parkour;

/// <summary>
/// Represents a non-terminal <see cref="ISyntaxElement"/> that contains other elements.
/// </summary>
public interface ISyntaxNode : ISyntaxElement
{
    /// <summary>
    /// The number of child <see cref="ISyntaxElement"/> contained by the span of this element.
    /// </summary>
    public int ChildCount { get; }

    /// <summary>
    /// Returns the <see cref="ISyntaxElement"/> at the specified index position.
    /// </summary>
    public ISyntaxElement? GetChild(int index);
}
