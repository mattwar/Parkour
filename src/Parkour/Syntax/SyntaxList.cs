namespace Parkour.Syntax;

/// <summary>
/// A <see cref="SyntaxNode"/> base class that represents an arbitrary list of <see cref="SyntaxElement"/>.
/// </summary>
public record SyntaxList(Diagnostic? Diagnostic)
    : SyntaxNode(Diagnostic)
{
    /// <summary>
    /// The elements of the list.
    /// </summary>
    public IReadOnlyList<SyntaxElement?> Elements { get; } = Array.Empty<SyntaxElement?>();

    public override int Length { get; }

    public SyntaxList(IReadOnlyList<SyntaxElement?> elements, Diagnostic? diagnostic = null)
        : this(diagnostic)
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
}
