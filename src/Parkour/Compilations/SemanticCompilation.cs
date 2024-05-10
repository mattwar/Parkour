namespace Parkour.Compilations;

using Semantics;
using Symbols;

public abstract class SemanticCompilation : Compilation
{
    public override ImmutableList<ISourceDocument> Documents { get; }

    public SemanticCompilation(
        ImmutableList<ISourceDocument> documents)
    {
        this.Documents = documents;
    }

    protected abstract ParseInfo Parse();
    protected abstract BindingInfo Bind();

    public ImmutableList<ISyntaxTree> SyntaxTrees => 
        this.GetParseInfo().SyntaxTrees;

    public record ParseInfo(
        ImmutableList<ISyntaxTree> SyntaxTrees);

    public record BindingInfo(
        SymbolTable Symbols,
        ImmutableList<SemanticElement> BoundElements);

    private ParseInfo? _parseInfo;
    private BindingInfo? _bindingInfo;

    private ParseInfo GetParseInfo()
    {
        if (_parseInfo == null)
        {
            var tmp = this.Parse();
            Interlocked.CompareExchange(ref _parseInfo, tmp, null);
        }
        return _parseInfo;
    }

    private BindingInfo GetBindingInfo()
    {
        if (_bindingInfo == null)
        {
            var tmp = this.Bind();
            Interlocked.CompareExchange(ref _bindingInfo, tmp, null);
        }

        return _bindingInfo;
    }

    private ImmutableDictionary<ISourceDocument, ISyntaxTree>? _docToTreeMap;

    public override ISyntaxTree? GetSyntaxTree(ISourceDocument document)
    {
        if (_docToTreeMap == null)
        {
            var info = this.GetParseInfo();
            var map = info.SyntaxTrees.ToImmutableDictionary(t => t.Document, t => t);
            Interlocked.CompareExchange(ref _docToTreeMap, map, null);
        }

        _docToTreeMap.TryGetValue(document, out var tree);
        return tree;
    }

    private Dictionary<ISourceDocument, ImmutableList<SemanticElement>>? _docToElementMap;

    private ImmutableList<SemanticElement> GetBoundElements(ISourceDocument document)
    {
        if (_docToElementMap == null)
        {
            var info = GetBindingInfo();

            var tmp = info.BoundElements
                .Where(d => d.Location != null)
                .ToLookup(d => d.Location!.Document, d => d)
                .ToDictionary(group => group.Key, group => group.ToImmutableList());

            Interlocked.CompareExchange(ref _docToElementMap, tmp, null);
        }

        _docToElementMap.TryGetValue(document, out var declarations);
        return declarations ?? ImmutableList<SemanticElement>.Empty;
    }

    public override void GetAnnotations<TAnnotation>(
        ISourceDocument document, 
        int position, 
        Func<TAnnotation, bool>? filter, 
        List<TAnnotation> annotations)
    {
        if (GetSyntaxTree(document) is { } tree)
        {
            tree.Annotations.GetAnnotations(position, filter, annotations);
        }
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
            var docDeclarations = GetBoundElements(document);
            foreach (var decl in docDeclarations)
            {
                decl.GetContainedDiagnostics(list);
            }

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
            && GetBoundElements(document) is { } declarations)
        {
            foreach (var decl in declarations)
            {
                var element = decl.GetElementAtLocation(token.TextStart);
                if (element != null)
                    return element;
            }
        }

        return null;
    }
}
