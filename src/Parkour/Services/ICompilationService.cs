namespace Parkour.Services;

/// <summary>
/// A compilation-wide service
/// </summary>
public interface ICompilationService
{
    public ICompilation Compilation { get; }
}
