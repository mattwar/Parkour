namespace Parkour.Services;

public interface IHoverTextDocumentService : IDocumentService
{
    /// <summary>
    /// Gets the text that would be displayed in a hover tip.
    /// </summary>
    HoverTextResult GetHoverText(
        int position,
        Settings options,
        CancellationToken cancellationToken);
}