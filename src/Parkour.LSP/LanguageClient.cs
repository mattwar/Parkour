using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;
using VSLSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Parkour.LSP;

public abstract class LanguageClient : IDisposable
{
    private readonly JsonRpc _rpc;

    public LanguageClient(
        Stream inputStream,
        Stream outputStream)
    {
        // Create the JSON-RPC connection
        var messageHandler = new HeaderDelimitedMessageHandler(outputStream, inputStream);
        _rpc = new JsonRpc(messageHandler, this);

        // Add custom target for handling requests
        _rpc.AddLocalRpcTarget(this);
    }

    public virtual void Start()
    {
        _rpc.StartListening();
    }

    public virtual void Dispose()
    {
        _rpc?.Dispose();
    }

    #region Requests and notifications sent from server (received by client)

    /// <summary>
    /// A notification from the server to show a message to the user.
    /// </summary>
    [JsonRpcMethod(Methods.WindowShowMessageName)]
    public virtual Task OnWindowShowMessageAsync(ShowMessageParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A request from the server to show a message and get a response from the user.
    /// </summary>
    [JsonRpcMethod(Methods.WindowShowMessageRequestName)]
    public virtual Task<MessageActionItem> OnWindowShowMessageRequestAsync(ShowMessageRequestParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new MessageActionItem());
    }
 
