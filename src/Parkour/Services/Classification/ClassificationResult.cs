namespace Parkour.Services;

public record ClassificationResult(ImmutableList<ClassifiedSpan> Classifications)
{
    public static ClassificationResult Empty =
        new ClassificationResult(ImmutableList<ClassifiedSpan>.Empty);
}