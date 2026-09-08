using Parkour.Text;

namespace Parkour.Services;

/// <summary>
/// The style applied to a specific range of text.
/// </summary>
public record struct StyledTextRange(
    TextStyle Style,
    int Start,
    int Length)
{
    /// <summary>
    /// The starting text position of the styled range.
    /// </summary>
    public int Start { get; } = Start;

    /// <summary>
    /// The length of the styled range in characters.
    /// </summary>
    public int Length { get; } = Length;

    /// <summary>
    /// The ending text position of the styled range (non-inclusive).
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// True if this text range overlaps the other text range.
    /// </summary>
    public bool Overlaps(TextRange other)
    {
        return TextRange.Overlaps(this.Start, this.Length, other.Start, other.Length);
    }

    public static implicit operator TextRange(StyledTextRange range) =>
        new TextRange(range.Start, range.Length);
}
