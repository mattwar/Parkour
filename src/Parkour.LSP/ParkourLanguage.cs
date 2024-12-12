namespace Parkour.LSP;

using Services;

public abstract class ParkourLanguage
{
    public abstract string LanguageId { get; }
    public abstract string DocumentPattern { get; }
    public abstract ICompilation CreateCompilation(ImmutableList<ISourceDocument> documents);
    public abstract IDocumentServiceFactory CreateDocumentServiceFactory(ICompilation compilation, ISourceDocument document);
}
