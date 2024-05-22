namespace Parkour.Parsers;

[System.Diagnostics.DebuggerDisplay("{Kind}: {Text}")]
public struct LexicalToken : ILexicalToken
{
    public string Kind { get; }
    public string Trivia { get; }
    public string Text { get; }
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

    public int Length => Trivia.Length + Text.Length;
}
