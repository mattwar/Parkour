namespace Parkour.Services;

public record ClassificationResult(ImmutableList<ClassifiedTextRange> ClassifiedRanges)
{
    public static ClassificationResult Empty =
        new ClassificationResult(ImmutableList<ClassifiedTextRange>.Empty);
}