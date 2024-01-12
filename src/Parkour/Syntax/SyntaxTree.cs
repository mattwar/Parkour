using Parkour.Analysis;

namespace Parkour.Syntax;
using Parsing;

public partial class SyntaxTree
{
    public string Name { get; }
    public string Text { get; }

    private readonly Parser<LexicalToken> _parser;
    private readonly LexicalToken[] _tokens;
    private readonly SyntaxElement _root;

    public SyntaxTree(
        string name,
        string text,
        Parser<LexicalToken> parser,
        LexicalToken[] tokens,
        SyntaxElement root)
    {
        Name = name;
        Text = text;
        _parser = parser;
        _tokens = tokens;
        _root = root;
        // assigns this tree to root, and freezes all syntax elements.
        _root.SetTree(this);
    }

    public SyntaxElement Root => _root;

    /// <summary>
    /// Gets the set of terms that could appear at the text position
    /// </summary>
    public IReadOnlyList<string> GetNextTermsAt(int textPosition)
    {
        var terms = new List<string>();

        var tokenIndex = GetTokenIndex(textPosition, out var textOffsetInToken);
        if (tokenIndex >= 0)
        {
            // affinitize with previous token?
            if (tokenIndex < _tokens.Length && tokenIndex > 0 && textOffsetInToken == 0)
            {
                var token = _tokens[tokenIndex];
                if (token.Trivia.Length > 0
                    || token.Text.Length > 0 && char.IsLetter(token.Text[0]))
                {
                    tokenIndex--;
                }
            }

            var input = _tokens.AsSpan();
            var nextParsers = _parser.GetNextParsers(
                input, tokenIndex,
                (parser, afterMissing) => parser.Term != null && !afterMissing);
            terms.AddRange(nextParsers.Select(p => p.Term).ToHashSet()!);
        }

        return terms.ToArray();
    }

    /// <summary>
    /// Returns the index of the token that contains the text position.
    /// </summary>
    public int GetTokenIndex(int textPosition)
    {
        return GetTokenIndex(textPosition, out _);
    }

    /// <summary>
    /// Returns the index of the token withing the set of all tokens in lexical order,
    /// that contains the text position.
    /// </summary>
    public int GetTokenIndex(int textPosition, out int textOffsetInToken)
    {
        if (textPosition < Text.Length)
        {
            for (int i = 0; i < _tokens.Length; i++)
            {
                var token = _tokens[i];
                if (textPosition < token.Length)
                {
                    textOffsetInToken = token.Length - textPosition;
                    return i;
                }

                textPosition -= token.Length;
            }
        }
        else if (textPosition == Text.Length && _tokens.Length > 0)
        {
            textOffsetInToken = _tokens[^1].Length;
            return _tokens.Length - 1;
        }

        textOffsetInToken = default;
        return -1;
    }

    private IReadOnlyList<Diagnostic>? _diagnostics;

    /// <summary>
    /// Gets a list of all the diagnostics produced during parsing.
    /// </summary>
    public IReadOnlyList<Diagnostic> GetDiagnostics()
    {
        if (_diagnostics == null)
        {
            var list = new List<Diagnostic>();

            SyntaxElement.WalkElements(this.Root, fnAfter: (element) =>
            {
                if (element.Diagnostic != null)
                    list.Add(element.Diagnostic.WithLocation(element));
            });

            _diagnostics = list;
        }

        return _diagnostics;
    }
}

