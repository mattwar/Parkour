namespace Parkour.Services;

public record HoverTextResult(
    ImmutableList<HoverTextSection> Sections)
{
    public static HoverTextResult Empty = 
        new HoverTextResult(ImmutableList<HoverTextSection>.Empty);
}
