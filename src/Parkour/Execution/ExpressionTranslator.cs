using L=System.Linq.Expressions;
using System.Reflection;

namespace Parkour.Execution;
using Binding;
using Semantics;
using Symbols;

/// <summary>
/// Translates <see cref="Expression"/> to LINQ expressions.
/// </summary>
public sealed class ExpressionTranslator
{
    public ExpressionTranslator()
    {
    }

    public L.LambdaExpression TranslateToLambda(LambdaExpression expression, Type? delegateType = null)
    {
        return (L.LambdaExpression)Translate(expression);
    }

    public L.Expression<TDelegate> TranslateToLambda<TDelegate>(LambdaExpression expression)
        where TDelegate : Delegate
    {
        return (L.Expression<TDelegate>)TranslateToLambda(expression, typeof(TDelegate));
    }

    public L.Expression Translate(Expression expression)
    {
        if (expression.ContainsUnknowns)
            throw new InvalidOperationException("Cannot translation expression with unknowns");

        switch (expression)
        {
            case BlockExpression block:
                return TranslateBlock(block);
            case BranchExpression branch:
                return TranslateBranch(branch);
            case CallExpression call:
                return TranslateCall(call);
            case ConditionExpression condition:
                return TranslateCondition(condition);
            case ConstantExpression constant:
                return TranslateConstant(constant);
            case ConvertExpression convert:
                return TranslateConvert(convert);
            case DeclarationExpression declaration:
                return TranslateDeclaration(declaration);
            case LambdaExpression function:
                return TranslateFunction(function);
            case LabelExpression label:
                return TranslateLabel(label);
            case PathExpression path:
                return TranslatePath(path);
            case ReferenceExpression rex:
                return TranslateReference(rex);
            case LoopExpression @while:
                return TranslateLoop(@while);
            default:
                throw new InvalidOperationException($"Unhandled semantic type '{expression.GetType().Name}' in {nameof(ExpressionTranslator)}.Translate");
        }
    }

    private Type Translate(TypeSymbol type)
    {
        if (type.RuntimeType != null)
            return type.RuntimeType;

        if (type is FunctionSymbol fs)
        {
            var list = new List<Type>();
            list.AddRange(fs.Parameters.Select(p => Translate(p.ParameterType)));
            list.Add(Translate(fs.ReturnType));
            return L.Expression.GetDelegateType(list.ToArray());
        };

        throw new InvalidOperationException($"Unhandled type '{type.Name}' in {nameof(ExpressionTranslator)}.GetType");
    }

    private L.Expression TranslateBlock(BlockExpression block)
    {
        var declarations = block.SelectWhere(e => 
            e is not BlockExpression && e is not LambdaExpression, 
            e => e is DeclarationExpression, 
            e => (DeclarationExpression)e)
            .ToList();

        if (declarations.Count > 0)
        {
            return L.Expression.Block(
                Translate(block.ResultType),
                declarations.Select(d => DeclareVariable(d.Variable!, d.Variable!.VariableType)), 
                block.Expressions.Select(e => Translate(e)));
        }
        else
        {
            return L.Expression.Block(
                Translate(block.ResultType), 
                block.Expressions.Select(e => Translate(e)));
        }
    }

    private L.Expression TranslateCall(CallExpression call)
    {
        var calledSymbol = call.CalledSymbol;
        if (calledSymbol == null)
            throw new InvalidOperationException($"Cannot translate call for unknown function");

        switch (calledSymbol)
        {
            case MethodSymbol method:
                if (method.RuntimeMethod is System.Reflection.MethodInfo mi)
                {
                    var instance = call.Expression is PathExpression path
                        ? Translate(path.Expression)
                        : null;
                    var arguments = call.Arguments.Select(a => Translate(a)).ToArray();

                    return L.Expression.Call(instance, mi, arguments);
                }
                break;
            case ConstructorSymbol constructor:
                if (constructor.RuntimeMethod is System.Reflection.ConstructorInfo ci)
                {
                    var arguments = call.Arguments.Select(a => Translate(a)).ToArray();
                    return L.Expression.New(ci, arguments);
                }
                break;
            case OperatorSymbol opsym:
                return TranslateOperatorCall(call, opsym);
            case FunctionSymbol function:
                {
                    var fn = Translate(call.Expression);
                    var arguments = call.Arguments.Select(a => Translate(a)).ToArray();
                    return L.Expression.Invoke(fn, arguments);
                }
        }

        throw new InvalidOperationException($"Cannot translate call for symbol '{calledSymbol.Name}'");
    }

