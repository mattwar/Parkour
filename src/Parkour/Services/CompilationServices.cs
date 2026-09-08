namespace Parkour.Services;

/// <summary>
/// An aggregate of common services available for an entire compilation.
/// </summary>
public class CompilationServices 
    : ICompilationServiceFactory,
      IDocumentServiceFactoryCompilationService,
      IDiagnosticCompilationService
{
    /// <summary>
    /// The language this compilation is associated with.
    /// </summary>
    public ILanguage Language { get; }

    /// <summary>
    /// The compilation this service instance is associated with.
    /// </summary>
    public ICompilation Compilation { get; }

    public CompilationServices(ILanguage language, ICompilation compilation)
    {
        this.Language = language;
        this.Compilation = compilation;
    }

    public bool TryGetComilationService<TService>([NotNullWhen(true)] out TService? service) where TService : class, ICompilationService
    {
        if (this is TService tservice)
        {
            service = tservice;
            return true;
        }
        service = null;
        return false;
    }

    private ImmutableDictionary<ISourceDocument, IDocumentServiceFactory> _docToServiceFactoryMap =
        ImmutableDictionary<ISourceDocument, IDocumentServiceFactory>.Empty;

    public IDocumentServiceFactory GetDocumentServiceFactory(ISourceDocument document)
    {
        if (!_docToServiceFactoryMap.TryGetValue(document, out var factory))
        {
            factory = ImmutableInterlocked.GetOrAdd(ref _docToServiceFactoryMap, document, GetFactory);
        }
        return factory;
    }

    protected virtual IDocumentServiceFactory CreateDocumentServiceFactory(ISourceDocument document)
    {
        return new DocumentServices(this.Compilation, document);
    }

    protected IDocumentServiceFactory GetFactory(ISourceDocument document)
    {
        return new DocumentServices(this.Compilation, document);
    }

    public DiagnosticResult GetDiagnostics(Settings options, CancellationToken cancellationToken)
    {
        var dx = new List<Diagnostic>();
        foreach (var doc in this.Compilation.Documents)
        {
            if (GetDocumentServiceFactory(doc) is { } factory
                && factory.TryGetDocumentService<IDiagnosticDocumentService>(out var docDiagnosticService))
            {
                var docResults = docDiagnosticService.GetDiagnostics(options, cancellationToken);
                dx.AddRange(docResults.Diagnostics);
            }
        }
        return new DiagnosticResult(dx.ToImmutableList());
    }
}