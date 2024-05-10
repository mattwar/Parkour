namespace Parkour.Services;

public class StandardServices
    : IDocumentServiceFactory,
      IClassificationService,
      ICodeActionService,
      ICompletionService,
      IDiagnosticService,
      IFormattingService,
      IHoverTextService
{
    public ISourceDocument Document { get; }

    public StandardServices(
        ISourceDocument document)
    {
        this.Document = document;
    }

    public TService? GetService<TService>() where TService : class, IDocumentService
    {
        return this as TService;
    }

    public virtual ImmutableList<string> GetClassificationKinds() =>
        ImmutableList<string>.Empty;

    public virtual ClassificationResult GetClassifications(int start, int length, CancellationToken cancellationToken) =>
        ClassificationResult.Empty;

    public virtual CompletionResult GetCompletions(int position, char? lastKey, CancellationToken cancellationToken) =>
        CompletionResult.Empty;

    public virtual DiagnosticResult GetDiagnostics(int start, int length, CancellationToken cancellationToken) =>
        DiagnosticResult.Empty;

    public virtual HoverTextResult GetHoverText(int position, CancellationToken cancellationToken) =>
        HoverTextResult.Empty;

    public virtual CodeActionResult GetActions(int position, CancellationToken cancellationToken) =>
        CodeActionResult.Empty;

    public virtual CodeOperationResult GetOperations(ICodeAction action, CancellationToken cancellationToken) =>
        CodeOperationResult.Empty;

    public virtual FormattingResult Format(int start, int length, CancellationToken cancellationToken) =>
        new FormattingResult(this.Document.Text.Substring(start, length));
}
