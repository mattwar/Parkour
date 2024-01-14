namespace Parkour;

public interface ISyntaxTree
{
    /// <summary>
    /// The document this <see cref="SyntaxTree"/> is sourced from.
    /// </summary>
    public ISourceDocument Document { get; }

    /// <summary>
    /// The root element of this <see cref="ISyntaxTree"/>
    /// </summary>
    public ISyntaxElement Root { get; }

    /// <summary>
    /// A means to discover annotations related the parsing of this syntax tree.
    /// </summary>
    public IAnnotationSource Annotations { get; }

    /// <summary>
    /// The collected syntax diagnostics found in this <see cref="SyntaxTree"/>
    /// </summary>
    public ImmutableList<Diagnostic> Diagnostics { get; }
}