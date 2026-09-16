namespace Parkour;

/// <summary>
/// An simple implmementation of <see cref="ISourceDocument">
/// </summary>
public record SourceDocument(string Name, string Text) : ISourceDocument;
