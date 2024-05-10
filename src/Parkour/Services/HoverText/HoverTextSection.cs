namespace Parkour.Services;

public record HoverTextSection(
    string Glyph,
    string Text,
    ImmutableList<StyledRange> Styles);
