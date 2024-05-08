namespace Parkour.Services;

/// <summary>
/// The classification for a specific text range.
/// </summary>
public struct ClassifiedSpan
{
    public string Classification { get; }
    public int Start { get; }
    public int Length { get; }

    public ClassifiedSpan(string classification, int start, int length)
    {
        this.Classification = classification;
        this.Start = start;
        this.Length = length;
    }
}

