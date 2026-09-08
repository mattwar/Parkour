namespace Parkour.Projects;

/// <summary>
/// Represents the immutable information about a project.
/// This record can be extended for different project kinds.
/// </summary>
public record ProjectInfo(
    string Path, 
    ImmutableList<DocumentInfo> Documents,
    ImmutableList<DependentProject> DependentProjects,
    Settings Settings
    );
