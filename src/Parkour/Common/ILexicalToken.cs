namespace Parkour;

/// <summary>
/// A token produced by a lexical parser.
/// </summary>
public interface ILexicalToken
{
    /// <summary>
    /// The text of the token.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// The kind of the token.
    /// </summary>
    string Kind { get; }
}