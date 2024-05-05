namespace Parkour.Binding;

using Semantics;

public class ExpressionBinding
{
    /// <summary>
    /// The expression after binding.
    /// </summary>
    public Expression Expression { get; }

    /// <summary>
    /// All diagnostics produced during binding.
    /// </summary>
    public ImmutableList<Diagnostic> Diagnostics { get; }

    public ExpressionBinding(
        Expression boundExpression,
        ImmutableList<Diagnostic> diagnostics)
    {
        this.Expression = boundExpression;
        this.Diagnostics = diagnostics;
    }
}
