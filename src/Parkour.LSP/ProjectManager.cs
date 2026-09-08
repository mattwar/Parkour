namespace Parkour.LSP;

using Parkour.Projects;

public class ProjectManager
{
    public ProjectSystem ProjectSystem { get; }

    private ImmutableDictionary<string, Project> _pathToProjectMap =
        ImmutableDictionary<string, Project>.Empty;

    public ProjectManager(ProjectSystem projectSystem)
    {
        this.ProjectSystem = projectSystem;
    }

    public async Task<Project?> GetOrLoadProject(string projectPath)
    {
        if (_pathToProjectMap.TryGetValue(projectPath, out var project))
        {
            return project;
        }
        else
        {
            var projectInfo = await this.ProjectSystem.LoadProjectAsync(projectPath).ConfigureAwait(false);
            if (projectInfo != null)
            {
                project = new Project(projectInfo!);
                ImmutableInterlocked.AddOrUpdate(ref _pathToProjectMap, projectPath, _path => project, (_path, _current) => project);
                return project;
            }
        }
        return null;
    }

    public async Task<Document?> TryGetDocumentAsync(string documentPath)
    {
        var projectPath = this.ProjectSystem.GetProjectPath(documentPath);
        if (_pathToProjectMap.TryGetValue(projectPath, out var project))
        {
            if (project.TryGetDocument(documentPath, out var document))
            {
                return document;
            }
        }
        return null;
    }

    public async Task<Document?> GetOrLoadDocumentAsync(string documentPath)
    {
        var projectPath = this.ProjectSystem.GetProjectPath(documentPath);
        var project = await GetOrLoadProject(projectPath).ConfigureAwait(false);
        if (project != null && project.TryGetDocument(documentPath, out var document))
        {
            return document;
        }
        return null;
    }

    public void UpdateProject(Project project)
    {
        ImmutableInterlocked.AddOrUpdate(ref _pathToProjectMap, project.Info.Path, _ => project, (_, _) => project);
    }
}