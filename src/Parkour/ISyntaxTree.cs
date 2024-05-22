namespace Parkour;

public interface ISyntaxTree
{
    /// <summary>
    /// The document this <see cref="SyntaxTree"/> is sourced from.
    /// </summary>
    ISourceDocument Document { get; }

    /// <summary>
    /// The root element of this <see cref="ISyntaxTree"/>
    /// </summary>
    ISyntaxElement Root { get; }

    /// <summary>
    /// The collected syntax diagnostics found in this <see cref="SyntaxTree"/>
    /// </summary>
    ImmutableList<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the tokens that overlap with the text range.
    /// </summary>
    ImmutableList<ISyntaxToken> GetTokens(int start, int length);

    /// <summary>
    /// Gets the token at the position.
    /// </summary>
    ISyntaxToken? GetToken(int position);
}