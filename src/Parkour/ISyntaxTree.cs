namespace Parkour;

public interface ISyntaxTree
{
    public ISourceDocument Document { get; }
    public ISyntaxElement Root { get; }
    public IAnnotationSource Annotations { get; }
}
