namespace Parkour.Projects;

/// <summary>
/// Represents an immutable document within a project.
/// </summary>
public class Document
{
    /// <summary>
    /// The <see cref="Project"/> that the <see cref="Document"/> is part of.
    /// </summary>
    public Project Project { get; }

    /// <summary>
    /// The document details.
    /// </summary>
    public DocumentInfo Info { get; }

    public Document(
        Project project,
        DocumentInfo info)
    {
        this.Project = project;
        this.Info = info;
    }

    public Document WithInfo(DocumentInfo newInfo)
    {
        return this.Project.AddOrUpdateDocument(newInfo).GetOrAddDocument(newInfo);
    }
}
