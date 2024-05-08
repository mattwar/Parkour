namespace Parkour.Services;

public interface IHoverTextService : IDocumentService
{
    /// <summary>
    /// Gets the text that would be displayed in a hover tip.
    /// </summary>
    Task<HoverTextResult> GetHoverTextAsync(int position, CancellationToken cancellationToken);
}

