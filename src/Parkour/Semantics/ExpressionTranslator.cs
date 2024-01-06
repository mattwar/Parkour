using System.Linq.Expressions;
using System.Reflection;

namespace Parkour.Semantics;

public sealed class ExpressionTranslator
{
    public ExpressionTranslator()
    {
    }

    public LambdaExpression TranslateLambda(Semantic expression, Type? delegateType = null)
    {
        if (expression is not Semantic.Function function)
            function = SemanticFactory.Function(expression);
        return (LambdaExpression)Translate(function);
    }

    public Expression<TDelegate> TranslateLambda<TDelegate>(Semantic expression)
        where TDelegate : Delegate
    {
        return (Expression<TDelegate>)TranslateLambda(expression, typeof(TDelegate));
    }

    public Expression Translate(Semantic expression)
    {
        if (expression.ContainsUnknowns)
            throw new InvalidOperationException("Cannot translation expression with unknowns");

        switch (expression)
        {
            case Semantic.Block block:
                return TranslateBlock(block);
            case Semantic.Branch branch:
                return TranslateBranch(branch);
            case Semantic.Call call:
                return TranslateCall(call);
            case Semantic.Condition condition:
                return TranslateCondition(condition);
            case Semantic.Constant constant:
                return TranslateConstant(constant);
            case Semantic.Convert convert:
                return TranslateConvert(convert);
            case Semantic.Declaration declaration:
                return TranslateDeclaration(declaration);
            case Semantic.Function function:
                return TranslateFunction(function);
            case Semantic.Label label:
                return TranslateLabel(label);
            case Semantic.Path path:
                return TranslatePath(path);
            case Semantic.Reference rex:
                return TranslateReference(rex);
            case Semantic.While @while:
                return TranslateWhile(@while);
            default:
                throw new InvalidOperationException($"Unhandled semantic type '{expression.GetType().Name}' in {nameof(ExpressionTranslator)}.Translate");
        }
    }

    private Type Translate(Symbol.Type type)
    {
        if (type.RuntimeType != null)
            return type.RuntimeType;
        throw new InvalidOperationException($"Unhandled type '{type.Name}' in {nameof(ExpressionTranslator)}.GetType");
    }

    private Expression TranslateBlock(Semantic.Block block)
    {
        var declarations = block.SelectWhere(e => e is not Semantic.Block && e is not Semantic.Function, e => e is Semantic.Declaration, e => (Semantic.Declaration)e).ToList();
        if (declarations.Count > 0)
        {
            return Expression.Block(
                Translate(block.ResultType),
                declarations.Select(d => DeclareVariable(d.Variable!)), 
                block.Expressions.Select(e => Translate(e)));
        }
        else
        {
            return Expression.Block(
                Translate(block.ResultType), 
                block.Expressions.Select(e => Translate(e)));
        }
    }

    private Expression TranslateCall(Semantic.Call call)
    {
        var calledSymbol = call.CalledSymbol;
        if (calledSymbol == null)
            throw new InvalidOperationException($"Cannot translate call for unknown function");

        switch (calledSymbol)
        {
            case Symbol.Method method:
                if (method.RuntimeMethod is System.Reflection.MethodInfo mi)
                {
                    var instance = call.Expression is Semantic.Path path
                        ? Translate(path.Expression)
                        : null;
                    var arguments = call.Arguments.Select(a => Translate(a)).ToArray();

                    return Expression.Call(instance, mi, arguments);
                }
                break;
            case Symbol.Constructor constructor:
                if (constructor.RuntimeMethod is System.Reflection.ConstructorInfo ci)
                {
                    var arguments = call.Arguments.Select(a => Translate(a)).ToArray();
                    return Expression.New(ci, arguments);
                }
                break;
            case Symbol.Function function:
                {
                    var fn = Translate(call.Expression);
                    var arguments = call.Arguments.Select(a => Translate(a)).ToArray();
                    return Expression.Invoke(fn, arguments);
                }
        }

        throw new InvalidOperationException($"Cannot translate call for symbol '{calledSymbol.Name}'");
    }

