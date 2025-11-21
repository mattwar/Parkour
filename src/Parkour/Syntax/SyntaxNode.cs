namespace Parkour.Syntax;

/// <summary>
/// A non-terminal syntax element that contains zero or more child elements.
/// </summary>
public abstract record SyntaxNode(Diagnostic? Diagnostic)
    : SyntaxElement(Diagnostic), ISyntaxNode
{
    #region ISyntaxNode
    ISyntaxElement? ISyntaxNode.GetChild(int index) =>
        GetChild(index);
    #endregion
}