namespace Parkour.LSP;

using Parkour.Projects;
using Parkour.Text;

public record OpenedDocumentInfo(string Path, EditString Text, int Version)
    : LoadedDocumentInfo(Path, Text);
