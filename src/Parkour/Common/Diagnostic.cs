using Parkour.Text;

namespace Parkour;

public class Diagnostic
{
    /// <summary>
    /// The code associated with the diagnostic.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// The severity of the diagnostic.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// The message describing the diagnostic.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The source location of the diagnostic.
    /// </summary>
    public ISourceLocation? Location { get; }

    private Diagnostic(string code, DiagnosticSeverity severity, string message, ISourceLocation? location = null)
    {
        Code = code;
        Severity = severity;
        Message = message;
        Location = location;
    }

    public Diagnostic(string message)
        : this("", DiagnosticSeverity.Error(), message, null)
    {
    }

    public Diagnostic WithLocation(ISourceLocation? location)
    {
        if (location == this.Location)
            return this;
        return new Diagnostic(this.Code, this.Severity, this.Message, location);
    }

    public override string ToString()
    {
        var message = this.Message;

        if (this.Code.Length > 0)
            message = $"[{this.Code}] {message}";

        message = $"{this.Severity}: {message}";

        if (this.Location != null)
        {
            var linePosition = this.Location.Document.Text.GetLinePosition(this.Location.Start);

            message = $"({linePosition.Line + 1}, {linePosition.Offset + 1}): ";

            if (this.Location.Document.Name.Length > 0)
                message = $"{this.Location.Document.Name}: {message}";
        }

        return message;
    }
}
