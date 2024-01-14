
namespace Parkour.Parsing;

/// <summary>
/// An annotation source that finds annotations in the parsing grammar
/// by scanning to the position using the original input.
/// </summary>
public class LexcialAnnotationSource
    : IAnnotationSource
{
    private readonly Parser<LexicalToken> _parser;
    private readonly ImmutableArray<LexicalToken> _tokens;
    private readonly int _textLength;

    public LexcialAnnotationSource(
        Parser<LexicalToken> parser, 
        ImmutableArray<LexicalToken> tokens)
    {
        _parser = parser;
        _tokens = tokens;
        _textLength = tokens.Sum(t => t.Length);
    }

    public void GetAnnotations<TAnnotation>(
        int position, 
        Func<TAnnotation, bool>? filter, 
        List<TAnnotation> annotations)
    {
        var tokenIndex = GetTokenIndex(position, out var textOffsetInToken);
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

            var nextParsers = new List<Parser<LexicalToken>>();
            _parser.GetNextParsers(
                _tokens.AsSpan(), 
                tokenIndex,
                (parser, afterMissing) => 
                    !afterMissing && parser.Annotations.Count > 0,
                nextParsers);

            annotations.AddRange(
                nextParsers
                    .SelectMany(p => p.Annotations)
                    .OfType<TAnnotation>()
                    .Where(ta => filter == null || filter(ta))
                );
        }
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
        if (textPosition < _textLength)
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
        else if (textPosition == _textLength && _tokens.Length > 0)
        {
            textOffsetInToken = _tokens[^1].Length;
            return _tokens.Length - 1;
        }

        textOffsetInToken = default;
        return -1;
    }
}