    private L.Expression TranslateBranch(BranchExpression branch)
    {
        var label = GetCurrentBranchTarget(branch.TargetName);
        if (label == null)
            throw new InvalidOperationException($"No branch target defined for '{branch.TargetName}'");

        if (branch.Expression == null || branch.Expression.ResultType == SpecialSymbols.Void)
        {
            if (branch.IsBreak)
                return L.Expression.Break(label);
            else if (branch.IsContinue)
                return L.Expression.Continue(label);
            else if (branch.IsReturn)
                return L.Expression.Return(label);
            else
                return L.Expression.Goto(label);
        }
        else
        {
            var expression = Translate(branch.Expression);
            if (branch.IsBreak)
                return L.Expression.Break(label, expression);
            else if (branch.IsContinue)
                return L.Expression.Continue(label);
            else if (branch.IsReturn)
                return L.Expression.Return(label, expression);
            else
                return L.Expression.Goto(label, expression);
        }
    }

    private L.Expression TranslateCondition(ConditionExpression condition) =>
        L.Expression.Condition(
            Translate(condition.Test), 
            Translate(condition.WhenTrue), 
            Translate(condition.WhenFalse));

    private L.Expression TranslateConstant(ConstantExpression constant) =>
        L.Expression.Constant(constant.Value, Translate(constant.ResultType));

    private L.Expression TranslateConvert(ConvertExpression convert) =>
        L.Expression.Convert(
            Translate(convert.Expression),
            Translate((TypeSymbol)convert.ConvertedType.ReferencedSymbol!));

    private L.Expression TranslateDeclaration(DeclarationExpression declaration)
    {
        var variable = GetVariable(declaration.Variable!);
        if (declaration.Initializer != null)
        {
            var initializer = Translate(declaration.Initializer);
            return L.Expression.Assign(variable, initializer);
        }
        else
        {
            return variable;
        }
    }

    private L.Expression TranslateFunction(LambdaExpression function)
    {
        var oldReturnTarget = GetCurrentBranchTarget("return");
        var oldDelegateType = _currentFunctionDelegateType;
        _currentFunctionDelegateType = null;

        var parameters = function.Symbol!.Parameters.Select(p => DeclareVariable(p, p.ParameterType)).ToArray();

        L.Expression lambdaBody;

        if (function.ReturnType == SpecialSymbols.Void
            || function.ReturnType == null)
        {
            var returnTarget = L.Expression.Label("return");
            SetCurrentBranchTarget("return", returnTarget);
            var body = Translate(function.Body);
            lambdaBody = L.Expression.Block(body, L.Expression.Label(returnTarget));
        }
        else
        {
            var returnType = Translate(function.ReturnType);
            var returnTarget = L.Expression.Label(returnType, "return");
            SetCurrentBranchTarget("return", returnTarget);

            var body = Translate(function.Body);

            lambdaBody = L.Expression.Block(
                returnType, 
                body,
                L.Expression.Return(returnTarget, body),
                L.Expression.Label(returnTarget, L.Expression.Default(returnType)));
        }

        SetCurrentBranchTarget("return", oldReturnTarget);
        _currentFunctionDelegateType = oldDelegateType;

        if (_currentFunctionDelegateType != null)
            return L.Expression.Lambda(_currentFunctionDelegateType, lambdaBody, parameters);
        return L.Expression.Lambda(lambdaBody, parameters);
    }

    private L.Expression TranslateLabel(LabelExpression label)
    {
        var target = GetCurrentBranchTarget(label.Name);
        if (target == null)
            throw new InvalidOperationException($"No branch label defined for '{label.Name}'");
        return L.Expression.Label(target);
    }

    private L.Expression TranslatePath(PathExpression path)
    {
        switch (path.Reference.ReferencedSymbol)
        {
            case PropertySymbol prop when prop.RuntimeProperty is PropertyInfo pi:
                if (prop.IsStatic)
                {
                    return L.Expression.Property(null, pi);
                }
                else
                {
                    var expression = Translate(path.Expression);
                    return L.Expression.Property(expression, pi);
                }
            case FieldSymbol field when field.RuntimeField is FieldInfo fi:
                if (field.IsStatic)
                {
                    return L.Expression.Field(null, fi);
                }
                else
                {
                    var expression = Translate(path.Expression);
                    return L.Expression.Field(expression, fi);
                }
            default:
                if (path.ReferencedSymbol == null)
                    throw new InvalidOperationException($"The reference has no symbol");
                throw new InvalidOperationException($"Unhandled symbol '{path.Reference.ReferencedSymbol?.Name ?? "?"}' in {nameof(ExpressionTranslator)}.{nameof(TranslatePath)}");
        }
    }

