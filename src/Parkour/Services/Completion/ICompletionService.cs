namespace Parkour.Services;

public interface ICompletionService : IDocumentService
{
    /// <summary>
    /// Gets the list of completion items available at the text position, 
    /// given the last key pressed.
    /// </summary>
    Task<CompletionResult> GetCompletionsAsync(int position, char? lastKey, CancellationToken cancellationToken);
}
