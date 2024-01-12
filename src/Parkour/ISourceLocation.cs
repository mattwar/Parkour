namespace Parkour;

public interface ISourceLocation
{
    /// <summary>
    /// The source document
    /// </summary>
    public ISourceDocument Document { get; }

    /// <summary>
    /// The starting text position of the source location.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// The length of the source location.
    /// </summary>
    public int Length { get; }
}