    private Expression TranslateBranch(Semantic.Branch branch)
    {
        var label = GetCurrentBranchTarget(branch.TargetName);
        if (label == null)
            throw new InvalidOperationException($"No branch target defined for '{branch.TargetName}'");

        if (branch.Expression == null || branch.Expression.ResultType == SymbolModel.Void)
        {
            if (branch.IsBreak)
                return Expression.Break(label);
            else if (branch.IsContinue)
                return Expression.Continue(label);
            else if (branch.IsReturn)
                return Expression.Return(label);
            else
                return Expression.Goto(label);
        }
        else
        {
            var expression = Translate(branch.Expression);
            if (branch.IsBreak)
                return Expression.Break(label, expression);
            else if (branch.IsContinue)
                return Expression.Continue(label);
            else if (branch.IsReturn)
                return Expression.Return(label, expression);
            else
                return Expression.Goto(label, expression);
        }
    }

    private Expression TranslateCondition(Semantic.Condition condition) =>
        Expression.Condition(
            Translate(condition.Test), 
            Translate(condition.WhenTrue), 
            Translate(condition.WhenFalse));

    private Expression TranslateConstant(Semantic.Constant constant) =>
        Expression.Constant(constant.Value, Translate(constant.ResultType));

    private Expression TranslateConvert(Semantic.Convert convert) =>
        Expression.Convert(
            Translate(convert.Expression),
            Translate(convert.ConvertedType));

    private Expression TranslateDeclaration(Semantic.Declaration declaration)
    {
        var initializer = Translate(declaration.Initializer);
        var variable = GetVariable(declaration.Variable!);
        return Expression.Block(
            Expression.Assign(variable, initializer),
            variable);
    }

    private Expression TranslateFunction(Semantic.Function function)
    {
        var oldReturnTarget = GetCurrentBranchTarget("return");
        var oldDelegateType = _currentFunctionDelegateType;
        _currentFunctionDelegateType = null;

        var parameters = function.Symbol!.Parameters.Select(p => DeclareVariable(p)).ToArray();

        Expression lambdaBody;

        if (function.ResultType == SymbolModel.Void)
        {
            var returnTarget = Expression.Label("return");
            SetCurrentBranchTarget("return", returnTarget);
            var body = Translate(function.Body);
            lambdaBody = Expression.Block(body, Expression.Label(returnTarget));
        }
        else
        {
            var returnType = Translate(function.ResultType);
            var returnTarget = Expression.Label(returnType, "return");
            SetCurrentBranchTarget("return", returnTarget);

            var body = Translate(function.Body);

            lambdaBody = Expression.Block(
                returnType, 
                body, 
                Expression.Return(returnTarget, body),
                Expression.Label(returnTarget, Expression.Default(returnType)));
        }

        SetCurrentBranchTarget("return", oldReturnTarget);
        _currentFunctionDelegateType = oldDelegateType;

        if (_currentFunctionDelegateType != null)
            return Expression.Lambda(_currentFunctionDelegateType, lambdaBody, parameters);
        return Expression.Lambda(lambdaBody, parameters);
    }

    private Expression TranslateLabel(Semantic.Label label)
    {
        var target = GetCurrentBranchTarget(label.Name);
        if (target == null)
            throw new InvalidOperationException($"No branch label defined for '{label.Name}'");
        return Expression.Label(target);
    }

    private Expression TranslatePath(Semantic.Path path)
    {
        switch (path.Reference.ReferencedSymbol)
        {
            case Symbol.Property prop when prop.RuntimeProperty is PropertyInfo pi:
                if (prop.IsStatic)
                {
                    return Expression.Property(null, pi);
                }
                else
                {
                    var expression = Translate(path.Expression);
                    return Expression.Property(expression, pi);
                }
            case Symbol.Field field when field.RuntimeField is FieldInfo fi:
                if (field.IsStatic)
                {
                    return Expression.Field(null, fi);
                }
                else
                {
                    var expression = Translate(path.Expression);
                    return Expression.Field(expression, fi);
                }
            default:
                if (path.ReferencedSymbol == null)
                    throw new InvalidOperationException($"The reference has no symbol");
                throw new InvalidOperationException($"Unhandled symbol '{path.Reference.ReferencedSymbol?.Name ?? "?"}' in {nameof(ExpressionTranslator)}.{nameof(TranslatePath)}");
        }
    }

