using Parkour.Syntax;

namespace Parkour;

public class Diagnostic
{
    public string Code { get; }
    public string Severity { get; }
    public string Message { get; }

    private readonly int _start;
    public bool HasLocation => _start >= 0;
    public int Start => HasLocation ? _start : 0;
    public int Length { get; }

    private Diagnostic(string code, string severity, string message, int start, int length)
    {
        Code = code;
        Severity = severity;
        Message = message;
        _start = start;
        Length = length;
    }

    public Diagnostic(string code, string severity, string message)
        : this(code, severity, message, -1, 0)
    {
    }

    public Diagnostic(string message)
        : this("", "Error", message, -1, 0)
    {
    }

    public Diagnostic WithLocation(int start, int length)
    {
        return new Diagnostic(Code, Severity, Message, start, length);
    }

    public Diagnostic WithLocation(SyntaxElement? element) =>
        element != null ? WithLocation(element.TextStart, element.TextLength) : this;
}
