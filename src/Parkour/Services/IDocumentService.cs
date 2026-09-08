namespace Parkour.Services;

/// <summary>
/// A document specific service.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// The <see cref="ISourceDocument"/> associated with the service."/>
    /// </summary>
    public ISourceDocument Document { get; }
}