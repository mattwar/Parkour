using System.Globalization;

namespace Parkour.Parsers;

/// <summary>
/// A <see cref="ILexicalToken"> implementation that retains trivia as part of the token.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{Kind}: {Text}")]
public struct LexicalToken : ILexicalToken
{
    /// <summary>
    /// The kind of token, depends on the language.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// The whitespace appearing before the token and after the previous token
    /// </summary>
    public string Trivia { get; }

    /// <summary>
    /// The text of the token
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// An optional diagnostic produced by parsing this token.
    /// This is typically used for errors like an unrecognized character or an incomplete/incorrect grammer.
    /// </summary>
    public Diagnostic? Diagnostic { get; }

    public LexicalToken(
        string kind, 
        string trivia, 
        string text, 
        Diagnostic? diagnostic = null)
    {
        Kind = kind ?? "";
        Trivia = trivia ?? "";
        Text = text ?? "";
        Diagnostic = diagnostic;
    }

    /// <summary>
    /// The total length of the token.
    /// </summary>
    public int Length => Trivia.Length + Text.Length;
}
