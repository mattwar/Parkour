namespace Parkour.Projects;

/// <summary>
/// Represents an immutable project within a solution, including its associated documents.
/// </summary>
public class Project
{
    /// <summary>
    /// The details about the project.
    /// </summary>
    public ProjectInfo Info { get; }

    private ImmutableDictionary<string, Document> _documentMap =
        ImmutableDictionary<string, Document>.Empty;

    private ImmutableList<Document>? _documents;

    public Project(
        ProjectInfo info)
    {
        this.Info = info;
    }

    /// <summary>
    /// Tries to get the <see cref="Document"/> for the specified path.
    /// </summary>
    public bool TryGetDocument(string path, [NotNullWhen(true)] out Document? document)
    {
        if (!_documentMap.TryGetValue(path, out document))
        {
            var documentInfo = this.Info.Documents.FirstOrDefault(d => d.Path == path);
            if (documentInfo != null)
            {
                document = GetOrAddDocument(documentInfo);
                return true;
            }
        }
        document = null;
        return false;
    }

    /// <summary>
    /// Gets or adds a <see cref="Document"/> for the specified info.
    /// </summary>
    public Document GetOrAddDocument(DocumentInfo info)
    {
        if (!_documentMap.TryGetValue(info.Path, out var document))
        {
            document = ImmutableInterlocked.GetOrAdd(ref _documentMap, info.Path, new Document(this, info));
        }
        return document;
    }

    /// <summary>
    /// The list of documents in the project.
    /// </summary>
    public ImmutableList<Document> Documents
    {
        get
        {
            if (_documents == null)
            {
                var tmp = this.Info.Documents.Select(GetOrAddDocument).ToImmutableList();
                Interlocked.CompareExchange(ref _documents, tmp, null);
            }
            return _documents;
        }
    }

    public Project AddOrUpdateDocument(DocumentInfo newDocumentInfo)
    {
        var existingInfo = this.Info.Documents.FirstOrDefault(d => d.Path == newDocumentInfo.Path);
        if (existingInfo == null)
        {
            var newProjectInfo = this.Info with
            {
                Documents = this.Info.Documents.Add(newDocumentInfo)
            };
            return new Project(newProjectInfo);
        }
        else if (existingInfo == newDocumentInfo)
        {
            var newProjectInfo = this.Info with
            {
                Documents = this.Info.Documents.Replace(existingInfo, newDocumentInfo)
            };
            return new Project(newProjectInfo);
        }
        else
        {
            return this;
        }
    }

    public Project RemoveDocument(DocumentInfo documentInfo)
    {
        if (this.Info.Documents.Contains(documentInfo))
        {
            var newProjectInfo = this.Info with
            {
                Documents = this.Info.Documents.Remove(documentInfo)
            };
            return new Project(newProjectInfo);
        }
        else
        {
            return this;
        }
    }
}
