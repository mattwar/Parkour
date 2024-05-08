namespace Parkour.Services;

public interface IDiagnosticService : IDocumentService
{
    /// <summary>
    /// Gets the list of diagnostics that overlap the text position.
    /// </summary>
    Task<DiagnosticResult> GetDiagnosticsAsync(int position, CancellationToken cancellationToken);
}
