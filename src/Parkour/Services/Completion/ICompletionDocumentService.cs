namespace Parkour.Services;

public interface ICompletionDocumentService : IDocumentService
{
    /// <summary>
    /// Gets the list of completion items available at the text position, 
    /// given the last key pressed.
    /// </summary>
    /// <param name="position">The text position of the caret when completion is requested.</param>
    /// <param name="triggerKey">The key pressed that triggered the completion request.</param>
    /// <param name="options"></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    CompletionResult GetCompletions(
        int position,
        char? triggerKey,
        Settings options,
        CancellationToken cancellationToken);
}
