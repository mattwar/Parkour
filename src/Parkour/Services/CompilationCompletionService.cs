namespace Parkour.Services;
using Parsing;

public class CompilationCompletionService : CompletionService
{
    private readonly CompilationLanguageService _service;

    public CompilationCompletionService(CompilationLanguageService service)
    {
        _service = service;
    }

    public override CompletionList GetCompletions(string text, int position, char? lastKey = null)
    {
        var compilation = _service.GetCompilation();
        var list = new List<CompletionItem>();

        var doc = compilation.Documents.FirstOrDefault(d => d.Text == text);
        if (doc == null)
            return CompletionList.Empty;

        var tree = compilation.GetSyntaxTree(doc);

        var annotations = new List<object>();
        compilation.GetAnnotations(doc, position, a => a is String || a is CompletionItem, annotations);

        list.AddRange(annotations.OfType<string>().Select(term => new CompletionItem(term, term, term)));
        list.AddRange(annotations.OfType<CompletionItem>());

        var symbols = new List<ISymbol>();
        compilation.GetSymbolsInScope(doc, position, symbols);

        list.AddRange(symbols.Select(s => new CompletionItem(s.Name, s.Name, s.Name)));

        list.Sort((a, b) => string.Compare(a.DisplayText, b.DisplayText));

        return new CompletionList(list.ToImmutableList());
    }
}
