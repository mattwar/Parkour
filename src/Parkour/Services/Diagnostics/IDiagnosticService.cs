namespace Parkour.Services;

public interface IDiagnosticService : IDocumentService
{
    /// <summary>
    /// Gets the list of diagnostics that overlap the text range.
    /// </summary>
    DiagnosticResult GetDiagnostics(int start, int length, CancellationToken cancellationToken);
}
