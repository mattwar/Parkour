namespace Parkour.Services;

using Parkour.Text;

/// <summary>
/// A aggregate of common services available for a document.
/// </summary>
public class DocumentServices
    : IDocumentServiceFactory,
      IClassificationDocumentService,
      ICompletionDocumentService,
      IHoverTextDocumentService,
      IDiagnosticDocumentService,
      IFormattingDocumentService,
      IDocumentCodeActionService
{
    public ICompilation Compilation { get; }

    public ISourceDocument Document { get; }

    public DocumentServices(
        ICompilation compilation,
        ISourceDocument document)
    {
        this.Compilation = compilation;
        this.Document = document;
    }

    public bool TryGetDocumentService<TService>(
        [NotNullWhen(true)] out TService? service)
        where TService : class, IDocumentService
    {
        service = this as TService;
        return service != null;
    }

    public virtual ClassificationResult GetClassifications(
        TextRange range,
        Settings options,
        CancellationToken cancellationToken)
    {
        var tree = this.Compilation.GetSyntaxTree(this.Document);
        if (tree == null)
            return ClassificationResult.Empty;

        var tokens = tree.GetTokens(range.Start, range.Length);

        var classifications = tokens.Select(t =>
            new ClassifiedTextRange(GetTokenClassification(t), t.TextStart, t.TextLength)
            ).ToImmutableList();

        return new ClassificationResult(classifications);
    }

    protected virtual ClassificationKind GetTokenClassification(ISyntaxToken token) =>
        ClassificationKind.Text();


    public virtual CompletionResult GetCompletions(
        int position,
        char? lastKey,
        Settings options,
        CancellationToken cancellation)
    {
        var tree = this.Compilation.GetSyntaxTree(this.Document);
        if (tree == null)
            return CompletionResult.Empty;

        var completions = new List<CompletionItem>();
        var annotations = this.Compilation.GetGrammarAnnotations<object>(this.Document, position, a => a is String || a is CompletionItem);
        completions.AddRange(annotations.OfType<string>().Select(term => new CompletionItem(term)));
        completions.AddRange(annotations.OfType<CompletionItem>());

        var symbols = this.Compilation.GetSymbolsInScope(this.Document, position);
        completions.AddRange(symbols.Select(s => new CompletionItem(s.Name)));

        completions.Sort((a, b) => string.Compare(a.OrderText, b.OrderText));

        return new CompletionResult(completions.ToImmutableList());
    }

    public virtual DiagnosticResult GetDiagnostics(
        int start, int length,
        Settings options,
        CancellationToken cancellation)
    {
        var compilation = this.Compilation;
        var docDiagnostics = compilation.GetDiagnostics(this.Document);

        if (start == 0 && length == this.Document.Text.Length)
            return new DiagnosticResult(docDiagnostics);

        var diagnostics = docDiagnostics
            .Where(
                d => d.Location != null
                && d.Location.End > start
                && d.Location.Start < start + length
                )
            .ToImmutableList();

        return new DiagnosticResult(diagnostics);
    }

    public virtual DiagnosticResult GetDiagnostics(Settings options, CancellationToken cancellation)
    {
        return GetDiagnostics(0, this.Document.Text.Length, options, cancellation);
    }

    public virtual HoverTextResult GetHoverText(
        int position,
        Settings options,
        CancellationToken cancellationToken)
    {
        var compilation = this.Compilation;

        var info = compilation.GetSemanticInfo(this.Document, position);
        var diagnostics = GetDiagnostics(position, 0, options, cancellationToken);

        var sections = new List<HoverTextSection>();
        if (info.ReferencedSymbol != null
            || info.ResultType != null)
        {
            var glyph = info.ReferencedSymbol != null
                ? GetGlyph(info.ReferencedSymbol)
                : "Expression";

            var text =
                info.ReferencedSymbol != null ?
                    (info.ReferencedSymbol.FullName != info.ReferencedSymbol.Name
                        ? $"{info.ReferencedSymbol.Name} ({info.ReferencedSymbol.FullName})"
                        : info.ReferencedSymbol.Name)
                : info.ResultType != null ? info.ResultType.FullName
                : "";

            var section = new HoverTextSection(glyph, text);
            sections.Add(section);
        }

        if (diagnostics.Diagnostics.Count > 0)
        {
            sections.Add(
                new HoverTextSection(
                    "Diagnostic",
                    diagnostics.Diagnostics[0].ToString()
                    ));
        }

        return new HoverTextResult(sections.ToImmutableList());
    }

    protected virtual string GetGlyph(ISymbol symbol) =>
        symbol.Kind;

    /// <summary>
    /// Gets available code actions at the specified text position.
    /// </summary>
    public virtual CodeActionResult GetActions(
        int position, 
        Settings options,
        CancellationToken cancellationToken) 
        =>
        CodeActionResult.Empty;

    /// <summary>
    /// Gets the operations needed to apply the code action.
    /// </summary>
    public virtual CodeOperationResult GetOperations(
        ICodeAction action, 
        Settings options,
        CancellationToken cancellationToken) 
        =>
        CodeOperationResult.Empty;

    /// <summary>
    /// Applies formatting rules to the text range specified.
    /// </summary>
    public virtual FormattingResult Format(
        int start, 
        int length, 
        Settings options,
        CancellationToken cancellationToken) 
        =>
        new FormattingResult(this.Document.Text.Substring(start, length));
}