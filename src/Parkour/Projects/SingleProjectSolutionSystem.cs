  namespace Parkour.Projects;

/// <summary>
/// A <see cref="SolutionSystem"/> for single-project solutios
/// without a solution file.
/// </summary>
public class SingleProjectSolutionSystem : SolutionSystem
{
    public ProjectSystem ProjectSystem { get; }

    public SingleProjectSolutionSystem(
        ProjectSystem? projectSystem = null)
    {
        this.ProjectSystem = projectSystem ?? new SingleFileProjectSystem();
    }

    public override string GetSolutionPath(string projectPath)
    {
        // there is no solution file, use the project path.
        return projectPath;
    }

    public override async Task<SolutionInfo?> LoadSolutionAsync(string solutionPath)
    {
        var projectPath = solutionPath;
        var project = await this.ProjectSystem.LoadProjectAsync(projectPath);
        if (project is null)
            return null;
        return CreateSolution(solutionPath, [project], project.Settings);
    }

    public virtual SolutionInfo CreateSolution(string path, ImmutableList<ProjectInfo> projects, Settings settings)
    {
        return new SolutionInfo(path, projects, settings);
    }
}