namespace Parkour.Services;

/// <summary>
/// The style to use for a specific text range.
/// </summary>
public record struct StyledRange(
    string Style,
    int Start,
    int Length);
