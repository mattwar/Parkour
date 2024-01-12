namespace Parkour.Syntax;

/// <summary>
/// A <see cref="SyntaxElement"/> that represents a single/terminal lexigraphical element.
/// </summary>
public class SyntaxToken 
    : SyntaxElement, ISyntaxToken
{
    /// <summary>
    /// The trivia text between the previous token and this token.
    /// This may include whitespace characters or and/or other items as determined by the language parsed.
    /// </summary>
    public string Trivia { get; }

    /// <summary>
    /// The text of the token.
    /// </summary>
    public string Text { get; }

    public SyntaxToken(string kind, string trivia, string text, Diagnostic? diagnostic = null)
        : base(kind, diagnostic)
    {
        Trivia = trivia;
        Text = text;
    }

    public SyntaxToken(LexicalToken token)
        : this(token.Kind, token.Trivia, token.Text, token.Diagnostic)
    {
    }

    public override int Length => Trivia.Length + Text.Length;
    public override int TextStart => Start + Trivia.Length;

    public override string ToString()
    {
        if (this.Trivia.Length == 0)
            return this.Text;
        else if (this.Text.Length == 0)
            return this.Trivia;
        else
            return $"{Trivia}{Text}";
    }

    /// <summary>
    /// Gets the next <see cref="SyntaxToken"/> in lexical order.
    /// </summary>
    public SyntaxToken? GetNextToken(bool includeZeroLengthTokens = false)
    {
        return GetNextToken(null, this, includeZeroLengthTokens);
    }

    /// <summary>
    /// Gets the previous <see cref="SyntaxToken"/> in lexical orer.
    /// </summary>
    public SyntaxToken? GetPreviousToken(bool includeZeroLengthTokens = false)
    {
        return GetPreviousToken(null, this, includeZeroLengthTokens);
    }
}
