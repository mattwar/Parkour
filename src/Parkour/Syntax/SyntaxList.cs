namespace Parkour.Syntax;

/// <summary>
/// A <see cref="SyntaxNode"/> that represents an arbitrary list of <see cref="SyntaxElement"/>
/// </summary>
public class SyntaxList : SyntaxNode
{
    public IReadOnlyList<SyntaxElement?> Elements { get; }
    public override int Length { get; }

    public SyntaxList(string kind, IReadOnlyList<SyntaxElement?> elements, Diagnostic? diagnostic = null)
        : base(kind, diagnostic)
    {
        int offsetInParent = 0;

        var newElements = new List<SyntaxElement?>(elements.Count);
        var length = 0;

        for (int index = 0; index < elements.Count; index++)
        {
            var element = elements[index];
            if (element != null)
            {
                element.SetParent(this, offsetInParent, index);
                offsetInParent += element.Length;
                newElements.Add(element);
                length += element.Length;
            }
            else
            {
                newElements.Add(null);
            }
        }

        this.Length = length;
        this.Elements = newElements.AsReadOnly();
    }

    public override int ChildCount => this.Elements.Count;
    public override SyntaxElement? GetChild(int index) => this.Elements[index];

    /// <summary>
    /// Creates a new <see cref="SyntaxList"/> with the specified elements if they are different
    /// than the this list's elements.
    /// </summary>
    public SyntaxList Update(IReadOnlyList<SyntaxElement?> elements)
    {
        return Elements == elements ? this : new SyntaxList(Kind, elements);
    }
}
