namespace Parkour.Projects;

/// <summary>
/// Represents the immutable information about a solution.
/// This record can be extended.
/// </summary>
public record SolutionInfo(
    string Path, 
    ImmutableList<ProjectInfo> Projects,
    Settings Settings
    );