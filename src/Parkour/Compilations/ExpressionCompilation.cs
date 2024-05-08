namespace Parkour.Compilations;

using Semantics;

public class ExpressionCompilation : Compilation
{
    public override ImmutableList<ISourceDocument> Documents { get; }

    public readonly ISyntaxTree _syntaxTree;
    public readonly Func<ISyntaxTree, BindingInfo> _fnBind;

    public ExpressionCompilation(
        ISyntaxTree syntaxTree,
        Func<ISyntaxTree, BindingInfo> fnBind)
    {
        _syntaxTree = syntaxTree;
        _fnBind = fnBind;
        this.Documents = [syntaxTree.Document];
    }

    public ExpressionCompilation(
        ISyntaxTree tree,
        BindingInfo bindingInfo)
        : this(tree, _tree => bindingInfo)
    {
    }

    public record BindingInfo(
        Expression BoundExpression);

    public BindingInfo? _info;

    private BindingInfo GetInfo()
    {
        if (_info == null)
        {
            var tmp = _fnBind(_syntaxTree);
            Interlocked.CompareExchange(ref _info, tmp, null);
        }

        return _info;
    }

    public override ISyntaxTree? GetSyntaxTree(ISourceDocument document)
    {
        if (document == _syntaxTree.Document)
            return _syntaxTree;
        return null;
    }

    public override void GetAnnotations<TAnnotation>(ISourceDocument document, int position, Func<TAnnotation, bool>? filter, List<TAnnotation> annotations)
    {
        if (document == _syntaxTree.Document)
        {
            _syntaxTree.Annotations.GetAnnotations(position, filter, annotations);
        }
    }

    public override void GetDiagnostics(ISourceDocument document, Func<Diagnostic, bool>? filter, List<Diagnostic> diagnostics)
    {
        if (document == _syntaxTree.Document)
        {
            diagnostics.AddRange(_syntaxTree.Diagnostics.Where(d => filter == null || filter(d)));
            var binding = GetInfo();
            binding.BoundExpression.GetContainedDiagnostics(filter, diagnostics);
        }
    }

    public override void GetSymbolsInScope(ISourceDocument document, int position, List<ISymbol> symbols)
    {
        base.GetSymbolsInScope(document, position, symbols);
    }
}
