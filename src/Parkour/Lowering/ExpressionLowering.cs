using Parkour.Binding;
using Parkour.Semantics;

namespace Parkour.Lowering;

public class ExpressionLowering
{
    /// <summary>
    /// The lowered expression.
    /// </summary>
    public Expression Expression { get; }

    /// <summary>
    /// Any diagnostics introduced during lowering.
    /// </summary>
    public ImmutableList<Diagnostic> Diagnostics { get; }

    public ExpressionLowering(
        Expression expression,
        ImmutableList<Diagnostic> diagnostics)
    {
        this.Expression = expression;
        this.Diagnostics = diagnostics;
    }
}
