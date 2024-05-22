namespace Parkour.Services;

public interface IClassificationService : IDocumentService
{
    /// <summary>
    /// Gets the classified text segments in the text range, in order.
    /// This information is used for text colorization in the editor.
    /// </summary>
    ClassificationResult GetClassifications(
        int start, 
        int length, 
        ServiceOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// The list of all classifications produced by the language.
    /// </summary>
    ImmutableList<string> GetClassificationKinds();
}