using Parkour.Text;

namespace Parkour.Services;

/// <summary>
/// The classification for a specific text range.
/// </summary>
public record struct ClassifiedTextRange(
    Classification Classification,
    TextRange Range)
{
    /// <summary>
    /// The kind of classification applied to the text range.
    /// </summary>
    public Classification ClassificationKind { get; } = Classification;

    public TextRange Range { get; } = Range;

    public ClassifiedTextRange(Classification classification, int start, int length)
        : this(classification, new TextRange(start, length))
    {
    }
}
