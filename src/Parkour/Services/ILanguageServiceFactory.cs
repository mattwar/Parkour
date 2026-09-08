namespace Parkour.Services;

public interface ILanguageServiceFactory
{
    public bool TryGetLanguageService<TService>(
        [NotNullWhen(true)] out TService? service)
        where TService : class, ILanguageService;
}
