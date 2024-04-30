using Parkour;
using Parkour.Binding;

namespace Parkour.Lowering;

/// <summary>
/// Lowers declarations and expression to a form compatible with CLR modules.
/// </summary>
public abstract class Lowerer
{
    /// <summary>
    /// Lowers the bound symbols, declarations and expressions.
    /// </summary>
    public abstract DeclarationLowering Lower(DeclarationBinding binding);

    /// <summary>
    /// Lowers the bound expression.
    /// </summary>
    public abstract ExpressionLowering Lower(ExpressionBinding boundExpression);
}
