namespace Parkour.Services;

public interface IDocumentServiceFactoryCompilationService : ICompilationService
{
    /// <summary>
    /// Gets the factory for document-level services for the specified document.
    /// </summary>
    public IDocumentServiceFactory GetDocumentServiceFactory(ISourceDocument document);
}
