namespace Parkour.Diagnostics;

public interface ISourceLocation
{
    /// <summary>
    /// The name of the source (file name or other)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The starting text position of the source location.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// The length of the source location.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// The starting <see cref="Diagnostics.LinePosition"/>
    /// </summary>
    public LinePosition LinePosition { get; }
}
