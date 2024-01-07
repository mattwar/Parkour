namespace Parkour.Analysis;
using Symbols;
using Syntax;

public abstract class SemanticAnalysis
{
    public abstract IReadOnlyList<SyntaxTree> Trees { get; }
    public abstract bool TryGetTreeAnalysis(SyntaxTree tree, out SyntaxTreeAnalysis analysis);
}

public class SyntaxTreeAnalysis
{
    /// <summary>
    /// The <see cref="SyntaxTree"/> this analysis is associated with.
    /// </summary>
    public SyntaxTree Tree { get; }

    protected SyntaxTreeAnalysis(SyntaxTree tree)
    {
        this.Tree = tree;
    }

    /// <summary>
    /// Gets the primary symbol referenced by the element.
    /// </summary>
    public virtual Symbol? GetReferencedSymbol(SyntaxElement element)
    {
        return null;
    }

    /// <summary>
    /// Gets all symbols referenced by the element.
    /// </summary>
    public virtual void GetReferencedSymbols(SyntaxElement element, List<Symbol> symbols)
    {
    }

    /// <summary>
    /// Gets the result type of the element.
    /// This is usually only returns a value for expression elements.
    /// </summary>
    public virtual Symbol? GetResultType(SyntaxElement element)
    {
        return null;
    }

    /// <summary>
    /// Gets all the symbols in scope at the specified position.
    /// </summary>
    public virtual void GetSymbolsAtPosition(int textPosition, List<Symbol> symbols)
    {
    }

    /// <summary>
    /// Returns diagnostics associated with a specific syntax element.
    /// </summary>
    public virtual void GetDiagnostics(SyntaxElement syntax, List<Diagnostic> diagnostics)
    {
        if (syntax.Diagnostic != null)
        {
            diagnostics.Add(syntax.Diagnostic.WithLocation(syntax));
        }
    }

    /// <summary>
    /// Returns diagnostics associated with all syntax elements in the tree.
    /// </summary>
    public void GetDiagnostics(List<Diagnostic> diagnostics)
    {
        if (_diagnostics != null)
        {
            diagnostics.AddRange(_diagnostics);
        }
        else
        {
            SyntaxElement.WalkElements(Tree.Root, fnAfter: (element) =>
            {
                GetDiagnostics(element, diagnostics);
            });
        }
    }

    private IReadOnlyList<Diagnostic>? _diagnostics = null;

    /// <summary>
    /// Returns diagnostics associated with all syntax elements in the tree.
    /// </summary>
    public IReadOnlyList<Diagnostic> GetDiagnostics()
    {
        if (_diagnostics == null)
        {
            var diagnostics = new List<Diagnostic>();
            GetDiagnostics(diagnostics);
            System.Threading.Interlocked.CompareExchange(ref _diagnostics, diagnostics, null);
        }

        return _diagnostics;
    }
}
