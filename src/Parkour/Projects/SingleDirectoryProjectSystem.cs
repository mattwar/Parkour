namespace Parkour.Projects;

/// <summary>
/// A <see cref="ProjectSystem"/> for all the matching files in a single directory.
/// </summary>
public class SingleDirectoryProjectSystem : ProjectSystem
{
    private readonly string _searchPattern;

    public SingleDirectoryProjectSystem(
        string searchPattern)
    {
        _searchPattern = searchPattern;
    }

    public override string GetProjectPath(string documentPath)
    {
        return Path.GetDirectoryName(documentPath) ?? "";
    }

    public override async Task<ProjectInfo?> LoadProjectAsync(string projectPath)
    {
        var documentPath = projectPath;

        var files = Directory.GetFiles(projectPath, _searchPattern);
        var documentTasks = files.Select(LoadDocumentAsync).ToArray();
        var documents = await Task.WhenAll(documentTasks).ConfigureAwait(false);
           
        var projectInfo = CreateProject(
            projectPath,
            documents.ToImmutableList()!,
            ImmutableList<DependentProject>.Empty,
            Settings.Default
            );

        return projectInfo;
    }
     
    public override Task SaveProjectAsync(ProjectInfo project)
    {
        // no project file, nothing to save
        throw new NotImplementedException();
    }
}