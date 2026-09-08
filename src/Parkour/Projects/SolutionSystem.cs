namespace Parkour.Projects;

public abstract class SolutionSystem
{
    /// <summary>
    /// Gets the path to the solution file that contains the specified project.
    /// </summary>
    public abstract string GetSolutionPath(string projectPath);

    public abstract Task<SolutionInfo?> LoadSolutionAsync(string solutionPath);
}