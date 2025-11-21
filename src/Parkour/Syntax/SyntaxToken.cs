namespace Parkour.Syntax;

/// <summary>
/// A <see cref="SyntaxElement"/> that represents a single/terminal lexigraphical element.
/// </summary>
public abstract record SyntaxToken(string Trivia, string Text, Diagnostic? Diagnostic = null)
    : SyntaxElement(Diagnostic), ISyntaxToken
{
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