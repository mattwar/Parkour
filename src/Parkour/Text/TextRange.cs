using System;
using System.Collections.Generic;
using System.Text;

namespace Parkour.Text;

/// <summary>
/// A range of text.
/// </summary>
public record struct TextRange(int Start, int Length)
{
    /// <summary>
    /// The starting position of the range in the text.
    /// </summary>
    public int Start { get; } = Start;

    /// <summary>
    /// The length of the range in the text.
    /// </summary>
    public int Length { get; } = Length;

    /// <summary>
    /// The ending of the range in the text.
    /// </summary>
    public int End => this.Start + this.Length;

    public static TextRange FromBounds(int start, int end)
    {
        return new TextRange(start, end - start);
    }

    public static readonly TextRange Empty = new TextRange(0, 0);

    /// <summary>
    /// True if this text range overlaps the other text range.
    /// </summary>
    public bool Overlaps(TextRange other)
    {
        return Overlaps(this.Start, this.Length, other.Start, other.Length);
    }

    /// <summary>
    /// True if the range A overlaps the range B
    /// </summary>
    public static bool Overlaps(int startA, int lengthA, int startB, int lengthB)
    {
        var endA = startA + lengthA;
        var endB = startB + lengthB;
        return Math.Max(startA, startB) <= Math.Min(endA, endB);
    }
}