using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;

namespace Parkour.LSP;

using MediatR;
using Services;

public class ParkourSemanticTokensHandler : SemanticTokensHandlerBase
{
    private readonly ParkourDocumentServicesManager _servicesManager;
    private readonly ParkourLanguage _language;

    public ParkourSemanticTokensHandler(
        ParkourDocumentServicesManager servicesManager,
        ParkourLanguage language)
    {
        _servicesManager = servicesManager;
        _language = language;
    }

    protected override Task Tokenize(
        SemanticTokensBuilder builder, 
        ITextDocumentIdentifierParams identifier, 
        CancellationToken cancellationToken)
    {
        if (_servicesManager.TryGetDocumentService<IClassificationService>(identifier.TextDocument, out var service))
        {
            var result = service.GetClassifications(0, service.Document.Text.Length, ServiceOptions.Default, cancellationToken);

            foreach (var classification in result.Classifications)
            {
                var linePos = service.Document.Text.GetLinePosition(classification.Start);
                builder.Push(linePos.Line, linePos.Offset, classification.Length, GetSemanticToken(classification.Classification), Array.Empty<SemanticTokenModifier>());
            }
        }

        return Unit.Task;
    }

    public override async Task<SemanticTokens?> Handle(
        SemanticTokensParams request, 
        CancellationToken cancellationToken)
    {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokens?> Handle(
        SemanticTokensRangeParams request, 
        CancellationToken cancellationToken)
    {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokensFullOrDelta?> Handle(
        SemanticTokensDeltaParams request,
        CancellationToken cancellationToken)
    {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params, 
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
    }

    private SemanticTokensLegend? _legend;

    protected SemanticTokensLegend GetLegend()
    {
        if (_legend == null)
        {
            var tokenTypes = new List<SemanticTokenType>();
            if (_servicesManager.TryGetDefaultDocumentService<IClassificationService>(out var service))
            {
                tokenTypes.AddRange(service.GetClassificationKinds().Select(k => GetSemanticToken(k.ToLower())));
            }

            var tmp = new SemanticTokensLegend()
            {
                TokenModifiers = [],
                TokenTypes = tokenTypes
            };

            Interlocked.CompareExchange(ref _legend, tmp, null);
        }

        return _legend;
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
    {

        return new SemanticTokensRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(_language.LanguageId),
            Legend = GetLegend()
        };
    }

    private ImmutableDictionary<string, SemanticTokenType> _classificationToTokenTypeMap =
        ImmutableDictionary<string, SemanticTokenType>.Empty;

    private SemanticTokenType GetSemanticToken(string classification)
    {
        if (!_classificationToTokenTypeMap.TryGetValue(classification, out var token))
        {
            var tmp = new SemanticTokenType(classification);
            token = ImmutableInterlocked.GetOrAdd(ref _classificationToTokenTypeMap, classification, tmp);
        }

        return token;
    }
}
