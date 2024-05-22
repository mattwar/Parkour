namespace Parkour;

public readonly struct LinePosition
{
    /// <summary>
    /// The line number within the text; (1-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The character offset from the start of the line in the document; (1-based).
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// True if the <see cref="LinePosition"/> is a valid document location.
    /// </summary>
    public bool IsValid => this.Line != 0 && this.Offset != 0;

    public LinePosition(int line, int offset)
    {
        this.Line = line;
        this.Offset = offset;
    }

    public override string ToString() =>
        $"({Line}, {Offset})";
}
