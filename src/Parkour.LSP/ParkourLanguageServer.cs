using Microsoft.VisualStudio.LanguageServer.Protocol;
using VSLSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Parkour.LSP;

using Parkour.Projects;
using Parkour.Services;
using Parkour.Text;
using System;
using System.Runtime.CompilerServices;

public abstract class ParkourLanguageServer : LanguageServer
{
    public ILanguage Language { get; }

    public ProjectManager ProjectManager { get; }


    public ParkourLanguageServer(
        Stream inputStream,
        Stream outputStream,
        ILanguage language,
        ProjectManager projectManager)
        : base(inputStream, outputStream)
    {
        this.Language = language;
        this.ProjectManager = projectManager;
    }

    protected InitializeParams ClientSettings { get; private set; } = null!;
    protected InitializeResult ServerSettings { get; private set; } = null!;


    public override Task<InitializeResult> OnInitializeAsync(InitializeParams @params, CancellationToken cancellationToken)
    {
        this.ClientSettings = @params;

        this.Language.Factory.TryGetLanguageService<IClassificationLanguageService>(out var classService);

        var classificationKinds = classService != null
            ? classService.SupportedClassificationKinds
            : ImmutableList<ClassificationKind>.Empty;

        this.SemanticTokenMap = classificationKinds
                .Select((kind, index) => new { kind, index })
                .ToImmutableDictionary(k => k.kind, k => k.index);

        var classificationModifiers = classService != null
            ? classService.SupportedClassificationModifiers
            : ImmutableList<ClassificationModifier>.Empty;

        this.SemanticModifierMap = classificationModifiers
                .Select((mod, index) => new { mod, index })
                .ToImmutableDictionary(k => k.mod, k => 1 << k.index);

        var semanticTokenOptions = classificationKinds.Count > 0
            ? new SemanticTokensOptions
            {
                Legend = new SemanticTokensLegend
                {
                    TokenTypes = classificationKinds.Select(GetLspSemanticToken).ToArray(),
                    TokenModifiers = classificationModifiers.Select(GetLspSemanticModifier).ToArray()
                },
                Full = true,
                Range = true
            }
            : null;

        this.Language.Factory.TryGetLanguageService<ICompletionLanguageService>(out var completionService);
        var completionOptions = new CompletionOptions
        {
            ResolveProvider = false, // so far there is no resolve step
            TriggerCharacters = completionService != null
                    ? completionService.GetCompletionDefaults().CommitChars.Select(c => c.ToString()).ToArray()
                    : null
        };

        this.ServerSettings = new InitializeResult
        {
            Capabilities = new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Full,
                    Save = new SaveOptions { IncludeText = true }
                },
                SemanticTokensOptions = semanticTokenOptions,
                CompletionProvider = completionOptions,
                HoverProvider = true,
                DocumentFormattingProvider = true,
                DocumentRangeFormattingProvider = true,
                //SignatureHelpProvider = new SignatureHelpOptions
                //{
                //    TriggerCharacters = new[] { "(", "," }
                //},
                //DefinitionProvider = true,
                //ReferencesProvider = true,
                //DocumentHighlightProvider = true,
                //DocumentSymbolProvider = true,
                //WorkspaceSymbolProvider = true,
                //CodeActionProvider = new CodeActionOptions
                //{
                //    CodeActionKinds = new[]
                //    {
                //        CodeActionKind.QuickFix,
                //        CodeActionKind.Refactor,
                //        CodeActionKind.RefactorExtract,
                //        CodeActionKind.RefactorInline,
                //        CodeActionKind.RefactorRewrite,
                //        CodeActionKind.Source,
                //        CodeActionKind.SourceOrganizeImports
                //    },
                //    ResolveProvider = true
                //},
                //CodeLensProvider = new CodeLensOptions { ResolveProvider = true },
                //DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions
                //{
                //    FirstTriggerCharacter = ";",
                //    MoreTriggerCharacter = new[] { "}", "\n" }
                //},
                //RenameProvider = new RenameOptions { PrepareProvider = true },
                //DocumentLinkProvider = new DocumentLinkOptions { ResolveProvider = true },
                //DocumentColorProvider = true,
                //FoldingRangeProvider = true,
                //DeclarationProvider = true,
                //ExecuteCommandProvider = new ExecuteCommandOptions
                //{
                //    Commands = new[] { "example.command" }
                //},
                //CallHierarchyProvider = true,
                //LinkedEditingRangeProvider = true,
                //TypeDefinitionProvider = true,
                //ImplementationProvider = true,
                //InlayHintProvider = new InlayHintOptions { ResolveProvider = true }
            }
        };

        return Task.FromResult(this.ServerSettings);
    }

    /// <summary>
    /// Maps between documents and their associated service factories.
    /// </summary>
    private readonly ConditionalWeakTable<Document, IDocumentServiceFactory> _documentServicesCache =
        new();

    protected async Task<TService?> TryGetServiceAsync<TService>(
        Document document)
        where TService : class, IDocumentService
    {
        if (!_documentServicesCache.TryGetValue(document, out var factory))
        {
            var tmp = this.CreateDocumentServiceFactory(document);
            if (tmp != null)
                factory = _documentServicesCache.GetOrAdd(document, tmp);
        }

        if (factory != null)
        {
            factory.TryGetDocumentService<TService>(out var service);
            return service;
        }

        return null;
    }

    protected async Task<TService?> GetServiceAsync<TService>(
        string documentPath)
        where TService : class, IDocumentService
    {
        var doc = await this.ProjectManager.TryGetDocumentAsync(documentPath).ConfigureAwait(false);
        if (doc != null)
        {
            return await TryGetServiceAsync<TService>(doc).ConfigureAwait(false);
        }
        return null;
    }
    protected abstract IDocumentServiceFactory? CreateDocumentServiceFactory(
        Document document);

    public override async Task OnTextDocumentOpenedAsync(DidOpenTextDocumentParams args)
    {
        var doc = await this.ProjectManager.GetOrLoadDocumentAsync(args.TextDocument.Uri.LocalPath).ConfigureAwait(false);
        if (doc != null && doc.Info is LoadedDocumentInfo loadedDoc)
        {
            // put document into opened state
            var newDoc = doc.WithInfo(new OpenedDocumentInfo(loadedDoc.Path, loadedDoc.Text, args.TextDocument.Version));
            this.ProjectManager.UpdateProject(newDoc.Project);
        }
    }

    public override async Task OnTextDocumentDidChangeAsync(DidChangeTextDocumentParams args)
    {
        var doc = await this.ProjectManager.TryGetDocumentAsync(args.TextDocument.Uri.LocalPath).ConfigureAwait(false);
        if (doc != null
            && doc.Info is OpenedDocumentInfo openDocInfo
            && args.TextDocument.Version > openDocInfo.Version)
        {
            // apply edits to document's current text
            var edits = args.ContentChanges.Select(c =>
            {
                var pos = openDocInfo.Text.CurrentText.GetTextPosition(c.Range.Start.Line - 1, c.Range.Start.Character - 1);
                return TextEdit.Replacement(pos, c.RangeLength ?? 0, c.Text);
            }).ToImmutableList();

            var newText = openDocInfo.Text.ApplyAll(edits);
            var newDoc = doc.WithInfo(openDocInfo with { Text = newText, Version = args.TextDocument.Version });
            this.ProjectManager.UpdateProject(newDoc.Project);
        }
    }

    public override Task OnTextDocumentWillSaveAsync(WillSaveTextDocumentParams @params)
    {
        return Task.CompletedTask;
    }

    public override async Task OnTextDocumentDidSaveAsync(DidSaveTextDocumentParams args)
    {
        var doc = await this.ProjectManager.TryGetDocumentAsync(args.TextDocument.Uri.LocalPath).ConfigureAwait(false);
        if (doc != null
            && doc.Info is OpenedDocumentInfo openDocInfo)
        {
            var newText = args.Text ?? openDocInfo.Text.CurrentText;
            var newDoc = doc.WithInfo(openDocInfo with { Text = newText });
            this.ProjectManager.UpdateProject(newDoc.Project);
        }
    }

    public override async Task OnTextDocumentDidCloseAsync(DidCloseTextDocumentParams @params)
    {
        var doc = await this.ProjectManager.TryGetDocumentAsync(@params.TextDocument.Uri.LocalPath).ConfigureAwait(false);
        if (doc != null
            && doc.Info is OpenedDocumentInfo openDocInfo)
        {
            // put document back into loaded state (not open).
            var newDoc = doc.WithInfo(new LoadedDocumentInfo(openDocInfo.Path, openDocInfo.Text.CurrentText));
            this.ProjectManager.UpdateProject(newDoc.Project);
        }
    }

    /// <summary>
    /// Call to publish diagnostics for a document to the client.
    /// </summary>
    public Task SendPublishDiagnosticsAsync(string documentPath, IEnumerable<Diagnostic> diagnostics)
    {
        var uri = new Uri(documentPath);

        var publishParams = new PublishDiagnosticParams
        {
            Uri = uri,
            Diagnostics = diagnostics.Select(d => new VSLSP.Diagnostic
            {
                Range = d.Location?.ToLspRange()!,
                Severity = d.Severity.ToLspSeverity(),
                Code = d.Code,
                Source = this.Language.Name,
                Message = d.Message
            }).ToArray()
        };

        return SendTextDocumentPublishDiagnosticsAsync(publishParams);
    }

    public override Task<SemanticTokens?> OnTextDocumentTokensFullAsync(SemanticTokensParams @params, CancellationToken cancellationToken)
    {
        return GetSemanticTokens(@params.TextDocument.Uri.LocalPath, null, cancellationToken);
    }

    public override Task<SemanticTokens?> OnTextDocumentSemanticTokensRangeAsync(SemanticTokensRangeParams @params, CancellationToken cancellationToken)
    {
        return GetSemanticTokens(@params.TextDocument.Uri.LocalPath, @params.Range, cancellationToken);
    }

    private async Task<SemanticTokens?> GetSemanticTokens(string documentPath, VSLSP.Range? range, CancellationToken cancellationToken)
    {
        if (!this.Language.Factory.TryGetLanguageService<IClassificationLanguageService>(out var classService))
            return null;

        var service = await GetServiceAsync<IClassificationDocumentService>(documentPath).ConfigureAwait(false);
        if (service == null)
            return null;

        var textRange = range != null
            ? range.ToTextRange(service.Document.Text)
            : new TextRange(0, service.Document.Text.Length);

        var result = service.GetClassifications(textRange, Settings.Default, cancellationToken);

        var encodedData = EncodeClassifications(service.Document, result.ClassifiedRanges);

        return new SemanticTokens
        {
            Data = encodedData
        };
    }

    public ImmutableDictionary<ClassificationKind, int> SemanticTokenMap { get; private set; } =
        ImmutableDictionary<ClassificationKind, int>.Empty;

    public ImmutableDictionary<ClassificationModifier, int> SemanticModifierMap { get; private set; } =
        ImmutableDictionary<ClassificationModifier, int>.Empty;

    protected ImmutableDictionary<ClassificationKind, int> CreateSemanticTokenMap(ImmutableList<ClassificationKind> kinds) =>
        ImmutableDictionary<ClassificationKind, int>.Empty;

    protected ImmutableDictionary<ClassificationKind, int> CreateSemanticModifierMap(ImmutableList<ClassificationModifier> modifiers) =>
        ImmutableDictionary<ClassificationKind, int>.Empty;

    /// <summary>
    /// Encodes classifications into the LSP semantic tokens format.
    /// </summary>
    private int[] EncodeClassifications(
        ISourceDocument document,
        ImmutableList<ClassifiedTextRange> classifiedRanges)
    {
        var encoded = new List<int>();

        LinePosition prev = default;
        List<TextRange>? segments = null;

        var multiLineSupport = this.ClientSettings.Capabilities?.TextDocument?.SemanticTokens?.MultilineTokenSupport ?? false;

        foreach (var cr in classifiedRanges)
        {
            var start = document.Text.GetLinePosition(cr.Range.Start);
            var end = document.Text.GetLinePosition(cr.Range.End);

            if (start.Line == end.Line
                || multiLineSupport)
            {
                Encode(start, cr.Range.Length, cr.Classification);
            }
            else
            {
                segments ??= new List<TextRange>();
                TextFacts.GetSingleLineSegments(document.Text, cr.Range, segments, includeLineBreaks: false);
                foreach (var sr in segments)
                {
                    var srStart = document.Text.GetLinePosition(sr.Start);
                    Encode(srStart, sr.Length, cr.Classification);
                }
                segments.Clear();
            }

            void Encode(LinePosition pos, int length, Classification classification)
            {
                System.Diagnostics.Debug.Assert(pos.Line >= prev.Line, "unordered classification ranges");

                // delta line
                encoded.Add(
                    encoded.Count == 0  // first position is absolute
                        ? pos.Line + 1
                        : pos.Line - prev.Line
                        );

                // delta character
                if (start.Line == prev.Line)
                {
                    encoded.Add(pos.Offset - prev.Offset);
                }
                else
                {
                    encoded.Add(pos.Offset + 1); // absolute is one based
                }

                // token length
                encoded.Add(length);

                // token type
                var tokenType = this.SemanticTokenMap.TryGetValue(cr.Classification.Kind, out var tt) ? tt : 0;
                encoded.Add(tokenType);

                // token modifier
                int modifier = 0;
                var mods = cr.Classification.Modifiers;
                if (mods.Count > 0)
                {
                    foreach (var mod in mods)
                    {
                        var modifierMask = this.SemanticModifierMap.TryGetValue(mod, out var tm) ? tm : 0;
                        modifier |= modifierMask;
                    }
                }
                encoded.Add(modifier);

                prev = pos;
            }
        }

        return encoded.ToArray();
    }

    protected virtual string GetLspSemanticToken(ClassificationKind kind) =>
        kind.ToString(); // default ToString() implementation already matches LSP token type names

    protected virtual string GetLspSemanticModifier(ClassificationModifier modifier) =>
        modifier.ToString(); // default ToString() implementation already matches LSP token modifier names

    public override async Task<SumType<VSLSP.CompletionItem[], VSLSP.CompletionList>?> OnTextDocumentCompletionAsync(CompletionParams context, CancellationToken cancellationToken)
    {
        var service = await GetServiceAsync<ICompletionDocumentService>(context.TextDocument.Uri.LocalPath).ConfigureAwait(false);
        if (service is not null)
        {
            var position = service.Document.Text.GetTextPosition(context.Position.Line - 1, context.Position.Character - 1);
            var completionResult = service.GetCompletions(position, context.Context?.TriggerCharacter?[0], Settings.Default, cancellationToken);

            var items = completionResult.Items.Select(item => CreateCompletionItem(item, service.Document, position)).ToArray();
            var defaults = completionResult.Defaults != null
                ? new CompletionListItemDefaults
                {
                    CommitCharacters = completionResult.Defaults.CommitChars?.Select(c => c.ToString())?.ToArray()
                }
                : null;

            return
                new CompletionList
                {
                    IsIncomplete = false,
                    Items = items,
                    ItemDefaults = default
                };
        }

        return new VSLSP.CompletionItem[0];
    }

    protected VSLSP.CompletionItem CreateCompletionItem(CompletionItem item, ISourceDocument document, int position)
    {
        var lspPos = position.ToLspPosition(document.Text);

        // we only know the starting point when items are created here
        var range = new VSLSP.Range { Start = lspPos, End = lspPos };

        return new VSLSP.CompletionItem
        {
            Kind = item.Kind.ToLspCompletionKind(),
            Label = item.DisplayText,
            SortText = item.OrderText,
            FilterText = item.MatchText,
            TextEdit = new VSLSP.TextEdit
            {
                NewText = item.InsertionText,
                Range = range
            },
            //Documentation =  // this is hover text for completion item
        };
    }

    public override async Task<Hover?> OnTextDocumentHoverAsync(TextDocumentPositionParams @params, CancellationToken cancellationToken)
    {
        var service = await GetServiceAsync<IHoverTextDocumentService>(@params.TextDocument.Uri.LocalPath).ConfigureAwait(false);
        if (service is not null)
        {
            var position = @params.Position.ToTextPosition(service.Document.Text);
            HoverTextResult result = service.GetHoverText(position, Settings.Default, cancellationToken);

            // TODO: translation for Glyph?
            return new Hover
            {
                Contents = new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = string.Join("  \n", result.Sections.Select(s => s.Text.ToMarkdown()))
                }
            };
        }

        return null;
    }
}