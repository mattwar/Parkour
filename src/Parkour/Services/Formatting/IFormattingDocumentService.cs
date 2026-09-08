namespace Parkour.Services;

public interface IFormattingDocumentService
{
    /// <summary>
    /// Gets the formatted version of the text in the specified range.
    /// </summary>
    FormattingResult Format(
        int start, 
        int length,
        Settings options,
        CancellationToken cancellationToken);
}
