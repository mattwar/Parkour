namespace Parkour;

public readonly struct LinePosition
{
    /// <summary>
    /// The zero-based line number within the text.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The zero-based character offset from the start of the line in the document
    /// </summary>
    public int Offset { get; }

    public LinePosition(int line, int offset)
    {
        this.Line = line;
        this.Offset = offset;
    }

    public override string ToString() =>
        $"(Line: {Line}, Offset: {Offset})";
}