    private L.Expression TranslateReference(ReferenceExpression rex)
    {
        switch (rex.ReferencedSymbol)
        {
            case VariableSymbol _:
            case ParameterSymbol _:
                return GetVariable(rex.ReferencedSymbol);
            case null:
                throw new InvalidOperationException("Reference has no symbol");
            default:
                throw new InvalidOperationException($"Unhandled symbol '{rex.ReferencedSymbol.Name}' in {nameof(ExpressionTranslator)}.{nameof(TranslateReference)}");
        }
    }

    private L.Expression TranslateLoop(LoopExpression @while)
    {
        var outerLoopContinue = GetCurrentBranchTarget("continue");
        var outerLoopBreak = GetCurrentBranchTarget("break");

        var loopContinue = L.Expression.Label("continue");
        var loopBreak = L.Expression.Label("break");

        SetCurrentBranchTarget("continue", loopContinue);
        SetCurrentBranchTarget("break", loopBreak);

        var body = Translate(@while.Body);

        var loop =
            L.Expression.Loop(
                body,
                loopBreak, loopContinue);

        SetCurrentBranchTarget("continue", outerLoopContinue);
        SetCurrentBranchTarget("break", outerLoopBreak);

        return loop;
    }

    private Type? _currentFunctionDelegateType;

    private Dictionary<string, L.LabelTarget?> _currentBranchTargets =
        new Dictionary<string, L.LabelTarget?>();

    public L.LabelTarget? GetCurrentBranchTarget(string targetName)
    {
        _currentBranchTargets.TryGetValue(targetName, out var labelTarget);
        return labelTarget;
    }

    public L.LabelTarget? SetCurrentBranchTarget(string targetName, L.LabelTarget? labelTarget)
    {
        _currentBranchTargets[targetName] = labelTarget;
        return labelTarget;
    }

    private Dictionary<Symbol, L.ParameterExpression> _variableMap =
        new Dictionary<Symbol, L.ParameterExpression>();

    private L.ParameterExpression DeclareVariable(Symbol symbol, TypeSymbol type)
    {
        var variable = L.Expression.Parameter(Translate(type), symbol.Name);
        _variableMap[symbol] = variable;
        return variable;
    }

    private L.ParameterExpression GetVariable(Symbol symbol)
    {
        if (_variableMap.TryGetValue(symbol, out var variable))
            return variable;

        throw new InvalidOperationException($"cannot find variable for symbol '{symbol.Name}'");
    }

    private L.Expression TranslateOperatorCall(CallExpression c, OperatorSymbol opsym)
    {
        switch (opsym.Kind)
        {
            case OperatorKinds.Add:
                return L.Expression.Add(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.Subtract:
                return L.Expression.Subtract(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.Multiply:
                return L.Expression.Multiply(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.Divide:
                return L.Expression.Divide(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.Remainder:
                return L.Expression.Modulo(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.Negate:
                return L.Expression.Negate(Translate(c.Arguments[0]));
            case OperatorKinds.BitwiseAnd:
                return L.Expression.And(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.BitwiseOr:
                return L.Expression.Or(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.BitwiseNot:
                return L.Expression.Not(Translate(c.Arguments[0]));
            case OperatorKinds.Equal:
                return L.Expression.Equal(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.NotEqual:
                return L.Expression.NotEqual(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.LessThan:
                return L.Expression.LessThan(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.LessThanOrEqual:
                return L.Expression.LessThanOrEqual(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.GreaterThan:
                return L.Expression.GreaterThan(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.GreaterThanOrEqual:
                return L.Expression.GreaterThanOrEqual(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.LogicalAnd:
                return L.Expression.And(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.LogicalAndAlso:
                return L.Expression.AndAlso(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.LogicalOr:
                return L.Expression.Or(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.LogicalOrElse:
                return L.Expression.OrElse(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKinds.LogicalNot:
                return L.Expression.Not(Translate(c.Arguments[0]));
        }

        throw new InvalidOperationException($"Unhandled operator kind '{opsym.Kind}'");
    }
}
