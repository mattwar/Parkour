namespace Parkour.Services;

public interface IHoverTextService : IDocumentService
{
    /// <summary>
    /// Gets the text that would be displayed in a hover tip.
    /// </summary>
    HoverTextResult GetHoverText(int position, CancellationToken cancellationToken);
}