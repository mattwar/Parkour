using Parkour.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Parkour.Projects;

public abstract class ProjectSystem
{
    /// <summary>
    /// Gets the project path for the given a document path.
    /// </summary>
    public abstract string GetProjectPath(string documentPath);

    public abstract Task<ProjectInfo?> LoadProjectAsync(string projectPath);

    public abstract Task SaveProjectAsync(ProjectInfo project);

    public virtual ProjectInfo CreateProject(string path, ImmutableList<DocumentInfo> documents, ImmutableList<DependentProject> dependentProjects, Settings settings) =>
        new ProjectInfo(path, documents, dependentProjects, settings);

    public virtual async Task<DocumentInfo> LoadDocumentAsync(string documentPath)
    {
        try
        {
            var text = await File.ReadAllTextAsync(documentPath).ConfigureAwait(false);
            return new LoadedDocumentInfo(documentPath, text);
        }
        catch (Exception e)
        {
            return new FailedDocumentInfo(documentPath, new Diagnostic($"Failed to load document: {e.Message}"));
        }
    }

    public virtual async Task SaveDocumentAsync(DocumentInfo document)
    {
        if (document is LoadedDocumentInfo openDoc)
        {
            try
            {
                await File.WriteAllTextAsync(document.Path, openDoc.Text.CurrentText).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }
    }

    protected virtual DocumentInfo CreateUnloadedDocument(string path) =>
        new UnloadedDocumentInfo(path);

    protected virtual DocumentInfo CreateLoadedDocument(string path, string text) =>
        new LoadedDocumentInfo(path, text);

    protected virtual DocumentInfo CreateFailedDocument(string path, Diagnostic diagnostic) =>
        new FailedDocumentInfo(path, diagnostic);
}
