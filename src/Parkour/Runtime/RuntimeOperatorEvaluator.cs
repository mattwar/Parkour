using System.Numerics; 

namespace Parkour;

/// <summary>
/// Evaluator for common runtime operators.
/// </summary>
public abstract class RuntimeOperatorEvaluator
{
    /// <summary>
    /// Invokes the unary operator with the operand value.
    /// </summary>
    public abstract object? Evaluate(RuntimeOperator op, object? value, bool isChecked = true);

    /// <summary>
    /// Invokes the binary operator with the the left & right argument values.
    /// </summary>
    public abstract object? Evaluate(RuntimeOperator op, object? left, object? right, bool isChecked = true);

    /// <summary>
    /// Converts the value to the type.
    /// </summary>
    public abstract object? Convert(Type type, object? value, bool isChecked = true);
}
