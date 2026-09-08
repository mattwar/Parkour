using Parkour.Text;

namespace Parkour.Services;

public abstract record TextStyle
{
    public record Plain : TextStyle;
    public record Bold : TextStyle;
    public record Italic : TextStyle;
    public record Code : TextStyle;
}

public static class TextStyleExtensions
{
    private static readonly TextStyle _plain = new TextStyle.Plain();
    private static readonly TextStyle _bold = new TextStyle.Bold();
    private static readonly TextStyle _italic = new TextStyle.Italic();
    private static readonly TextStyle _code = new TextStyle.Code();

    extension(TextStyle)
    {
        public static TextStyle Plain() => _plain;
        public static TextStyle Bold() => _bold;
        public static TextStyle Italic() => _italic;
        public static TextStyle Code() => _code;
    }
}