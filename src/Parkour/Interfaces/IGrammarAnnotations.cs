namespace Parkour;

/// <summary>
/// A source of grammar annotations.
/// </summary>
public interface IGrammarAnnotations
{
    /// <summary>
    /// Gets the grammar annotations at the text position.
    /// </summary>
    ImmutableList<TAnnotation> GetAnnotations<TAnnotation>(
        int position,
        Func<TAnnotation, bool>? filter = null);
}