using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;
using VSLSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Parkour.LSP;

using System;

public abstract class LanguageServer //: IDisposable
{
    private JsonRpc _rpc;

    public LanguageServer(
        Stream inputStream, 
        Stream outputStream)
    {
        // Create the JSON-RPC connection
        var messageHandler = new HeaderDelimitedMessageHandler(outputStream, inputStream);
        _rpc = new JsonRpc(messageHandler, this);

        // Add custom target for handling requests
        _rpc.AddLocalRpcTarget(this);
    }

    /// <summary>
    /// Starts the language server and returns a task that completes when the
    /// language server is disposed.
    /// </summary>
    public virtual Task Run()
    {
        _rpc.StartListening();
        return _rpc.Completion;
    }

    public void Stop()
    {
        _rpc.Dispose();
    }

    //public virtual void Dispose()
    //{
    //    _rpc?.Dispose();
    //}

    #region Requests and notifications sent from client

    /// <summary>
    /// A request from the client to initialize the server and get its capabilities.
    /// </summary>
    [JsonRpcMethod(Methods.InitializeName)]
    public virtual Task<InitializeResult> OnInitializeAsync(InitializeParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new InitializeResult
        {
            Capabilities = new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Full,
                    Save = new SaveOptions { IncludeText = true }
                },
                //SemanticTokensOptions = semanticTokenOptions,
                //CompletionProvider = completionOptions,
                //HoverProvider = true,
                //DocumentFormattingProvider = true,
                //DocumentRangeFormattingProvider = true,
            }
        });
    }

    /// <summary>
    /// A notifcation from the client when it is ready to recieve notifications and requests from server.
    /// </summary>
    [JsonRpcMethod(Methods.InitializedName)]
    public virtual Task OnInitializedAsync(InitializedParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A request from the client to register new capabilities at runtime.
    /// </summary>
    [JsonRpcMethod(Methods.ClientRegisterCapabilityName)]
    public virtual Task OnClientRegisterCapabilityAsync(RegistrationParams @params, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A request from the client to unregister capabilities at runtime.
    /// </summary>
    [JsonRpcMethod(Methods.ClientUnregisterCapabilityName)]
    public virtual Task OnClientUnregisterCapabilityAsync(UnregistrationParams @params, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A request from the client to shutdown the server, but not to exit its process.
    /// </summary>
    [JsonRpcMethod(Methods.ShutdownName)]
    public virtual Task OnShutdownAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A request from the client for the server to exit its process.
    /// </summary>
    [JsonRpcMethod(Methods.ExitName)]
    public virtual Task OnExitAsync()
    {
        // dispose the RPC now, which will complete the run task
        this.Stop();
        return Task.CompletedTask;
    }

    /// <summary>
    /// A notification from the client when it has opened a document.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
    public virtual Task OnTextDocumentOpenedAsync(DidOpenTextDocumentParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// An notification from the client when a document's text has changed.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
    public virtual Task OnTextDocumentDidChangeAsync(DidChangeTextDocumentParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A notification from the client before it saves a document.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentWillSaveName)]
    public virtual Task OnTextDocumentWillSaveAsync(WillSaveTextDocumentParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A notification from the client after it has saved a document.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDidSaveName)]
    public virtual Task OnTextDocumentDidSaveAsync(DidSaveTextDocumentParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A notification from the client after it has closed a document.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDidCloseName)]
    public virtual Task OnTextDocumentDidCloseAsync(DidCloseTextDocumentParams @params)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// A request sent from client to get semantic tokens for an entire document.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentSemanticTokensFullName)]
    public virtual Task<SemanticTokens?> OnTextDocumentTokensFullAsync(SemanticTokensParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<SemanticTokens?>(null);
    }

    /// <summary>
    /// A request from client to get semantic tokens for a range of a document.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentSemanticTokensRangeName)]
    public virtual Task<SemanticTokens?> OnTextDocumentSemanticTokensRangeAsync(SemanticTokensRangeParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<SemanticTokens?>(null);
    }

    /// <summary>
    /// Sends a request to the client to invalidate all semantic tokens in the workspace and to refresh them from the server.
    /// </summary>
    public async Task SendWorkspaceSemanticTokensRefresh()
    {
        // Send notification to client (out-of-band, not a response)
        await _rpc.NotifyWithParameterObjectAsync("workspace/semanticTokens/refresh")
            .ConfigureAwait(false);
    }

    /// <summary>
    /// A request from the client to get the completion items for a document at a given position.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentCompletionName)]
    public virtual Task<SumType<VSLSP.CompletionItem[], VSLSP.CompletionList>?> OnTextDocumentCompletionAsync(CompletionParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<SumType<VSLSP.CompletionItem[], VSLSP.CompletionList>?>(null);
    }

    /// <summary>
    /// A request from the client to resolve additional information for a selected completion item.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentCompletionResolveName)]
    public virtual Task<VSLSP.CompletionItem> OnTextDocumentCompletionResolveAsync(VSLSP.CompletionItem @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(@params);
    }

    /// <summary>
    /// A request from the client to get the hover text information for a document at a given position.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentHoverName)]
    public virtual Task<Hover?> OnTextDocumentHoverAsync(TextDocumentPositionParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<Hover?>(null);
    }

    /// <summary>
    /// A request from the client to get the signature help information for a document at a given position.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentSignatureHelpName)]
    public virtual Task<SignatureHelp?> OnTextDocumentSignatureHelpAsync(SignatureHelpParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<SignatureHelp?>(null);
    }

    /// <summary>
    /// A request from the client to get the locations where a symbol (at a given position) is referenced.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentReferencesName)]
    public virtual Task<Location[]?> OnTextDocumentReferencesAsync(ReferenceParams @params, CancellationToken cancellationToken)
    {
        // Return reference locations
        return Task.FromResult<Location[]?>(null);
    }

    /// <summary>
    /// A request from the client to get the corresponding ranges to highlight for a given text position.
    /// For example, matching braces or all occurrences of a symbol.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName)]
    public virtual Task<DocumentHighlight[]?> OnTextDocumentHighlightAsync(TextDocumentPositionParams @params, CancellationToken cancellationToken)
    {
        // Return document highlights
        return Task.FromResult<DocumentHighlight[]?>(null);
    }

    /// <summary>
    /// A request from the client for all the symbols defined in a document.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName)]
    public virtual Task<SumType<DocumentSymbol[], SymbolInformation[]>?> OnTextDocumentSymbolAsync(DocumentSymbolParams @params, CancellationToken cancellationToken)
    {
        // Return document symbols
        return Task.FromResult<SumType<DocumentSymbol[], SymbolInformation[]>?>(null);
    }

    /// <summary>
    /// A request from the client for all code actions applicable for a given document and range.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentCodeActionName)]
    public virtual Task<SumType<Command, CodeAction>[]?> OnTextDocumentCodeActionAsync(CodeActionParams @params, CancellationToken cancellationToken)
    {
        // Return code actions
        return Task.FromResult<SumType<Command, CodeAction>[]?>(null);
    }

    /// <summary>
    /// A request from the client to resolve additional information for a specific code action.
    /// This typically applies the code action and returns with the edits to be made.
    /// </summary>
    [JsonRpcMethod(Methods.CodeActionResolveName)]
    public virtual Task<CodeAction> OnCodeActionResolveAsync(CodeAction @params, CancellationToken cancellationToken)
    {
        // Resolve code action details
        return Task.FromResult(@params);
    }

    [JsonRpcMethod(Methods.TextDocumentCodeLensName)]
    public virtual Task<CodeLens[]?> OnTextDocumentCodeLensAsync(CodeLensParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<CodeLens[]?>(null);
    }

    [JsonRpcMethod(Methods.CodeLensResolveName)]
    public virtual Task<CodeLens> OnCodeLenseResolveAsync(CodeLens @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(@params);
    }

    [JsonRpcMethod(Methods.TextDocumentDocumentLinkName)]
    public virtual Task<DocumentLink[]?> OnTextDocumentDocumentLinkAsync(DocumentLinkParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<DocumentLink[]?>(null);
    }

    [JsonRpcMethod(Methods.DocumentLinkResolveName)]
    public virtual Task<DocumentLink> OnDocumentLinkResolveAsync(DocumentLink @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(@params);
    }

    [JsonRpcMethod(Methods.TextDocumentDocumentColorName)]
    public virtual Task<ColorInformation[]> OnTextDocumentDocumentColorAsync(DocumentColorParams @params, CancellationToken cancellation)
    {
        return Task.FromResult(Array.Empty<ColorInformation>());
    }

    /// <summary>
    /// A request from the client to format an entire document.
    /// </summary>
    [JsonRpcMethod(Methods.TextDocumentFormattingName)]
    public virtual Task<TextEdit[]?> OnTextDocumentFormattingAsync(DocumentFormattingParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<TextEdit[]?>(null);
    }

    [JsonRpcMethod(Methods.TextDocumentRangeFormattingName)]
    public virtual Task<TextEdit[]?> OnTextDocumentRangeFormattingAsync(DocumentRangeFormattingParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<TextEdit[]?>(null);
    }

    [JsonRpcMethod(Methods.TextDocumentOnTypeFormattingName)]
    public virtual Task<TextEdit[]?> OnTextDocumentOnTypeFormattingAsync(DocumentOnTypeFormattingParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<TextEdit[]?>(null);
    }

    [JsonRpcMethod(Methods.TextDocumentRenameName)]
    public virtual Task<WorkspaceEdit?> OnTextDocumentRenameAsync(RenameParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<WorkspaceEdit?>(null);
    }

    [JsonRpcMethod(Methods.TextDocumentFoldingRangeName)]
    public virtual Task<FoldingRange[]?> OnTextDocumentFoldingAsync(FoldingRangeParams @params, CancellationToken cancellationToken)
    {
        // Return folding ranges
        return Task.FromResult<FoldingRange[]?>(null);
    }

    [JsonRpcMethod(Methods.TextDocumentLinkedEditingRangeName)]
    public virtual Task<LinkedEditingRanges?> GetLinkedEditingRangesAsync(LinkedEditingRangeParams @params, CancellationToken cancellationToken)
    {
        // Return linked editing ranges
        return Task.FromResult<LinkedEditingRanges?>(null);
    }

    /// <summary>
    /// Gets the workspace symbols matching the query.
    /// </summary>
    [JsonRpcMethod(Methods.WorkspaceSymbolName)]
    public virtual Task<SymbolInformation[]?> OnWorkspaceSymbolsAsync(WorkspaceSymbolParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<SymbolInformation[]?>(null);
    }

    [JsonRpcMethod("workspaceSymbol/resolve")]
    public virtual Task<SymbolInformation> OnWorkspaceSymbolResolveAsync(SymbolInformation @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(@params);
    }

    [JsonRpcMethod(Methods.WorkspaceDidChangeConfigurationName)]
    public virtual Task OnWorkspaceDidChangeConfigurationAsync(VSLSP.DidChangeConfigurationParams @params)
    {
        return Task.CompletedTask;
    }

    [JsonRpcMethod("workspace/workspaceFolder")]
    public virtual Task<WorkspaceFolder[]?> OnWorkspaceWorkspaceFoldersAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<WorkspaceFolder[]?>(null);
    }

    [JsonRpcMethod("workspace/didChangeWorkspaceFolders")]
    public virtual Task OnWorkspaceDidChangeWorkspaceFoldersAsync(DidChangeWorkspaceFolderParams @params)
    {
        return Task.CompletedTask;
    }

    [JsonRpcMethod(Methods.WorkspaceExecuteCommandName)]
    public virtual Task<object?> OnWorkspaceExecuteCommandAsync(ExecuteCommandParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult<object?>(null);
    }

    [JsonRpcMethod(Methods.WorkspaceDidChangeWatchedFilesName)]
    public virtual Task OnWorkspaceDidChangeWatchedFilesAsync(DidChangeWatchedFilesParams @params, CancellationToken cancellationToken)
    {
        // Handle watched files change
        return Task.CompletedTask;
    }

    #endregion

    #region Requests and notifications sent from server to client
    /// <summary>
    /// Sends a request to the client to show a message to the user.
    /// Does not wait for a reply.
    /// </summary>
    public Task SendWindowShowMessageAsync(ShowMessageParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.WindowShowMessageName,
            @params);
    }

    /// <summary>
    /// Sends a request to the client to show a message and wait for a resonse.
    /// </summary>
    public Task<MessageActionItem> SendWindowShowMessageRequestAsync(ShowMessageRequestParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<MessageActionItem>(
            Methods.WindowShowMessageRequestName,
            @params,
            cancellationToken
            );
    }

    /// <summary>
    /// Sends a request to the client to log message.
    /// </summary>
    public Task SendWindowLogMessageAsync(LogMessageParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.WindowLogMessageName,
            @params);
    }

    /// <summary>
    /// Sends a request to the client to log telemetry event.
    /// </summary>
    public Task SendTelemetryEventAsync(object data)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.TelemetryEventName,
            data);
    }

    public Task SendSetTraceAsync(SetTraceParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            "$/setTrace",
            @params);
    }

    public Task SendLogTraceAsync(LogTraceParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            "$/logTrace",
            @params);
    }

    /// <summary>
    /// Sends a request to the client to set workspace configurations.
    /// </summary>
    [JsonRpcMethod(Methods.WorkspaceConfigurationName)]
    public Task<object?[]> SendWorkspaceConfigurationAsync(ConfigurationParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<object?[]>(
            Methods.WorkspaceConfigurationName,
            @params,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the client to publish diagnostics for a document.
    /// </summary>
    public Task SendTextDocumentPublishDiagnosticsAsync(PublishDiagnosticParams @params)
    {
        return _rpc.NotifyWithParameterObjectAsync(
            Methods.TextDocumentPublishDiagnosticsName,
            @params
        );
    }

    /// <summary>
    /// Sends a request to the client to apply workspace edits.
    /// </summary>
    public Task<ApplyWorkspaceEditResponse> SendWorkspaceApplyEdit(ApplyWorkspaceEditParams @params, CancellationToken cancellationToken)
    {
        return _rpc.InvokeWithParameterObjectAsync<ApplyWorkspaceEditResponse>(
            Methods.WorkspaceApplyEditName,
            @params,
            cancellationToken
        );
    }
    #endregion
}

public record SetTraceParams(string value);

public record LogTraceParams(string message, string? verbose);

public record WorkspaceFolder(Uri uri, string name);

public record WorkspaceFolderChangeEvent(WorkspaceFolder[] added, WorkspaceFolder[] removed);

public record DidChangeWorkspaceFolderParams(WorkspaceFolderChangeEvent @event);