namespace Parkour.Syntax;

public partial class SyntaxTree
    : ISyntaxTree, ISourceDocument
{
    public string Name { get; }
    public string Text { get; }
    public IAnnotationSource Annotations { get; }

    private readonly SyntaxElement _root;
   
    public SyntaxTree(
        string name,
        string text,
        SyntaxElement root,
        IAnnotationSource annotations)
    {
        Name = name;
        Text = text;
        _root = root;
        Annotations = annotations;

        // assigns this tree to root, and freezes all syntax elements.
        _root.SetTree(this);
    }

    public SyntaxElement Root => _root;

    private ImmutableList<Diagnostic>? _diagnostics;

    /// <summary>
    /// Gets a list of all the diagnostics produced during parsing.
    /// </summary>
    public ImmutableList<Diagnostic> Diagnostics
    {
        get
        {
            if (_diagnostics == null)
            {
                var list = new List<Diagnostic>();

                SyntaxElement.WalkElements(this.Root, fnAfter: (element) =>
                {
                    if (element.Diagnostic != null)
                        list.Add(element.Diagnostic.WithLocation(element));
                });

                Interlocked.CompareExchange(ref _diagnostics, list.ToImmutableList(), null);
            }

            return _diagnostics;
        }
    }

    #region ISyntaxTree
    ISourceDocument ISyntaxTree.Document => this;
    ISyntaxElement ISyntaxTree.Root => this.Root;

    ImmutableList<ISyntaxToken> ISyntaxTree.GetTokens(int start, int length)
    {
        var tokens = new List<ISyntaxToken>();
        GetTokens(this.Root);
        return tokens.ToImmutableList();

        void GetTokens(ISyntaxElement element)
        {
            if (element is ISyntaxNode node)
            {
                for (int i = 0, n = node.ChildCount; i < n; i++)
                {
                    var child = node.GetChild(i);
                    if (child != null && OverlapsRange(child, start, length))
                    {
                        GetTokens(child);
                    }
                }
            }
            else if (element is ISyntaxToken token
                && OverlapsRange(token, start, length))
            {
                tokens.Add(token);
            }
        }
    }

    private static bool OverlapsRange(ISyntaxElement element, int start, int length) =>
        element.TextStart < start + length
        && element.TextStart + element.TextLength >= start;

    #endregion
}

