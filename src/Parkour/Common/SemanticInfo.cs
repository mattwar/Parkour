namespace Parkour;

/// <summary>
/// Semantic information associated with a text position.
/// </summary>
public record SemanticInfo(
    string Kind,
    ISymbol? ResultType,
    ISymbol? ReferencedSymbol
    )
{
    public static readonly SemanticInfo None =
        new SemanticInfo("", null, null);
}