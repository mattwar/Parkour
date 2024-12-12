namespace Parkour.Services;

public interface IDocumentServiceFactory
{
    bool TryGetDocumentService<TService>(
        [NotNullWhen(true)] out TService? service) 
        where TService : class, IDocumentService;
}