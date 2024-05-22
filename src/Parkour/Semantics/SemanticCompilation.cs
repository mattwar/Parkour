namespace Parkour.Semantics;

using Parsers;
using Symbols;

/// <summary>
/// A compilation that knows how to parse its documents into syntax trees
/// and bind them into semantic elements.
/// </summary>
public abstract class SemanticCompilation : Compilation
{
    public override ImmutableList<ISourceDocument> Documents { get; }

    public SemanticCompilation(
        ImmutableList<ISourceDocument> documents)
    {
        this.Documents = documents;
    }

    protected abstract ParsingInfo Parse(ISourceDocument document);
    protected abstract BindingInfo Bind();

    protected record ParsingInfo(
        ISyntaxTree SyntaxTree,
        IGrammarAnnotations GrammarAnnotations);

    protected record BindingInfo(
        SymbolTable Symbols,
        ImmutableList<SemanticElement> BoundElements);

    private BindingInfo? _bindingInfo;

    protected BindingInfo GetBindingInfo()
    {
        if (_bindingInfo == null)
        {
            var tmp = this.Bind();
            Interlocked.CompareExchange(ref _bindingInfo, tmp, null);
        }

        return _bindingInfo;
    }

    private ImmutableDictionary<ISourceDocument, ParsingInfo> _docToParsingInfoMap =
        ImmutableDictionary<ISourceDocument, ParsingInfo>.Empty;

    protected ParsingInfo? GetParsingInfo(ISourceDocument document)
    {
        if (!_docToParsingInfoMap.TryGetValue(document, out var info))
        {
            var tmp = this.Parse(document);
            info = ImmutableInterlocked.GetOrAdd(ref _docToParsingInfoMap, document, tmp);
        }

        return info;
    }

    public override ISyntaxTree? GetSyntaxTree(ISourceDocument document) =>
        this.GetParsingInfo(document)?.SyntaxTree;

    private Dictionary<ISourceDocument, ImmutableList<SemanticElement>>? _docToRootElementsMap;

    private ImmutableList<SemanticElement> GetRootElements(ISourceDocument document)
    {
        if (_docToRootElementsMap == null)
        {
            var info = GetBindingInfo();

            var tmp = info.BoundElements
                .Where(d => d.Location != null)
                .ToLookup(d => d.Location!.Document, d => d)
                .ToDictionary(group => group.Key, group => group.ToImmutableList());

            Interlocked.CompareExchange(ref _docToRootElementsMap, tmp, null);
        }

        _docToRootElementsMap.TryGetValue(document, out var declarations);
        return declarations ?? ImmutableList<SemanticElement>.Empty;
    }

    public override ImmutableList<TAnnotation> GetGrammarAnnotations<TAnnotation>(
        ISourceDocument document, 
        int position, 
        Func<TAnnotation, bool>? filter = null)
    {
        if (GetParsingInfo(document) is { } info)
        {
            return info.GrammarAnnotations.GetAnnotations(position, filter);
        }

        return ImmutableList<TAnnotation>.Empty;
    }

    private ImmutableDictionary<ISourceDocument, ImmutableList<Diagnostic>> _docDiagnostics =
        ImmutableDictionary<ISourceDocument, ImmutableList<Diagnostic>>.Empty;

    public override ImmutableList<Diagnostic> GetDiagnostics(ISourceDocument document)
    {
        if (!_docDiagnostics.TryGetValue(document, out var diagnostics))
        {
            var list = new List<Diagnostic>(0);

            // get all syntax diagnostics
            if (this.GetSyntaxTree(document) is { } tree)
            {
                list.AddRange(tree.Diagnostics);
            }

            // get all semantic diagnostics
            var docDeclarations = GetRootElements(document);
            list.AddRange(docDeclarations.GetContainedDiagnostics());

            diagnostics = ImmutableInterlocked.GetOrAdd(ref _docDiagnostics, document, list.ToImmutableList());
        }

        return diagnostics;
    }

    public override SemanticInfo GetSemanticInfo(ISourceDocument document, int position)
    {
        var element = GetSemanticElement(document, position);
        if (element is Expression expr)
        {
            return new SemanticInfo("", expr.ResultType, expr.ReferencedSymbol);
        }

        return SemanticInfo.None;
    }

    protected virtual SemanticElement? GetSemanticElement(ISourceDocument document, int position)
    {
        if (GetSyntaxTree(document) is { } tree
            && tree.GetToken(position) is { } token
            && position >= token.TextStart
            && position < token.TextEnd
            && GetRootElements(document) is { } rootElements)
        {
            foreach (var root in rootElements)
            {
                var element = root.GetElementAtLocation(token.TextStart);
                if (element != null)
                    return element;
            }
        }

        return null;
    }

#if false
    private ImmutableDictionary<ISourceDocument, ImmutableDictionary<ISourceLocation, SemanticElement>> _docToLocationMap =
        ImmutableDictionary<ISourceDocument, ImmutableDictionary<ISourceLocation, SemanticElement>>.Empty;

    protected SemanticElement? GetElementAtLocation(ISourceLocation location)
    {
        if (!_docToLocationMap.TryGetValue(location.Document, out var locationMap))
        {
            var rootElements = GetRootElements(location.Document);
            var map = new Dictionary<ISourceLocation, SemanticElement>();

            foreach (var rootElement in rootElements)
            {
                BuildMap(rootElement);

                void BuildMap(SemanticElement element)
                {
                    if (element.Location != null)
                    {
                        map[element.Location] = element;
                    }

                    for (int i = 0; i < element.ChildCount; i++)
                    {
                        var child = element.GetChild(i);
                        if (child != null)
                        {
                            BuildMap(child);
                        }
                    }
                }
            }

            locationMap = ImmutableInterlocked.GetOrAdd(ref _docToLocationMap, location.Document, _ => map.ToImmutableDictionary());
        }

        locationMap.TryGetValue(location, out var element);
        return element;
    }
#endif
}
