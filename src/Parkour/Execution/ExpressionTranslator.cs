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
            case AssignExpression assign:
                return TranslateAssign(assign);
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
            case DefaultExpression dex:
                return TranslateDefault(dex);
            case LabelExpression label:
                return TranslateLabel(label);
            case LambdaExpression lambda:
                return TranslateLambda(lambda);
            case LoopExpression loop:
                return TranslateLoop(loop);
            case PathExpression path:
                return TranslatePath(path);
            case ReferenceExpression rex:
                return TranslateReference(rex);
            case VariableExpression variable:
                return TranslateVariable(variable);
            case VoidExpression @void:
                return TranslateVoid(@void);
            default:
                throw new InvalidOperationException($"Unhandled semantic type '{expression.GetType().Name}' in {nameof(ExpressionTranslator)}.Translate");
        }
    }

    private Type Translate(TypeSymbol type)
    {
        if (type == SpecialSymbols.Void
            || type == SpecialSymbols.DoesNotReturn)
            return typeof(void);

        if (type.RuntimeType != null)
            return type.RuntimeType;

        if (type is LambdaSymbol fs)
        {
            var list = new List<Type>();
            list.AddRange(fs.Parameters.Select(p => Translate(p.ParameterType)));
            list.Add(Translate(fs.ReturnType));
            return L.Expression.GetDelegateType(list.ToArray());
        };

        throw new InvalidOperationException($"Unhandled type '{type.Name}' in {nameof(ExpressionTranslator)}.GetType");
    }

    private L.Expression TranslateAssign(AssignExpression assign)
    {
        var target = Translate(assign.Target);
        var source = ConvertVoidToValue(Translate(assign.Source), target.Type);
        return L.Expression.Assign(target, source);
    }

    private L.Expression TranslateBlock(BlockExpression block)
    {
        var variableDecls = block.SelectWhere(e => 
            e == block || (e is not BlockExpression && e is not LambdaExpression), 
            e => e is VariableExpression, 
            e => (VariableExpression)e)
            .ToList();

        var variables = new List<L.ParameterExpression>();
        foreach (var decl in variableDecls)
        {
            var v = DeclareVariable(decl.Variable!, decl.Variable!.VariableType);
            variables.Add(v);
        }

        var labels = block.Expressions
            .OfType<LabelExpression>()
            .ToList();

        foreach (var label in labels)
        {
            var labelType = Translate(label.ResultType);
            var labelTarget = L.Expression.Label(labelType, label.Name);
            SetCurrentBranchTarget(label.LabelSymbol!, labelTarget);
        }

        var blockType = Translate(block.ResultType);
        var expressions = block.Expressions.Select(e => Translate(e)).ToList();

        if (variableDecls.Count > 0)
        {
            return L.Expression.Block(
                blockType,
                variables, 
                expressions);
        }
        else
        {
            return L.Expression.Block(
                blockType, 
                expressions);
        }
    }

    private L.Expression TranslateBranch(BranchExpression branch)
    {
        var label = GetCurrentBranchTarget(branch.LabelSymbol!);
        if (label == null)
            throw new InvalidOperationException($"No branch target defined for '{branch.LabelName}'");

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

    private L.Expression TranslateCall(CallExpression call)
    {
        var calledSymbol = call.CalledSymbol;
        if (calledSymbol == null)
            throw new InvalidOperationException($"Cannot translate call for unknown function");

        switch (calledSymbol)
        {
            case MethodSymbol method:
                if (method.RuntimeMethod is MethodInfo mi)
                {
                    var instance = call.Expression is PathExpression path
                        ? Translate(path.Expression)
                        : null;
                    var parameterTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
                    var arguments = TranslateArguments(call.Arguments, parameterTypes);
                    return L.Expression.Call(instance, mi, arguments);
                }
                break;
            case ConstructorSymbol constructor:
                if (constructor.RuntimeMethod is ConstructorInfo ci)
                {
                    var parameterTypes = ci.GetParameters().Select(p => p.ParameterType).ToArray();
                    var arguments = TranslateArguments(call.Arguments, parameterTypes);
                    return L.Expression.New(ci, arguments);
                }
                break;
            case OperatorSymbol opsym:
                return TranslateOperatorCall(call, opsym);
            case LambdaSymbol function:
                {
                    var fn = Translate(call.Expression);
                    var parameterTypes = function.Parameters.Select(p => Translate(p.ParameterType)).ToArray();
                    var arguments = TranslateArguments(call.Arguments, parameterTypes);
                    return L.Expression.Invoke(fn, arguments);
                }
        }

        throw new InvalidOperationException($"Cannot translate call for symbol '{calledSymbol.Name}'");
    }

    private IReadOnlyList<L.Expression> TranslateArguments(
        ImmutableList<Expression> arguments, IReadOnlyList<Type> parameterTypes)
    {
        var translatedArgs = new List<L.Expression>();
        for (int i = 0; i < arguments.Count; i++)
        {
            var ptype = parameterTypes[i];
            var arg = ConvertVoidToValue(Translate(arguments[i]), ptype);
            translatedArgs.Add(arg);
        }
        return translatedArgs;
    }

    private L.Expression TranslateCondition(ConditionExpression condition)
    {
        var type = Translate(condition.ResultType);
        var test = Translate(condition.Test);
        var whenTrue = ConvertVoidToValue(Translate(condition.WhenTrue), type);
        var whenFalse = ConvertVoidToValue(Translate(condition.WhenFalse), type);
        return L.Expression.Condition(test, whenTrue, whenFalse, type);
    }

    private L.Expression ConvertVoidToValue(L.Expression expr, Type type)
    {
        if (expr.Type == typeof(void) && type != typeof(void))
        {
            var def = L.Expression.Default(type);
            return L.Expression.Block(
                type,
                expr,
                def);
        }

        return expr;
    }

    private L.Expression TranslateConstant(ConstantExpression constant) =>
        L.Expression.Constant(constant.Value, Translate(constant.ResultType));

    private L.Expression TranslateConvert(ConvertExpression convert) =>
        L.Expression.Convert(
            Translate(convert.Expression),
            Translate(convert.ResultType));

    private L.Expression TranslateDefault(DefaultExpression dex)
    {
        var type = Translate(dex.ResultType);
        return L.Expression.Default(type);
    }

    private L.Expression TranslateLabel(LabelExpression label)
    {
        var labelTarget = GetCurrentBranchTarget(label.LabelSymbol!);
        if (labelTarget == null)
        {
            var labelType = Translate(label.ResultType);
            labelTarget = L.Expression.Label(labelType, label.Name);
            SetCurrentBranchTarget(label.LabelSymbol!, labelTarget);
        }

        if (label.ResultType == SpecialSymbols.Void)
            return L.Expression.Label(labelTarget);
        return L.Expression.Label(labelTarget, L.Expression.Default(labelTarget.Type));
    }

    private L.Expression TranslateLambda(LambdaExpression lambda)
    {
        var parameters = lambda.LambdaSymbol!.Parameters
            .Select(p => DeclareVariable(p, p.ParameterType))
            .ToArray();

        L.Expression lambdaBody;

        if (lambda.ReturnType == SpecialSymbols.Void
            || lambda.ReturnType == null)
        {
            var returnTarget = L.Expression.Label(lambda.ReturnTarget!.Name);
            SetCurrentBranchTarget(lambda.ReturnTarget!, returnTarget);
            var body = Translate(lambda.Body);
            lambdaBody = L.Expression.Block(body, L.Expression.Label(returnTarget));
        }
        else
        {
            var returnType = Translate(lambda.ReturnType);
            var returnTarget = L.Expression.Label(returnType, lambda.ReturnTarget!.Name);
            SetCurrentBranchTarget(lambda.ReturnTarget, returnTarget);

            var body = Translate(lambda.Body);
            
            lambdaBody = L.Expression.Block(
                returnType,
                body,
                L.Expression.Return(returnTarget, body),
                L.Expression.Label(returnTarget, L.Expression.Default(returnType)));
        }

        return L.Expression.Lambda(lambdaBody, parameters);
    }

    private L.Expression TranslateLoop(LoopExpression loop)
    {
        var loopContinue = L.Expression.Label(loop.ContinueTarget!.Name);
        var breakType = Translate(loop.BreakTarget!.Type);
        var loopBreak = L.Expression.Label(breakType, loop.BreakTarget!.Name);

        SetCurrentBranchTarget(loop.ContinueTarget, loopContinue);
        SetCurrentBranchTarget(loop.BreakTarget, loopBreak);

        var body = Translate(loop.Body);

        return
            L.Expression.Loop(
                body,
                loopBreak, loopContinue);
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
                var v = GetVariable(rex.ReferencedSymbol);
                if (v == null)
                    throw new InvalidOperationException($"The name '{rex.ReferencedSymbol}' has no matching variable.");
                return v;
            case null:
                throw new InvalidOperationException("Reference has no symbol");
            default:
                throw new InvalidOperationException($"Unhandled symbol '{rex.ReferencedSymbol.Name}' in {nameof(ExpressionTranslator)}.{nameof(TranslateReference)}");
        }
    }

    private L.Expression TranslateVariable(VariableExpression declaration)
    {
        var variable = GetVariable(declaration.Variable!);
        if (variable == null)
        {
            // variable must be associated with a block or parameter,
            // but was not predeclared in map from an outer block or parameter
            // so add wrapper block and try again.
            var block = new BlockExpression([declaration], declaration.Location, declaration.ResultType, null);
            return TranslateBlock(block);
        }
        else
        {
            if (declaration.Initializer != null)
            {
                var initializer = ConvertVoidToValue(Translate(declaration.Initializer), variable.Type);
                return L.Expression.Assign(variable, initializer);
            }
            else
            {
                return variable;
            }
        }
    }

    private static readonly L.Expression _void =
        L.Expression.Block(typeof(void));

    private L.Expression TranslateVoid(VoidExpression vex)
    {
        return _void;
    }

    private Dictionary<LabelSymbol, L.LabelTarget> _currentBranchTargets =
        new Dictionary<LabelSymbol, L.LabelTarget>();

    public L.LabelTarget? GetCurrentBranchTarget(LabelSymbol labelSymbol)
    {
        if (labelSymbol == null)
            return null;
        _currentBranchTargets.TryGetValue(labelSymbol, out var target);
        return target;
    }

    public void SetCurrentBranchTarget(LabelSymbol labelSymbol, L.LabelTarget labelTarget)
    {
        _currentBranchTargets[labelSymbol] = labelTarget;
    }

    private Dictionary<Symbol, L.ParameterExpression> _variableMap =
        new Dictionary<Symbol, L.ParameterExpression>();

    private L.ParameterExpression DeclareVariable(Symbol symbol, TypeSymbol type)
    {
        var variable = L.Expression.Parameter(Translate(type), symbol.Name);
        _variableMap[symbol] = variable;
        return variable;
    }

    private L.ParameterExpression? GetVariable(Symbol symbol)
    {
        _variableMap.TryGetValue(symbol, out var variable);
        return variable;
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
