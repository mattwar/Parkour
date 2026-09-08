namespace Parkour.Projects;

using Parkour.Text;

/// <summary>
/// Represents the immutable information about a document.
/// This record can be extended for different document kinds.
/// </summary>
public abstract record DocumentInfo(
    string Path
    );

public record UnloadedDocumentInfo(
    string Path
    ) : DocumentInfo(Path);

public record LoadedDocumentInfo(
    string Path,
    EditString Text
    ) : DocumentInfo(Path);

public record FailedDocumentInfo(
    string Path,
    Diagnostic Error
    ) : DocumentInfo(Path);