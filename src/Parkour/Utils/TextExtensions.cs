using System.Runtime.CompilerServices;

namespace Parkour;

public static class TextExtensions
{
    /// <summary>
    /// Returns this <see cref="LinePosition"/> for the text position.
    /// </summary>
    public static LinePosition GetLinePosition(this string text, int position)
    {
        var lineStarts = GetLineStarts(text);
        var lineIndex = lineStarts.BinarySearch(position);
        lineIndex = lineIndex >= 0 ? lineIndex : ~lineIndex - 1;

        if (lineIndex < 0 || lineIndex >= lineStarts.Count)
        {
            return default;
        }
        else
        {
            var offset = position - lineStarts[lineIndex];
            var line = lineIndex;
            return new LinePosition(line, offset);
        }
    }

    /// <summary>
    /// Gets the text position for the zero-based line and offset
    /// </summary>
    public static int GetTextPosition(this string text, int line, int lineOffset)
    {
        var lineStarts = GetLineStarts(text);
        if (line >= 0 && line < lineStarts.Count)
        {
            return lineStarts[line] + lineOffset;
        }
        else if (line < 0 && lineStarts.Count > 0)
        {
            return lineStarts[0];
        }
        else if (line >= lineStarts.Count && lineStarts.Count > 0)
        {
            return lineStarts[^1];
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the text position for the <see cref="LinePosition"/>
    /// </summary>
    public static int GetTextPosition(this string text, LinePosition linePosition) =>
        GetTextPosition(text, linePosition.Line, linePosition.Offset);

    private static readonly ConditionalWeakTable<string, ImmutableList<int>> _lineStartsMap =
        new ConditionalWeakTable<string, ImmutableList<int>>();

    private static ImmutableList<int> GetLineStarts(string text)
    {
        if (_lineStartsMap.TryGetValue(text, out var lineStarts))
        {
            lineStarts = _lineStartsMap.GetValue(text, _text => CreateLineStarts(_text));
        }

        return lineStarts!;
    }

    private static readonly char[] _lineBreakChars = ['\n', '\r'];

    private static ImmutableList<int> CreateLineStarts(string text)
    {
        var lineStarts = new List<int>();

        var start = 0;
        while (start < text.Length)
        {
            var nextBreak = text.IndexOfAny(_lineBreakChars, start);
            if (nextBreak <= start)
                break;

            if (text[nextBreak] == '\r'
                && nextBreak < text.Length - 1
                && text[nextBreak + 1] == '\n')
            {
                nextBreak++;
            }
            lineStarts.Add(nextBreak);
            start = nextBreak;
        }

        return lineStarts.ToImmutableList();
    }
}
