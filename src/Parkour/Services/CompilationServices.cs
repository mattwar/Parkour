
namespace Parkour.Services;

public class CompilationServices : DocumentServices
{
    public Compilation Compilation { get; }

    public CompilationServices(
        Compilation compilation,
        ISourceDocument document)
        : base(document)
    {
        this.Compilation = compilation;
    }

    public override ClassificationResult GetClassifications(
        int start, int length, 
        ServiceOptions options,
        CancellationToken cancellationToken)
    {
        var tree = this.Compilation.GetSyntaxTree(this.Document);
        if (tree == null)
            return ClassificationResult.Empty;
       
        var tokens = tree.GetTokens(start, length);
        
        var classifications = tokens.Select(t =>
            new ClassifiedSpan(GetTokenClassification(t), t.TextStart, t.TextLength)
            ).ToImmutableList();

        return new ClassificationResult(classifications);
    }

    protected virtual string GetTokenClassification(ISyntaxToken token) =>
        ClassificationKinds.Text;

    public override ImmutableList<string> GetClassificationKinds()
    {
        return [ClassificationKinds.Text];
    }

    public override CompletionResult GetCompletions(
        int position, 
        char? lastKey, 
        ServiceOptions options,
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

    public override DiagnosticResult GetDiagnostics(
        int start, int length, 
        ServiceOptions options,
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

    public override HoverTextResult GetHoverText(
        int position, 
        ServiceOptions options,
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

            var section = new HoverTextSection(glyph, text, ImmutableList<StyledRange>.Empty);
            sections.Add(section);
        }

        if (diagnostics.Diagnostics.Count > 0)
        {
            sections.Add(
                new HoverTextSection(
                    "Diagnostic",
                    diagnostics.Diagnostics[0].ToString(),
                    ImmutableList<StyledRange>.Empty));
        }

        return new HoverTextResult(sections.ToImmutableList());
    }

    protected virtual string GetGlyph(ISymbol symbol) =>
        symbol.Kind;
}
