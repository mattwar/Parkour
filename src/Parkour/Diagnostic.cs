namespace Parkour;

public class Diagnostic
{
    public string Code { get; }
    public string Severity { get; }
    public string Message { get; }
    public ISourceLocation? Location { get; }

    private Diagnostic(string code, string severity, string message, ISourceLocation? location = null)
    {
        Code = code;
        Severity = severity;
        Message = message;
        Location = location;
    }

    public Diagnostic(string message)
        : this("", "Error", message, null)
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

        if (this.Code != null)
            message = $"[{Code}] {message}";

        message = $"{Severity}: {message}";

        if (this.Location != null)
        {
            var linePosition = this.Location.Document.Text.GetLinePosition(this.Location.Start);

            message = $"{linePosition}: ";

            if (this.Location.Document.Name.Length > 0)
                message = $"{this.Location.Document.Name}: {message}";
        }

        return message;
    }
}
