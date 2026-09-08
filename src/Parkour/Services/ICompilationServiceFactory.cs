namespace Parkour.Services;

/// <summary>
/// A factory for accessing compilation-wide services.
/// </summary>
public interface ICompilationServiceFactory
{
    /// <summary>
    /// Gets the corresponding compilation-wide service.
    /// </summary>
    public bool TryGetComilationService<TService>(
        [NotNullWhen(true)] out TService? service
        ) where TService : class, ICompilationService;
}