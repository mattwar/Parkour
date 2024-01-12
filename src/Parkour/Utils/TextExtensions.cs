using System.Runtime.CompilerServices;

namespace Parkour;

internal static class TextExtensions
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
            var offset = position - lineStarts[lineIndex] + 1; // 1-based
            var line = lineIndex + 1; // 1-based
            return new LinePosition(line, offset);
        }
    }

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
