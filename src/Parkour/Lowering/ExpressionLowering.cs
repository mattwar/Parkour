using Parkour.Binding;
using Parkour.Semantics;

namespace Parkour.Lowering;

public class ExpressionLowering
{
    /// <summary>
    /// The input <see cref="ExpressionBinding"/> for the lowering
    /// </summary>
    public ExpressionBinding Binding { get; }

    /// <summary>
    /// The lowered expression.
    /// </summary>
    public Expression Expression { get; }

    public ExpressionLowering(
        ExpressionBinding binding,
        Expression expression)
    {
        this.Binding = binding;
        this.Expression = expression;
    }
}
