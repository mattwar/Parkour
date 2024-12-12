namespace Parkour.LSP;

using Semantics;
using Symbols;

public class ParkourDocumentManager
{
    private class DocumentInfo
    {
        public int? Version { get; }
        public SourceDocument Document { get; }

        public DocumentInfo(int? version, SourceDocument document)
        {
            this.Version = version;
            this.Document = document;
        }

        public DocumentInfo(TextDocumentItem document)
            : this(document.Version, new SourceDocument(document.Uri.ToString(), document.Text))
        {
        }
    }

    private ImmutableDictionary<Uri, DocumentInfo> _idToSourceDocument
        = ImmutableDictionary<Uri, DocumentInfo>.Empty;

    public ISourceDocument AddOrUpdateDocument(TextDocumentItem document)
    {
        var info = ImmutableInterlocked.AddOrUpdate(
            ref _idToSourceDocument,
            document.Uri.ToUri(),
            _id => new DocumentInfo(document), 
            (_id, _oldInfo) => document.Version > _oldInfo.Version 
                ? new DocumentInfo(document) 
                : _oldInfo
            );

        return info.Document;
    }

    public bool RemoveDocument(TextDocumentIdentifier id)
    {
        var key = id.Uri.ToUri();
        return ImmutableInterlocked.TryRemove(ref _idToSourceDocument, key, out _);
    }

    public bool TryGetSourceDocument(TextDocumentIdentifier id, [NotNullWhen(true)] out ISourceDocument? sourceDocument)
    {
        if (_idToSourceDocument.TryGetValue(id.Uri.ToUri(), out var info))
        {
            sourceDocument = info.Document;
            return true;
        }
        else
        {
            sourceDocument = null;
            return false;
        }
    }

    public void ApplyDocumentChanges(
        OptionalVersionedTextDocumentIdentifier id, 
        IEnumerable<TextDocumentContentChangeEvent> changes)
    {
        if (_idToSourceDocument.TryGetValue(id.Uri.ToUri(), out var info)
            && info.Version == id.Version)
        {
            var text = info.Document.Text;

            foreach (var change in changes)
            {
                if (change.Range != null)
                {
                    var startPosition = info.Document.Text.GetTextPosition(change.Range.Start.Line, change.Range.Start.Character);
                    var endPosition = info.Document.Text.GetTextPosition(change.Range.End.Line, change.Range.End.Character);

                    if (startPosition >= 0 && startPosition <= text.Length
                        && endPosition >= 0 && endPosition <= text.Length)
                    {
                        if (change.Text.Length == 0)
                        {
                            text = text.Remove(startPosition, endPosition - startPosition);
                        }
                        else
                        {
                            text = text.Substring(0, startPosition)
                                + change.Text
                                + text.Substring(endPosition, text.Length - endPosition);
                        }
                    }
                }
                else
                {
                    text = change.Text;
                }
            }
        }
    }
}
