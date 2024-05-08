namespace Parkour.Services;

public interface IDocumentServiceFactory
{
    TService? GetService<TService>() where TService : class, IDocumentService;
}