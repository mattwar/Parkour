namespace Parkour.Services;

/// <summary>
/// Represents a piece of text and the styles applied to specific ranges of the text.
/// The sytles and ranges may be overlapping.
/// </summary>
public record StyledText(string Text, ImmutableList<StyledTextRange> Styles)
{
    /// <summary>
    /// The text that is styled.
    /// </summary>
    public string Text { get; } = Text;

    /// <summary>
    /// A collection of styles applied to ranges of the text.
    /// </summary>
    public ImmutableList<StyledTextRange> Styles { get; } = Styles;

    public static implicit operator StyledText(string text) =>
        new StyledText(text, ImmutableList<StyledTextRange>.Empty);
}