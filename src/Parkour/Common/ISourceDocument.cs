namespace Parkour;

/// <summary>
/// Represents the source document of a syntax tree or compilation input.
/// </summary>
public interface ISourceDocument
{
    public string Name { get; }
    public string Text { get; }
}
