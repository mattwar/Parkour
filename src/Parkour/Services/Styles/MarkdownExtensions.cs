
namespace Parkour.Services;

using Parkour.Text;

public static class MarkdownExtensions
{
    public static string ToMarkdown(
        this StyledText text)
    {
        return ToMarkdown(text.Text, text.Styles);
    }

    public static string ToMarkdown(
        this string text,
        ImmutableList<StyledTextRange> styles)
    {
        var edits = styles.Select(s => TextEdit.Replacement(s.Start, s.Length, ApplyStyle(text.Substring(s.Start, s.Length), s.Style))).ToImmutableList();
        return new EditString(text).ApplyAll(edits).CurrentText;
    }

    public static string ApplyStyle(string text, TextStyle style)
    {
        return style switch
        {
            TextStyle.Bold => $"**{text}**",
            TextStyle.Italic => $"*{text}*",
            TextStyle.Code => text.Contains('\n') ? $"```\n{text}\n```" : $"`{text}`",
            TextStyle.Plain => text,
            _ => text,
        };
    }
}
