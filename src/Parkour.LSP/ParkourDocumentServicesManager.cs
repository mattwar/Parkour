namespace Parkour.LSP;

using Services;

public class ParkourDocumentServicesManager
{
    private readonly ParkourDocumentManager _documentManager;
    private readonly ParkourLanguage _language;

    public ParkourDocumentServicesManager(
        ParkourDocumentManager documentManager, 
        ParkourLanguage language)
    {
        _documentManager = documentManager;
        _language = language;
    }

    public class CompilationInfo
    {
        public ImmutableList<TextDocumentIdentifier> DocumentIds { get; }
        public ICompilation Compilation { get; }
        private ParkourLanguage _language;

        public CompilationInfo(
            ImmutableList<TextDocumentIdentifier> documentIds,
            ICompilation compilation,
            ParkourLanguage language)
        {
            this.DocumentIds = documentIds;
            this.Compilation = compilation;
            _language = language;
        }

        private ImmutableDictionary<ISourceDocument, IDocumentServiceFactory> _docToServiceFactoryMap =
            ImmutableDictionary<ISourceDocument, IDocumentServiceFactory>.Empty;

        public bool TryGetDocumentServiceFactory(TextDocumentIdentifier id, [NotNullWhen(true)] out IDocumentServiceFactory? factory)
        {
            var index = this.DocumentIds.IndexOf(id);
            if (index >= 0 && index < this.Compilation.Documents.Count)
            {
                var document = this.Compilation.Documents[index];

                if (!_docToServiceFactoryMap.TryGetValue(document, out factory))
                {
                    var tmp = _language.CreateDocumentServiceFactory(this.Compilation, document);
                    factory = ImmutableInterlocked.GetOrAdd(ref _docToServiceFactoryMap, document, tmp);
                }

                return factory != null;
            }
            else
            {
                factory = null;
                return false;
            }
        }
    }

    private ImmutableDictionary<Uri, CompilationInfo> _docIdToCompilation =
        ImmutableDictionary<Uri, CompilationInfo>.Empty;

    private CompilationInfo GetCurrentCompilationInfo(
        TextDocumentIdentifier id)
    {
        var key = id.Uri.ToUri();

        if (_docIdToCompilation.TryGetValue(key, out var info))
        {
            var currentCompilation = GetCurrent(info.DocumentIds, info.Compilation);
            if (currentCompilation != info.Compilation)
            {
                var newInfo = new CompilationInfo(info.DocumentIds, currentCompilation, _language);
                info = ImmutableInterlocked.AddOrUpdate(ref _docIdToCompilation, key, _ => newInfo, (_, _) => newInfo);
            }
        }
        else
        {
            ImmutableList<TextDocumentIdentifier> ids = [id];
            var tmp = new CompilationInfo(ids, CreateCompilation(ids), _language);
            info = ImmutableInterlocked.GetOrAdd(ref _docIdToCompilation, key, tmp);
        }

        return info;

        ICompilation GetCurrent(
            ImmutableList<TextDocumentIdentifier> documentIds,
            ICompilation compilation)
        {
            if (AreDocumentsCurrent(documentIds, compilation))
                return compilation;

            return CreateCompilation(documentIds);
        }

        ICompilation CreateCompilation(
            ImmutableList<TextDocumentIdentifier> documentIds)
        {
            var newDocs = new List<ISourceDocument>();
            foreach (var id in documentIds)
            {
                if (_documentManager.TryGetSourceDocument(id, out var doc))
                {
                    newDocs.Add(doc);
                }
            }

            return _language.CreateCompilation(newDocs.ToImmutableList());
        }

        bool AreDocumentsCurrent(ImmutableList<TextDocumentIdentifier> documentIds, ICompilation compilation)
        {
            if (compilation.Documents.Count != documentIds.Count)
                return false;

            for (int i = 0; i < documentIds.Count; i++)
            {
                var id = documentIds[i];
                if (!_documentManager.TryGetSourceDocument(id, out var doc)
                    || doc != compilation.Documents[i])
                    return false;
            }

            return true;
        }
    }

    private bool TryGetServiceFactory(
        TextDocumentIdentifier id,
        [NotNullWhen(true)] out IDocumentServiceFactory? factory)
    {
        var info = GetCurrentCompilationInfo(id);
        return info.TryGetDocumentServiceFactory(id, out factory);
    }

    public bool TryGetDocumentService<TService>(TextDocumentIdentifier id, [NotNullWhen(true)] out TService? service)
        where TService : class, IDocumentService
    {
        if (TryGetServiceFactory(id, out var factory))
        {
            return factory.TryGetDocumentService<TService>(out service);
        }
        else
        {
            service = null;
            return false;
        }
    }

    private IDocumentServiceFactory? _defaultFactory;

    public bool TryGetDefaultDocumentService<TService>([NotNullWhen(true)] out TService? service)
        where TService : class, IDocumentService
    {
        if (_defaultFactory == null)
        {
            var doc = new SourceDocument("", "");
            var tmp = _language.CreateDocumentServiceFactory(
                _language.CreateCompilation([doc]),
                doc
                );
            Interlocked.CompareExchange(ref _defaultFactory, tmp, null);
        }

        return _defaultFactory.TryGetDocumentService(out service);
    }
}