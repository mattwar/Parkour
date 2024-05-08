
namespace Parkour.Services;

public class CompilationServices : StandardServices
{
    public Compilation Compilation { get; }

    public CompilationServices(
        Compilation compilation,
        ISourceDocument document)
        : base(document)
    {
        this.Compilation = compilation;
    }

    public override Task<ClassificationResult> GetClassificationsAsync(int start, int length, CancellationToken cancellationToken)
    {
        var tree = this.Compilation.GetSyntaxTree(this.Document);
        if (tree == null)
            return Task.FromResult(ClassificationResult.Empty);
       
        var tokens = tree.GetTokens(start, length);
        
        var map = GetTokenClassifications();

        var classifications = tokens.Select(t =>
            new ClassifiedSpan(
                map != null
                    ? (map.TryGetValue(t.Kind, out var tc)
                        ? tc 
                        : ClassificationKinds.Text)
                    : ClassificationKinds.Text,
                t.TextStart,
                t.TextLength)
            ).ToImmutableList();

        return Task.FromResult(new ClassificationResult(classifications));
    }

    public override ImmutableList<string> GetClassificationKinds()
    {
        var map = GetTokenClassifications();
        if (map != null)
        {
            return map.Values.Distinct().ToImmutableList();
        }
        else
        {
            return [ClassificationKinds.Text];
        }
    }

    protected virtual ImmutableDictionary<string, string>? GetTokenClassifications() =>
        null;

    public override Task<CompletionResult> GetCompletionsAsync(int position, char? lastKey, CancellationToken cancellation)
    {
        var tree = this.Compilation.GetSyntaxTree(this.Document);
        if (tree == null)
            return Task.FromResult(CompletionResult.Empty);

        var completions = new List<CompletionItem>();

        var annotations = new List<object>();
        this.Compilation.GetAnnotations(this.Document, position, a => a is String || a is CompletionItem, annotations);

        completions.AddRange(annotations.OfType<string>().Select(term => new CompletionItem(term, term, term)));
        completions.AddRange(annotations.OfType<CompletionItem>());

        var symbols = new List<ISymbol>();
        this.Compilation.GetSymbolsInScope(this.Document, position, symbols);

        completions.AddRange(symbols.Select(s => new CompletionItem(s.Name, s.Name, s.Name)));
        completions.Sort((a, b) => string.Compare(a.DisplayText, b.DisplayText));

        return Task.FromResult(new CompletionResult(completions.ToImmutableList()));
    }

    public override Task<DiagnosticResult> GetDiagnosticsAsync(int position, CancellationToken cancellation)
    {
        var compilation = this.Compilation;

        var diagnostics = new List<Diagnostic>();

        compilation.GetDiagnostics(
            this.Document, 
            d => d.Location != null && position >= d.Location.Start && position <= d.Location.End,
            diagnostics);

        return Task.FromResult(new DiagnosticResult(diagnostics.ToImmutableList()));
    }
}
