namespace Parkour.Services;

public interface ICompletionLanguageService : ILanguageService
{
    CompletionDefaults GetCompletionDefaults();

    ICompletionDocumentService GetDocumentService(ISourceDocument document, ICompilation compilation);
}
