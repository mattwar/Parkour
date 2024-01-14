namespace Parkour.Services;

public class CompilationLanguageService : LanguageService
{
    public ISourceDocument Document { get; }
    private Func<Compilation> _fnCompile;

    public CompilationLanguageService(
        ISourceDocument document,
        Func<Compilation> fnCompile)
    {
        this.Document = document;
        _fnCompile = fnCompile;
    }

    private Compilation? _compilation;

    public Compilation GetCompilation()
    {
        if (_compilation == null)
        {
            var tmp = _fnCompile();
            Interlocked.CompareExchange(ref _compilation, tmp, null);
        }

        return _compilation!;
    }

    public override void GetCompletions(int position, char? lastKey, List<CompletionItem> completions)
    {
        var compilation = GetCompilation();

        var tree = compilation.GetSyntaxTree(this.Document);

        var annotations = new List<object>();
        compilation.GetAnnotations(this.Document, position, a => a is String || a is CompletionItem, annotations);

        completions.AddRange(annotations.OfType<string>().Select(term => new CompletionItem(term, term, term)));
        completions.AddRange(annotations.OfType<CompletionItem>());

        var symbols = new List<ISymbol>();
        compilation.GetSymbolsInScope(this.Document, position, symbols);

        completions.AddRange(symbols.Select(s => new CompletionItem(s.Name, s.Name, s.Name)));

        completions.Sort((a, b) => string.Compare(a.DisplayText, b.DisplayText));
    }

    public override void GetDiagnostics(int position, List<Diagnostic> diagnostics)
    {
        var compilation = GetCompilation();
        compilation.GetDiagnostics(
            this.Document, 
            d => d.Location != null && position >= d.Location.Start && position <= d.Location.End,
            diagnostics);
    }
}
