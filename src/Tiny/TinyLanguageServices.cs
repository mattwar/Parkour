using Parkour;
using Parkour.Services;
using System.Collections.Immutable;

namespace Tiny;

public class TinyLanguageServices : LanguageServices
{
    public TinyLanguageServices()
        : base("Tiny")
    {
    }

    private static ImmutableList<ClassificationKind> _allClassifications =
        new[]
        {
            ClassificationKind.Name(),
            ClassificationKind.Number(),
            ClassificationKind.String(),
            ClassificationKind.Punctuation(),
            ClassificationKind.Keyword()

        }.ToImmutableList();

    public override ImmutableList<ClassificationKind> SupportedClassificationKinds =>
        _allClassifications;

    protected override ICompilationServiceFactory CreateCompilationServiceFactory(ICompilation compilation)
    {
        return new TinyCompilationServices(this, compilation);
    }
}

public class TinyCompilationServices : CompilationServices
{
    public TinyCompilationServices(ILanguage language, ICompilation compilation)
        : base(language, compilation)
    {
    }

    protected override IDocumentServiceFactory CreateDocumentServiceFactory(ISourceDocument document)
    {
        return new TinyDocumentServices(this.Compilation, document);
    }
}

public class TinyDocumentServices : DocumentServices
{
    public TinyDocumentServices(ICompilation compilation, ISourceDocument document)
        : base(compilation, document)
    {
    }

    protected override ClassificationKind GetTokenClassification(ISyntaxToken token)
    {
        return _tokenClassifications.TryGetValue(token.Kind, out var classification)
            ? classification
            : ClassificationKind.Text();
    }

    private static ImmutableDictionary<string, ClassificationKind> _tokenClassifications =
        new Dictionary<string, ClassificationKind>
        {
            { TinyTokenKinds.IdentifierToken, ClassificationKind.Name() },
            { TinyTokenKinds.NumberToken, ClassificationKind.Number() },
            { TinyTokenKinds.StringToken, ClassificationKind.String() },
            { TinyTokenKinds.OpenParenToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.CloseParenToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.PlusToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.DashToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.AsteriskToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.SlashToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.EqualToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.EqualEqualToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.NotEqualToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.GreaterThanToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.GreaterThanEqualToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.LessThanToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.LessThanEqualToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.AndToken, ClassificationKind.Keyword() },
            { TinyTokenKinds.OrToken, ClassificationKind.Keyword() },
            { TinyTokenKinds.NotToken, ClassificationKind.Keyword() },
            { TinyTokenKinds.LetToken, ClassificationKind.Keyword() },
            { TinyTokenKinds.ColonToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.CommaToken, ClassificationKind.Punctuation() },
            { TinyTokenKinds.QuestionMarkToken, ClassificationKind.Punctuation() }
        }.ToImmutableDictionary();
}
