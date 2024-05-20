using System.Numerics;

namespace Parkour.Lowering;

using Semantics;
using Symbols;
using static Semantics.SemanticFactory;

public class ConstFoldingLowerer
{
#if false
    /// <summary>
    /// Folds constants across operators.
    /// </summary>
    public static SemanticElement FoldConstants(
        SemanticElement element, SymbolTable symbols)
    {
        if (element is OperatorExpression ox)
        {
            if (ox.Arguments.Count == 1)
            {
                if (ox.Arguments[0] is ConstantExpression c0
                    && c0.Value is object v0)
                {
                }
            }
            else if (ox.Arguments.Count == 2)
            {
                if (ox.Arguments[0] is ConstantExpression c0
                    && c0.Value is object v0
                    && ox.Arguments[1] is ConstantExpression c1
                    && c1.Value is object v1)
                {
                    // convert values to same type
                    v0 = System.Convert.ChangeType(c0.Value, wider);
                    v1 = System.Convert.ChangeType(c1.Value, wider);

                    var result = v0 switch
                    {
                        byte v => DoBinary(ox.Kind, v, (byte)v1),
                        sbyte v => DoBinary(ox.Kind, v, (sbyte)v1),
                        short v => DoBinary(ox.Kind, v, (short)v1),
                        ushort v => DoBinary(ox.Kind, v, (ushort)v1),
                        int v => DoBinary(ox.Kind, v, (int)v1),
                        uint v => DoBinary(ox.Kind, v, (uint)v1),
                        long v => DoBinary(ox.Kind, v, (long)v1),
                        ulong v => DoBinary(ox.Kind, v, (ulong)v1),
                        float v => DoBinary(ox.Kind, v, (float)v1),
                        double v => DoBinary(ox.Kind, v, (double)v1),
                        decimal v => DoBinary(ox.Kind, v, (decimal)v1),
                        _ => null
                    };

                    return result == null
                        ? element
                        : Constant(result);

                }
            }
        }

        return element;
    }


    private static object? DoUnary<T>(string kind, T operand)
        where T : INumber<T>
    {
    }

    private static object? DoBinary<T>(string kind, T left, T right)
        where T : INumber<T>
    {
        switch (kind)
        {
            case OperatorKind.Add:
                return left + right;
            case OperatorKind.Subtract:
                return left - right;
            case OperatorKind.Multiply:
                return left * right;
            case OperatorKind.Divide:
                return left / right;
            case OperatorKind.Remainder:
                return left % right;
            case OperatorKind.Equal:
                return left == right;
            case OperatorKind.NotEqual:
                return left != right;
            case OperatorKind.GreaterThan:
                return left > right;
            case OperatorKind.GreaterThanOrEqual:
                return left >= right;
            case OperatorKind.LessThan:
                return left < right;
            case OperatorKind.LessThanOrEqual:
                return left <= right;
            case OperatorKind.BitwiseAnd:
                return left & right;
            case OperatorKind.BitwiseOr:
                return T.
            default:
                return T.Zero;
        }
    }
#endif
}