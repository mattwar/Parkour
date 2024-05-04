namespace Parkour.Compilations;
using Binding;
using Symbols;

public class ExpressionCompilation : Compilation
{
    public readonly ISyntaxTree _syntaxTree;
    public readonly SymbolTable _externalSymbols;
    public readonly Func<ISyntaxTree, SymbolTable, ExpressionBinding> _fnBind;
    public override ImmutableList<ISourceDocument> Documents { get; }

    public ExpressionCompilation(
        ISyntaxTree syntaxTree,
        SymbolTable externalSymbols,
        Func<ISyntaxTree, SymbolTable, ExpressionBinding> fnBind)
    {
        _syntaxTree = syntaxTree;
        _externalSymbols = externalSymbols;
        _fnBind = fnBind;
        this.Documents = [syntaxTree.Document];
    }

    public ExpressionCompilation(
        ISyntaxTree tree,
        ExpressionBinding binding)
        : this(tree, binding.ExternalSymbols, (_tree, _externals) => binding)
    {
    }

    public ExpressionBinding? _binding;

    private ExpressionBinding GetBinding()
    {
        if (_binding == null)
        {
            var tmp = _fnBind(_syntaxTree, _externalSymbols);
            Interlocked.CompareExchange(ref _binding, tmp, null);
        }

        return _binding;
    }

    public override ISyntaxTree? GetSyntaxTree(ISourceDocument document)
    {
        return document as ISyntaxTree;
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
            var binding = GetBinding();
            binding.BoundExpression.GetContainedDiagnostics(filter, diagnostics);
        }
    }

    public override void GetSymbolsInScope(ISourceDocument document, int position, List<ISymbol> symbols)
    {
        base.GetSymbolsInScope(document, position, symbols);
    }
}
