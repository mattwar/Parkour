using System.Collections.Immutable;
using Parkour;
using Parkour.Services;
using Parkour.Symbols;

namespace Tiny;

public class TinyServices : CompilationServices
{
    public TinyServices(TinyCompilation compilation, ISourceDocument document)
        : base(compilation, document)
    {
    }

    protected override ImmutableDictionary<string, string>? GetTokenClassifications() =>
        _tokenClassifications;

    private static ImmutableDictionary<string, string> _tokenClassifications =
        new Dictionary<string, string>
        {
            { TinyTokenKinds.IdentifierToken, ClassificationKinds.Name },
            { TinyTokenKinds.NumberToken, ClassificationKinds.NumericLiteral },
            { TinyTokenKinds.StringToken, ClassificationKinds.StringLiteral },
            { TinyTokenKinds.OpenParenToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.CloseParenToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.PlusToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.DashToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.AsteriskToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.SlashToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.EqualToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.EqualEqualToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.NotEqualToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.GreaterThanToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.GreaterThanEqualToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.LessThanToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.LessThanEqualToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.AndToken, ClassificationKinds.Keyword },
            { TinyTokenKinds.OrToken, ClassificationKinds.Keyword },
            { TinyTokenKinds.NotToken, ClassificationKinds.Keyword },
            { TinyTokenKinds.LetToken, ClassificationKinds.Keyword },
            { TinyTokenKinds.ColonToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.CommaToken, ClassificationKinds.Punctuation },
            { TinyTokenKinds.QuestionMarkToken, ClassificationKinds.Punctuation }
        }.ToImmutableDictionary();

    public static StandardServices Create(string text, SymbolTable externalSymbols)
    {
        var compilation = new TinyCompilation(text, externalSymbols);       
        return new TinyServices(compilation, compilation.Documents[0]);
    }
}
