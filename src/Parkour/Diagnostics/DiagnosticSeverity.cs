namespace Parkour;

public abstract record DiagnosticSeverity(string Name)
{
    public override string ToString() => this.Name;

    public record Error() : DiagnosticSeverity("error");
    public record Warning() : DiagnosticSeverity("warning");

    public record Hint() : DiagnosticSeverity("hint");

    public record Information() : DiagnosticSeverity("information");
}

public static class DiagnosticSeverityExtensions
{
    private static readonly DiagnosticSeverity _error = new DiagnosticSeverity.Error();
    private static readonly DiagnosticSeverity _warning = new DiagnosticSeverity.Warning();
    private static readonly DiagnosticSeverity _hint = new DiagnosticSeverity.Hint();
    private static readonly DiagnosticSeverity _information = new DiagnosticSeverity.Information();

    extension(DiagnosticSeverity)
    {
        public static DiagnosticSeverity Error() => _error;
        public static DiagnosticSeverity Warning() => _warning;
        public static DiagnosticSeverity Hint() => _hint;
        public static DiagnosticSeverity Information() => _information;
    }
}