    private Expression TranslateReference(Semantic.Reference rex)
    {
        switch (rex.ReferencedSymbol)
        {
            case Symbol.Variable _:
            case Symbol.Parameter _:
                return GetVariable(rex.ReferencedSymbol);
            case null:
                throw new InvalidOperationException("Reference has no symbol");
            default:
                throw new InvalidOperationException($"Unhandled symbol '{rex.ReferencedSymbol.Name}' in {nameof(ExpressionTranslator)}.{nameof(TranslateReference)}");
        }
    }

    private Expression TranslateWhile(Semantic.While @while)
    {
        var outerLoopContinue = GetCurrentBranchTarget("continue");
        var outerLoopBreak = GetCurrentBranchTarget("break");

        var test = Translate(@while.Test);

        var loopContinue = Expression.Label("continue");
        var loopBreak = Expression.Label("break");

        SetCurrentBranchTarget("continue", loopContinue);
        SetCurrentBranchTarget("break", loopBreak);

        var body = Translate(@while.Body);

        var loop =
            Expression.Loop(
                Expression.Condition(test, body, Expression.Goto(loopBreak)),
                loopBreak, loopContinue);

        SetCurrentBranchTarget("continue", outerLoopContinue);
        SetCurrentBranchTarget("break", outerLoopBreak);

        return loop;
    }

    private Type? _currentFunctionDelegateType;

    private Dictionary<string, LabelTarget?> _currentBranchTargets =
        new Dictionary<string, LabelTarget?>();


    public LabelTarget? GetCurrentBranchTarget(string targetName)
    {
        _currentBranchTargets.TryGetValue(targetName, out var labelTarget);
        return labelTarget;
    }

    public LabelTarget? SetCurrentBranchTarget(string targetName, LabelTarget? labelTarget)
    {
        _currentBranchTargets[targetName] = labelTarget;
        return labelTarget;
    }

    private Dictionary<Symbol, ParameterExpression> _variableMap =
        new Dictionary<Symbol, ParameterExpression>();

    private ParameterExpression DeclareVariable(Symbol symbol)
    {
        var variable = Expression.Parameter(Translate(symbol.GetResultType()), symbol.Name);
        _variableMap[symbol] = variable;
        return variable;
    }
    private ParameterExpression GetVariable(Symbol symbol)
    {
        if (_variableMap.TryGetValue(symbol, out var variable))
            return variable;

        throw new InvalidOperationException($"cannot find variable for symbol '{symbol.Name}'");
    }

    private Expression? TranslateIntrinsic(Semantic.Call c)
    {
        if (c.CalledSymbol is Symbol.IntrinsicFunction intrinsic)
        {
            switch (intrinsic.RelatedFunction.Name)
            {
                case nameof(Operators.Add):
                    return Expression.Add(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.Subtract):
                    return Expression.Subtract(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.Multiply):
                    return Expression.Multiply(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.Divide):
                    return Expression.Divide(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.Remainder):
                    return Expression.Modulo(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.Negate):
                    return Expression.Negate(Translate(c.Arguments[0]));
                case nameof(Operators.BitwiseAnd):
                    return Expression.And(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.BitwiseOr):
                    return Expression.Or(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.BitwiseNot):
                    return Expression.Not(Translate(c.Arguments[0]));
                case nameof(Operators.Equal):
                    return Expression.Equal(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.NotEqual):
                    return Expression.NotEqual(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.LessThan):
                    return Expression.LessThan(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.LessThanOrEqual):
                    return Expression.LessThanOrEqual(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.GreaterThan):
                    return Expression.GreaterThan(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.GreaterThanOrEqual):
                    return Expression.GreaterThanOrEqual(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.LogicalAnd):
                    return Expression.And(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.LogicalAndAlso):
                    return Expression.AndAlso(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.LogicalOr):
                    return Expression.Or(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.LogicalOrElse):
                    return Expression.OrElse(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
                case nameof(Operators.LogicalNot):
                    return Expression.Not(Translate(c.Arguments[0]));
            }
        }

        return null;
    }
}
