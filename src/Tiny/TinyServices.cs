using System.Collections.Immutable;
using Parkour;
using Parkour.Services;
using Parkour.Symbols;

namespace Tiny;

public class TinyServices : CompilationServices
{
    public TinyServices(ICompilation compilation, ISourceDocument document)
        : base(compilation, document)
    {
    }

    private static ImmutableList<string> _allClassifications =
        new string[]
        {
            ClassificationKinds.Name,
            ClassificationKinds.Number,
            ClassificationKinds.String,
            ClassificationKinds.Punctuation,
            ClassificationKinds.Keyword

        }.ToImmutableList();

    public override ImmutableList<string> GetClassificationKinds() =>
        _allClassifications;

    private static ImmutableDictionary<string, string> _tokenClassifications =
        new Dictionary<string, string>
        {
            { TinyTokenKinds.IdentifierToken, ClassificationKinds.Name },
            { TinyTokenKinds.NumberToken, ClassificationKinds.Number },
            { TinyTokenKinds.StringToken, ClassificationKinds.String },
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

    protected override string GetTokenClassification(ISyntaxToken token)
    {
        return _tokenClassifications.TryGetValue(token.Kind, out var classification)
            ? classification
            : ClassificationKinds.Text;
    }
}
