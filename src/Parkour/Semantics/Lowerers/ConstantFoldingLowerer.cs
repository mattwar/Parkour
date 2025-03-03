namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Constant folds all intrinsic operators and conversions if possible
/// </summary>
public class ConstantFoldingLowerer : PartialLowerer
{
    private readonly RuntimeOperators _operators;

    public ConstantFoldingLowerer(RuntimeOperators operators)
    {
        _operators = operators;
    }

    public static readonly PartialLowerer Instance =
        new ConstantFoldingLowerer(StandardRuntimeOperators.Instance);

    public override ImmutableList<SemanticElement> Lower(
        ImmutableList<SemanticElement> elements,
        SymbolTable symbols)
    {
        return elements.RewriteAll<SemanticElement, Expression>(ex =>
        {
            // intrinsic operators have operator symbols, not methods
            if (ex is OperatorExpression op
                && op.OperatorSymbol is OperatorSymbol)
            {
                if (op.Arguments.Count == 1
                    && op.Arguments[0] is ConstantExpression unaryArg)
                {
                    try
                    {
                        var result = _operators.Invoke(op.Kind, unaryArg.Value);
                        if (result == null || symbols.GetTypeSymbol(result.GetType()) == op.ResultType)
                        {
                            return new ConstantExpression(result, op.Location)
                                .WithResultType(op.ResultType);
                        }
                    }
                    finally
                    {
                    }
                }
                else if (op.Arguments.Count == 2
                    && op.Arguments[0] is ConstantExpression arg0
                    && op.Arguments[1] is ConstantExpression arg1)
                {
                    try
                    {
                        var result = _operators.Invoke(op.Kind, arg0.Value, arg1.Value);
                        if (result == null || symbols.GetTypeSymbol(result.GetType()) == op.ResultType)
                        {
                            return new ConstantExpression(result, op.Location)
                                .WithResultType(op.ResultType);
                        }
                    }
                    finally
                    {
                    }
                }
            }
            else if (ex is ConvertExpression cv
                && cv.ConversionSymbol == null  // not converted via method call
                && cv.Expression is ConstantExpression cvx
                && symbols.TryGetType(cv.ResultType, out var convertToType))
            {
                try
                {
                    var result = _operators.Convert(convertToType, cvx.Value);
                    return new ConstantExpression(result, cv.Location)
                            .WithResultType(cv.ResultType);
                }
                finally
                {
                }
            }

            return ex;
        });
    }
}
