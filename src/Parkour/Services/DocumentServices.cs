namespace Parkour.Services;

public class DocumentServices
    : IDocumentServiceFactory,
      IClassificationService,
      ICodeActionService,
      ICompletionService,
      IDiagnosticService,
      IFormattingService,
      IHoverTextService
{
    public ISourceDocument Document { get; }

    public DocumentServices(
        ISourceDocument document)
    {
        this.Document = document;
    }

    public bool TryGetDocumentService<TService>(
        [NotNullWhen(true)] out TService? service)
        where TService : class, IDocumentService
    {
        service = this as TService;
        return service != null;
    }

    /// <summary>
    /// Returns all classification kinds possible for the document.
    /// </summary>
    public virtual ImmutableList<string> GetClassificationKinds() =>
        ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the classifications for the text elements in the specified range.
    /// </summary>
    public virtual ClassificationResult GetClassifications(
        int start, 
        int length, 
        ServiceOptions options,
        CancellationToken cancellationToken) 
        =>
        ClassificationResult.Empty;

    /// <summary>
    /// Gets the completions at the position in the document.
    /// </summary>
    public virtual CompletionResult GetCompletions(
        int position, 
        char? lastKey, 
        ServiceOptions options,
        CancellationToken cancellationToken) 
        =>
        CompletionResult.Empty;

    /// <summary>
    /// Gets the diagnostics overlapping with the specified text range.
    /// </summary>
    public virtual DiagnosticResult GetDiagnostics(
        int start, 
        int length, 
        ServiceOptions options,
        CancellationToken cancellationToken) =>
        DiagnosticResult.Empty;

    /// <summary>
    /// Gets the text to show in a hovering tool tip for the specified text position.
    /// </summary>
    public virtual HoverTextResult GetHoverText(
        int position, 
        ServiceOptions options,
        CancellationToken cancellationToken) 
        =>
        HoverTextResult.Empty;

    /// <summary>
    /// Gets available code actions at the specified text position.
    /// </summary>
    public virtual CodeActionResult GetActions(
        int position, 
        ServiceOptions options,
        CancellationToken cancellationToken) 
        =>
        CodeActionResult.Empty;

    /// <summary>
    /// Gets the operations needed to apply the code action.
    /// </summary>
    public virtual CodeOperationResult GetOperations(
        ICodeAction action, 
        ServiceOptions options,
        CancellationToken cancellationToken) 
        =>
        CodeOperationResult.Empty;

    /// <summary>
    /// Applies formatting rules to the text range specified.
    /// </summary>
    public virtual FormattingResult Format(
        int start, 
        int length, 
        ServiceOptions options,
        CancellationToken cancellationToken) 
        =>
        new FormattingResult(this.Document.Text.Substring(start, length));
}
