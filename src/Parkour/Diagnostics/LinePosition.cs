namespace Parkour.Diagnostics;

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
    /// True if the <see cref="LinePosition"/> is valid.
    /// </summary>
    public bool IsValid => this.Line != 0 && this.Offset != 0;

    public LinePosition(int line, int offset)
    {
        this.Line = line;
        this.Offset = offset;
    }
}
