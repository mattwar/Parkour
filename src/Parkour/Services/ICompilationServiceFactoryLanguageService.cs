namespace Parkour.Services;

public interface ICompilationServiceFactoryLanguageService : ILanguageService
{
    /// <summary>
    /// Gets the factory for compilation-level services for the specified compilation.
    /// </summary>
    public ICompilationServiceFactory GetCompilationServiceFactory(ICompilation compilation);
}
