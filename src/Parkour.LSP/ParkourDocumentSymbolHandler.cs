using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

using Parkour;
namespace Parkour.LSP;

using Services;

#if false
internal class ParkourDocumentHighlightHandler : IDocumentHighlightHandler
{
    private readonly ParkourDocumentServicesManager _servicesManager;
    private readonly ParkourLanguage _language;

    public ParkourDocumentHighlightHandler(
        ParkourDocumentServicesManager servicesManager,
        ParkourLanguage language)
    {
        _servicesManager = servicesManager;
        _language = language;
    }

    public Task<DocumentHighlightContainer?> Handle(DocumentHighlightParams request, CancellationToken cancellationToken)
    {
        var highlights = new List<DocumentHighlight>();

        if (_servicesManager.TryGetDocumentService<IClassificationService>(request.TextDocument, out var service))
        {
            var result = service.GetClassifications(0, service.Document.Text.Length, ServiceOptions.Default, cancellationToken);

            foreach (var item in result.Classifications)
            {
                if (item.Classification == ClassificationKinds.Trivia)
                    continue;

                if (GetHighlightKind(item.Classification) is DocumentHighlightKind kind)
                {
                    var text = service.Document.Text.Substring(item.Start, item.Length);
                    var startPos = service.Document.Text.GetLinePosition(item.Start);
                    var endPos = service.Document.Text.GetLinePosition(item.Start + item.Length);
                    var range = new Range(new Position(startPos.Line, startPos.Offset), new Position(endPos.Line, endPos.Offset));
                    highlights.Add(
                        new DocumentHighlight()
                        {
                            Range = range,
                            Kind = kind
                        });
                }
            }
        }

        return highlights;
    }

    protected virtual DocumentHighlightKind GetHighlightKind(string classification)
    {
        return classification switch
        {
            ClassificationKinds.Text => DocumentHighlightKind.Text,
            ClassificationKinds.Keyword => DocumentHighlightKind.Text,
            //public const string Punctuation = nameof(Punctuation);
            //public const string Trivia = nameof(Trivia);
            //public const string Comment = nameof(Comment);
            //public const string Annotation = nameof(Annotation);
            //public const string Literal = nameof(Literal);
            //public const string NumericLiteral = nameof(NumericLiteral);
            //public const string StringLiteral = nameof(StringLiteral);
            //public const string DateTimeLiteral = nameof(DateTimeLiteral);
            //public const string Intrinsic = nameof(Intrinsic);
            //public const string Name = nameof(Name);
            //public const string TypeName = nameof(TypeName);
            //public const string TypeMemberName = nameof(TypeMemberName);
            //public const string TypeParameterName = nameof(TypeParameterName);
            //public const string InterfaceName = nameof(InterfaceName);
            //public const string NamespaceName = nameof(NamespaceName);
            //public const string MethodName = nameof(MethodName);
            //public const string FieldName = nameof(FieldName);
            //public const string PropertyName = nameof(PropertyName);
            //public const string EventName = nameof(EventName);
            _ => null
        };
    }

    public DocumentHighlightRegistrationOptions GetRegistrationOptions(
        DocumentHighlightCapability capability, ClientCapabilities clientCapabilities)
    {
        SemanticTokensCapability cap;

        return new DocumentHighlightRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(_language.LanguageId);
        }
    }
}

internal class ParkourDocumentSymbolHandler : IDocumentSymbolHandler
{
    private readonly ParkourDocumentServicesManager _servicesManager;
    private readonly ParkourLanguage _language;

    public ParkourDocumentSymbolHandler(
        ParkourDocumentServicesManager servicesManager,
        ParkourLanguage language)
    {
        _servicesManager = servicesManager;
        _language = language;
    }

    public async Task<SymbolInformationOrDocumentSymbolContainer?> Handle(
        DocumentSymbolParams request,
        CancellationToken cancellationToken)
    {
        // defer to background
        await Task.Yield();

        var symbols = new List<SymbolInformationOrDocumentSymbol>();

        if (_servicesManager.TryGetDocumentService<IClassificationService>(request.TextDocument, out var service))
        {
            var result = service.GetClassifications(0, service.Document.Text.Length, ServiceOptions.Default, cancellationToken);

            foreach (var item in result.Classifications)
            {
                if (item.Classification == ClassificationKinds.Trivia)
                    continue;

                if (GetSymbolKind(item.Classification) is SymbolKind kind)
                {
                    var text = service.Document.Text.Substring(item.Start, item.Length);
                    var startPos = service.Document.Text.GetLinePosition(item.Start);
                    var endPos = service.Document.Text.GetLinePosition(item.Start + item.Length);
                    var range = new Range(new Position(startPos.Line, startPos.Offset), new Position(endPos.Line, endPos.Offset));
                    symbols.Add(
                        new DocumentSymbol
                        {
                            Detail = null, // this maybe quick info text
                            Kind = kind,
                            Range = range,  // range wider than just text (maybe includes some trivia)
                            SelectionRange = range, // just the name/text
                            Name = text     // the name of the symbol
                        });
                }
            }
        }

        return symbols;

#if false
        // you would normally get this from a common source that is managed by current open editor, current active editor, etc.
        var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(request)!, cancellationToken).ConfigureAwait(false);
        var lines = content.Split('\n');
        var symbols = new List<SymbolInformationOrDocumentSymbol>();
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var parts = line.Split(' ', '.', '(', ')', '{', '}', '[', ']', ';');
            var currentCharacter = 0;
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    currentCharacter += part.Length + 1;
                    continue;
                }

                symbols.Add(
                    new DocumentSymbol
                    {
                        Detail = part,
                        Deprecated = true,
                        Kind = SymbolKind.Field,
                        Tags = new[] { SymbolTag.Deprecated },
                        Range = new Range(
                            new Position(lineIndex, currentCharacter),
                            new Position(lineIndex, currentCharacter + part.Length)
                        ),
                        SelectionRange =
                            new Range(
                                new Position(lineIndex, currentCharacter),
                                new Position(lineIndex, currentCharacter + part.Length)
                            ),
                        Name = part
                    }
                );
                currentCharacter += part.Length + 1;
            }
        }

        // await Task.Delay(2000, cancellationToken);
        return symbols;
#endif
    }

    protected virtual SymbolKind? GetSymbolKind(string classification)
    {
        return classification switch
        {
            ClassificationKinds.Text => null,
            ClassificationKinds.Keyword => SymbolKind.Null,
    //public const string Punctuation = nameof(Punctuation);
    //public const string Trivia = nameof(Trivia);
    //public const string Comment = nameof(Comment);
    //public const string Annotation = nameof(Annotation);
    //public const string Literal = nameof(Literal);
    //public const string NumericLiteral = nameof(NumericLiteral);
    //public const string StringLiteral = nameof(StringLiteral);
    //public const string DateTimeLiteral = nameof(DateTimeLiteral);
    //public const string Intrinsic = nameof(Intrinsic);
    //public const string Name = nameof(Name);
    //public const string TypeName = nameof(TypeName);
    //public const string TypeMemberName = nameof(TypeMemberName);
    //public const string TypeParameterName = nameof(TypeParameterName);
    //public const string InterfaceName = nameof(InterfaceName);
    //public const string NamespaceName = nameof(NamespaceName);
    //public const string MethodName = nameof(MethodName);
    //public const string FieldName = nameof(FieldName);
    //public const string PropertyName = nameof(PropertyName);
    //public const string EventName = nameof(EventName);
            _ => null
        };
    }


    public DocumentSymbolRegistrationOptions GetRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentSymbolRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(_language.LanguageId)
        };
    }
}
#endif