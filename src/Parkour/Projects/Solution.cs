namespace Parkour.Projects;
/// <summary>
/// Represents an immutable solution, including its associated projects and documents.
/// </summary>
public class Solution
{
    public SolutionInfo Info { get; }

    private ImmutableDictionary<string, Project> _projectMap =
        ImmutableDictionary<string, Project>.Empty;

    private ImmutableList<Project>? _projects;

    public Solution(SolutionInfo info)
    {
        this.Info = info;
    }

    /// <summary>
    /// Tries to get the project with the associated path.
    /// </summary>
    public bool TryGetProject(string path, [NotNullWhen(true)] out Project? project)
    {
        if (!_projectMap.TryGetValue(path, out project))
        {
            var projectInfo = this.Info.Projects.FirstOrDefault(p => p.Path == path);
            if (projectInfo != null)
            {
                project = GetOrAddProject(projectInfo);
                return true;
            }
        }
        project = null;
        return false;
    }

    /// <summary>
    /// Gets or adds a project with the associated info.
    /// </summary>
    public Project GetOrAddProject(ProjectInfo info)
    {
        if (!_projectMap.TryGetValue(info.Path, out var project))
        {
            project = ImmutableInterlocked.GetOrAdd(ref _projectMap, info.Path, new Project(info));
        }
        return project;
    }

    /// <summary>
    /// The list of projects in the solution.
    /// </summary>
    public ImmutableList<Project> Projects
    {
        get
        {
            if (_projects == null)
            {
                var tmp = this.Info.Projects.Select(GetOrAddProject).ToImmutableList();
                Interlocked.CompareExchange(ref _projects, tmp, null);
            }
            return _projects;
        }
    }

    public Solution AddOrUpdateProject(ProjectInfo newProjectInfo)
    {
        var existingInfo = this.Info.Projects.FirstOrDefault(p => p.Path == newProjectInfo.Path);
        if (existingInfo == null)
        {
            return new Solution(this.Info with 
            { 
                Projects = this.Info.Projects.Add(newProjectInfo) 
            });
        }
        else if (existingInfo != newProjectInfo)
        {
            return new Solution(this.Info with
            {
                Projects = this.Info.Projects.Select(p => p == existingInfo ? newProjectInfo : p).ToImmutableList()
            });
        }
        else
        {
            return this;
        }
    }

    public Solution RemoveProject(ProjectInfo projectInfo)
    {
        if (this.Info.Projects.Contains(projectInfo))
        {
            return new Solution(this.Info with
            {
                Projects = this.Info.Projects.Remove(projectInfo)
            });
        }
        else
        {
            return this;
        }
    }
}
