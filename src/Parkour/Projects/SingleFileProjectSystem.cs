namespace Parkour.Projects;

using Parkour.Text;

/// <summary>
/// A <see cref="ProjectSystem"/> for single source file projects without a project file.
/// </summary>
public class SingleFileProjectSystem : ProjectSystem
{
    public SingleFileProjectSystem()
    {
    }

    public override string GetProjectPath(string documentPath)
    {
        // There is no project file, so use the document path
        return documentPath;
    }

    public override async Task<ProjectInfo?> LoadProjectAsync(string projectPath)
    {
        // project path is the path to the single source file
        var documentPath = projectPath;
        var docInfo = await LoadDocumentAsync(documentPath).ConfigureAwait(false);
        return CreateProject(projectPath, [docInfo], [], Settings.Default);
    }

    public override Task SaveProjectAsync(ProjectInfo project)
    {
        // there is no project file, nothing to save
        return Task.CompletedTask;
    }
}
