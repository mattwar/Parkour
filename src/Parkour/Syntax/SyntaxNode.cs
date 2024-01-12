namespace Parkour.Syntax;

/// <summary>
/// A non-terminal synatx element that contains zero or more child elements.
/// </summary>
public abstract class SyntaxNode 
    : SyntaxElement, ISyntaxNode
{
    protected SyntaxNode(string kind, Diagnostic? diagnostic)
        : base(kind, diagnostic)
    {
    }

    #region ISyntaxNode
    ISyntaxElement? ISyntaxNode.GetChild(int index) =>
        GetChild(index);
    #endregion
}
