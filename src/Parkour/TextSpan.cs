namespace Parkour;

public readonly struct TextSpan
{
    /// <summary>
    /// The start position within the text.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// The number of characters in the text span.
    /// </summary>
    public int Length { get; }

    public TextSpan(int start, int length)
    {
        this.Start = start;
        this.Length = length;
    }
}