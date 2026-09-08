namespace Parkour.Services;

public interface IDiagnosticCompilationService : ICompilationService
{
    /// <summary>
    /// Gets the list of diagnostics for all documents in the compilation
    /// </summary>
    DiagnosticResult GetDiagnostics(
        Settings options,
        CancellationToken cancellationToken);
}
