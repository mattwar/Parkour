namespace Parkour.Services;

public interface IDiagnosticDocumentService : IDocumentService
{
    /// <summary>
    /// Gets the list of diagnostics for the entire document.
    /// </summary>
    DiagnosticResult GetDiagnostics(
        Settings options,
        CancellationToken token);

    /// <summary>
    /// Gets the list of diagnostics that overlap the text range in the document.
    /// </summary>
    DiagnosticResult GetDiagnostics(
        int start, 
        int length, 
        Settings options,
        CancellationToken cancellationToken);
}
