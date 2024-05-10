namespace Parkour.Services;

/// <summary>
/// The classification for a specific text range.
/// </summary>
public record struct ClassifiedSpan(
    string Classification,
    int Start,
    int Length);
