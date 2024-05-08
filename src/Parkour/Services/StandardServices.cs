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

    public virtual Task<ClassificationResult> GetClassificationsAsync(int start, int length, CancellationToken cancellationToken) =>
        Task.FromResult(ClassificationResult.Empty);

    public virtual Task<CompletionResult> GetCompletionsAsync(int position, char? lastKey, CancellationToken cancellationToken) =>
        Task.FromResult(CompletionResult.Empty);

    public virtual Task<DiagnosticResult> GetDiagnosticsAsync(int position, CancellationToken cancellationToken) =>
        Task.FromResult(DiagnosticResult.Empty);

    public virtual Task<HoverTextResult> GetHoverTextAsync(int position, CancellationToken cancellationToken) =>
        Task.FromResult(HoverTextResult.Empty);

    public virtual Task<CodeActionResult> GetActionsAsync(int position, CancellationToken cancellationToken) =>
        Task.FromResult(CodeActionResult.Empty);

    public virtual Task<CodeOperationResult> GetOperationsAsync(ICodeAction action, CancellationToken cancellationToken) =>
        Task.FromResult(CodeOperationResult.Empty);

    public virtual Task<FormattingResult> FormatAsync(int start, int length, CancellationToken cancellationToken) =>
        Task.FromResult(new FormattingResult(this.Document.Text.Substring(start, length)));
}