    /// <summary>
    /// A notification from the server to log a message.
    /// </summary>
    [JsonRpcMethod(Methods.WindowLogMessageName)]
    public virtual Task OnWindowLogMessageAsync(LogMessageParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A notification from the server to log a telemetry event.
    /// </summary>
    [JsonRpcMethod(Methods.TelemetryEventName)]
    public virtual Task OnTelemetryEventAsync(object data)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A notification from the server to set trace level.
    /// </summary>
    [JsonRpcMethod("$/setTrace")]
    public virtual Task OnSetTraceAsync(SetTraceParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A notification from the server to log trace information.
    /// </summary>
    [JsonRpcMethod("$/logTrace")]
    public virtual Task OnLogTraceAsync(LogTraceParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A notification from the server to publish diagnostics for a document.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentPublishDiagnosticsName)]
    public virtual Task OnTextDocumentPublishDiagnosticsAsync(PublishDiagnosticParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A request from the server to apply workspace edits.
    /// </summary>
    [JsonRpcMethod(Methods.WorkspaceApplyEditName)]
    public virtual Task<ApplyWorkspaceEditResponse> OnWorkspaceApplyEditAsync(ApplyWorkspaceEditParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ApplyWorkspaceEditResponse { Applied = false });
    }

    /// <summary>
    /// A notification from the server to refresh semantic tokens.
    /// </summary>
    [JsonRpcMethod("workspace/semanticTokens/refresh")]
    public virtual Task OnWorkspaceSemanticTokensRefreshAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Requests and notifications sent from client to server

    /// <summary>
    /// Sends a request to the server to initialize and get its capabilities.
    /// </summary>
    public Task<InitializeResult> SendInitializeAsync(InitializeParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<InitializeResult>(
            Methods.InitializeName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a notification to the server that the client is ready to receive notifications and requests.
    /// </summary>
    public Task SendInitializedAsync(InitializedParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.InitializedName,
            @params);
    }

    /// <summary>
    /// Sends a request to the server to register new capabilities at runtime.
    /// </summary>
    public Task SendClientRegisterCapabilityAsync(RegistrationParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync(
            Methods.ClientRegisterCapabilityName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to unregister capabilities at runtime.
    /// </summary>
    public Task SendClientUnregisterCapabilityAsync(UnregistrationParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync(
            Methods.ClientUnregisterCapabilityName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to shutdown (but not exit).
    /// </summary>
    public Task SendShutdownAsync()
    {
        return _rpc.InvokeAsync(Methods.ShutdownName);
    }

    /// <summary>
    /// Sends a notification to the server to exit its process.
    /// </summary>
    public Task SendExitAsync()
    {
        return _rpc.NotifyAsync(Methods.ExitName);
    }

    /// <summary>
    /// Sends a notification to the server that a document has been opened.
    /// </summary>
    public Task SendTextDocumentDidOpenAsync(DidOpenTextDocumentParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.TextDocumentDidOpenName,
            @params);
    }

    /// <summary>
    /// Sends a notification to the server that a document's text has changed.
    /// </summary>
    public Task SendTextDocumentDidChangeAsync(DidChangeTextDocumentParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.TextDocumentDidChangeName,
            @params);
    }

    /// <summary>
    /// Sends a notification to the server before saving a document.
    /// </summary>
    public Task SendTextDocumentWillSaveAsync(WillSaveTextDocumentParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.TextDocumentWillSaveName,
            @params);
    }

    /// <summary>
    /// Sends a notification to the server that a document has been saved.
    /// </summary>
    public Task SendTextDocumentDidSaveAsync(DidSaveTextDocumentParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.TextDocumentDidSaveName,
            @params);
    }

    /// <summary>
    /// Sends a notification to the server that a document has been closed.
    /// </summary>
    public Task SendTextDocumentDidCloseAsync(DidCloseTextDocumentParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.TextDocumentDidCloseName,
            @params);
    }

    /// <summary>
    /// Sends a request to the server to get semantic tokens for an entire document.
    /// </summary>
    public Task<SemanticTokens?> SendTextDocumentSemanticTokensFullAsync(SemanticTokensParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<SemanticTokens?>(
            Methods.TextDocumentSemanticTokensFullName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get semantic tokens for a range of a document.
    /// </summary>
    public Task<SemanticTokens?> SendTextDocumentSemanticTokensRangeAsync(SemanticTokensRangeParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<SemanticTokens?>(
            Methods.TextDocumentSemanticTokensRangeName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get completion items for a document at a given position.
    /// </summary>
    public Task<SumType<VSLSP.CompletionItem[], VSLSP.CompletionList>?> SendTextDocumentCompletionAsync(CompletionParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<SumType<VSLSP.CompletionItem[], VSLSP.CompletionList>?>(
            Methods.TextDocumentCompletionName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to resolve additional information for a selected completion item.
    /// </summary>
    public Task<VSLSP.CompletionItem> SendTextDocumentCompletionResolveAsync(VSLSP.CompletionItem @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<VSLSP.CompletionItem>(
            Methods.TextDocumentCompletionResolveName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get hover text information for a document at a given position.
    /// </summary>
    public Task<Hover?> SendTextDocumentHoverAsync(TextDocumentPositionParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<Hover?>(
            Methods.TextDocumentHoverName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get signature help information for a document at a given position.
    /// </summary>
    public Task<SignatureHelp?> SendTextDocumentSignatureHelpAsync(SignatureHelpParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<SignatureHelp?>(
            Methods.TextDocumentSignatureHelpName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get the locations where a symbol is referenced.
    /// </summary>
    public Task<Location[]?> SendTextDocumentReferencesAsync(ReferenceParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<Location[]?>(
            Methods.TextDocumentReferencesName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get document highlights for a given text position.
    /// </summary>
    public Task<DocumentHighlight[]?> SendTextDocumentDocumentHighlightAsync(TextDocumentPositionParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<DocumentHighlight[]?>(
            Methods.TextDocumentDocumentHighlightName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get all symbols defined in a document.
    /// </summary>
    public Task<SumType<DocumentSymbol[], SymbolInformation[]>?> SendTextDocumentDocumentSymbolAsync(DocumentSymbolParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<SumType<DocumentSymbol[], SymbolInformation[]>?>(
            Methods.TextDocumentDocumentSymbolName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get code actions for a given document and range.
    /// </summary>
    public Task<SumType<Command, CodeAction>[]?> SendTextDocumentCodeActionAsync(CodeActionParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<SumType<Command, CodeAction>[]?>(
            Methods.TextDocumentCodeActionName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to resolve additional information for a specific code action.
    /// </summary>
    public Task<CodeAction> SendCodeActionResolveAsync(CodeAction @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<CodeAction>(
            Methods.CodeActionResolveName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get code lenses for a document.
    /// </summary>
    public Task<CodeLens[]?> SendTextDocumentCodeLensAsync(CodeLensParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<CodeLens[]?>(
            Methods.TextDocumentCodeLensName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to resolve additional information for a code lens.
    /// </summary>
    public Task<CodeLens> SendCodeLensResolveAsync(CodeLens @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<CodeLens>(
            Methods.CodeLensResolveName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get document links.
    /// </summary>
    public Task<DocumentLink[]?> SendTextDocumentDocumentLinkAsync(DocumentLinkParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<DocumentLink[]?>(
            Methods.TextDocumentDocumentLinkName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to resolve additional information for a document link.
    /// </summary>
    public Task<DocumentLink> SendDocumentLinkResolveAsync(DocumentLink @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<DocumentLink>(
            Methods.DocumentLinkResolveName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get document colors.
    /// </summary>
    public Task<ColorInformation[]> SendTextDocumentDocumentColorAsync(DocumentColorParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<ColorInformation[]>(
            Methods.TextDocumentDocumentColorName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to format an entire document.
    /// </summary>
    public Task<TextEdit[]?> SendTextDocumentFormattingAsync(DocumentFormattingParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<TextEdit[]?>(
            Methods.TextDocumentFormattingName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to format a range of a document.
    /// </summary>
    public Task<TextEdit[]?> SendTextDocumentRangeFormattingAsync(DocumentRangeFormattingParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<TextEdit[]?>(
            Methods.TextDocumentRangeFormattingName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to format on type.
    /// </summary>
    public Task<TextEdit[]?> SendTextDocumentOnTypeFormattingAsync(DocumentOnTypeFormattingParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<TextEdit[]?>(
            Methods.TextDocumentOnTypeFormattingName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to rename a symbol.
    /// </summary>
    public Task<WorkspaceEdit?> SendTextDocumentRenameAsync(RenameParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<WorkspaceEdit?>(
            Methods.TextDocumentRenameName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get folding ranges for a document.
    /// </summary>
    public Task<FoldingRange[]?> SendTextDocumentFoldingRangeAsync(FoldingRangeParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<FoldingRange[]?>(
            Methods.TextDocumentFoldingRangeName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get linked editing ranges.
    /// </summary>
    public Task<LinkedEditingRanges?> SendTextDocumentLinkedEditingRangeAsync(LinkedEditingRangeParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<LinkedEditingRanges?>(
            Methods.TextDocumentLinkedEditingRangeName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to get workspace symbols matching a query.
    /// </summary>
    public Task<SymbolInformation[]?> SendWorkspaceSymbolAsync(WorkspaceSymbolParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<SymbolInformation[]?>(
            Methods.WorkspaceSymbolName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server to execute a command.
    /// </summary>
    public Task<object?> SendWorkspaceExecuteCommandAsync(ExecuteCommandParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<object?>(
            Methods.WorkspaceExecuteCommandName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a notification to the server that configuration has changed.
    /// </summary>
    public Task SendWorkspaceDidChangeConfigurationAsync(DidChangeConfigurationParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.WorkspaceDidChangeConfigurationName,
            @params);
    }

    /// <summary>
    /// Sends a notification to the server that watched files have changed.
    /// </summary>
    public Task SendWorkspaceDidChangeWatchedFilesAsync(DidChangeWatchedFilesParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.WorkspaceDidChangeWatchedFilesName,
            @params);
    }

    #endregion
}