namespace Parkour.Services;
using Parkour.Text;

public interface IClassificationDocumentService : IDocumentService
{
    /// <summary>
    /// Gets the classified text segments in the text range, in order.
    /// This information is used for text colorization in the editor.
    /// </summary>
    ClassificationResult GetClassifications(
        TextRange range,
        Settings options,
        CancellationToken cancellationToken);
}
