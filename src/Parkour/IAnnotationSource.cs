namespace Parkour;

public interface IAnnotationSource
{
    /// <summary>
    /// Gets the annotations associated with the text position.
    /// </summary>
    void GetAnnotations<TAnnotation>(
        int position, 
        Func<TAnnotation, bool>? filter, 
        List<TAnnotation> annotations);
}
