namespace Parkour.Compilations;
using Binding;
using Semantics;

public class DeclarationCompilation : Compilation
{
    private readonly ImmutableList<ISyntaxTree> _syntaxTrees;
    private readonly Func<ImmutableList<ISyntaxTree>, BindingInfo> _fnBind;

    public override ImmutableList<ISourceDocument> Documents { get; }

    public DeclarationCompilation(
        ImmutableList<ISyntaxTree> syntaxTrees,
        Func<ImmutableList<ISyntaxTree>, BindingInfo> fnBind)
    {
        _syntaxTrees = syntaxTrees;
        _fnBind = fnBind;

        this.Documents = _syntaxTrees
            .OfType<ISourceDocument>()
            .ToImmutableList();
    }

    public DeclarationCompilation(
        ImmutableList<ISyntaxTree> syntaxTrees,
        BindingInfo info)
        : this(syntaxTrees, _trees => info)
    {
    }

    public record BindingInfo(
        ImmutableList<Declaration> BoundDeclarations);

    private BindingInfo? _info;

    private BindingInfo GetInfo()
    {
        if (_info == null)
        {
            var tmp = _fnBind(_syntaxTrees);
            Interlocked.CompareExchange(ref _info, tmp, null);
        }

        return _info;
    }

    private Dictionary<ISourceDocument, ImmutableList<Declaration>>? _docToDeclarationMap;

    private ImmutableList<Declaration> GetBoundDeclarations(ISourceDocument document)
    {
        if (_docToDeclarationMap == null)
        {
            var info = GetInfo();

            var tmp = info.BoundDeclarations
                .Where(d => d.Location != null)
                .ToLookup(d => d.Location!.Document, d => d)
                .ToDictionary(group => group.Key, group => group.ToImmutableList());

            Interlocked.CompareExchange(ref _docToDeclarationMap, tmp, null);
        }

        _docToDeclarationMap.TryGetValue(document, out var declarations);
        return declarations ?? ImmutableList<Declaration>.Empty;
    }

    public override ISyntaxTree? GetSyntaxTree(ISourceDocument document)
    {
        return document as ISyntaxTree;
    }

    public override void GetAnnotations<TAnnotation>(
        ISourceDocument document, 
        int position, 
        Func<TAnnotation, bool>? filter, 
        List<TAnnotation> annotations)
    {
        if (_syntaxTrees.FirstOrDefault(t => t.Document == document) is { } tree)
        {
            tree.Annotations.GetAnnotations(position, filter, annotations);
        }
    }

    public override void GetDiagnostics(ISourceDocument document, Func<Diagnostic, bool>? filter, List<Diagnostic> diagnostics)
    {
        // get all syntax diagnostics for document
        diagnostics.AddRange(
            _syntaxTrees
                .Where(t => t.Document == document)
                .SelectMany(t => t.Diagnostics)
            .Where(d => filter == null || filter(d))
            );

        // get all semantic diagnostics for document
        var info = GetInfo();
        var docDeclarations = GetBoundDeclarations(document);
        foreach (var decl in docDeclarations)
        {
            decl.GetContainedDiagnostics(filter, diagnostics);
        }
    }
}
