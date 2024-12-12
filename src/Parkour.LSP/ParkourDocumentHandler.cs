
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Parkour.LSP;

using Semantics;
using Symbols;

public class ParkourDocumentHandler : TextDocumentSyncHandlerBase
{
    private readonly ILogger<ParkourDocumentHandler> _logger;
    private readonly ILanguageServerConfiguration _configuration;
    private readonly ParkourDocumentManager _documentManager;
    private readonly ParkourLanguage _language;
    private readonly TextDocumentSelector _textDocumentSelector;

    public ParkourDocumentHandler(
        ILogger<ParkourDocumentHandler> logger, 
        ILanguageServerConfiguration configuration,
        ParkourDocumentManager documentManager,
        ParkourLanguage language)
    {
        _logger = logger;
        _configuration = configuration;
        _documentManager = documentManager;
        _language = language;
        _textDocumentSelector = new TextDocumentSelector(new TextDocumentFilter { Pattern = language.DocumentPattern });
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

    public override Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
    {
        _documentManager.ApplyDocumentChanges(notification.TextDocument, notification.ContentChanges);
        //_logger.LogCritical("Critical");
        //_logger.LogDebug("Debug");
        //_logger.LogTrace("Trace");
        //_logger.LogInformation("Hello world!");
        return Unit.Task;
    }

    public override async Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token)
    {
        var configuaration = await _configuration.GetScopedConfiguration(notification.TextDocument.Uri, token).ConfigureAwait(false);
        _documentManager.AddOrUpdateDocument(notification.TextDocument);
        return Unit.Value;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token)
    {
        if (_configuration.TryGetScopedConfiguration(notification.TextDocument.Uri, out var disposable))
        {
            disposable.Dispose();
        }

        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken token)
    {
        // document is being saved... do something?
        return Unit.Task;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability, 
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSyncRegistrationOptions()
        {
            DocumentSelector = _textDocumentSelector,
            Change = Change,
            Save = new SaveOptions() { IncludeText = true }
        };
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, _language.LanguageId);
    }
}