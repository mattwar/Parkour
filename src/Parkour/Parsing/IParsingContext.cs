namespace Parkour.Parsing;

public interface IParsingContext
{
    /// <summary>
    /// Gets the parsing annotations associated with the text position.
    /// </summary>
    ImmutableList<TAnnotation> GetAnnotations<TAnnotation>(
        int position,
        Func<TAnnotation, bool>? filter = null);
}
