namespace Parkour.Services;

public interface IDocumentServiceFactory
{
    /// <summary>
    /// Gets a document service of the specified service type.
    /// </summary>
    bool TryGetDocumentService<TService>(
        [NotNullWhen(true)] out TService? service) 
        where TService : class, IDocumentService;
}