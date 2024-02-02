using L=System.Linq.Expressions;
using System.Reflection;

namespace Parkour.Emit;
using Binding;
using Semantics;
using Symbols;

/// <summary>
/// Translates <see cref="Expression"/> elements into LINQ expressions.
/// </summary>
public class LinqExpressionTranslator
{
    private readonly RuntimeSymbols _runtimeSymbols;

    /// <summary>
    /// Constructs a <see cref="LinqExpressionTranslator"/>
    /// that translates <see cref="Expression"/> instances into <see cref="L.Expression"/> types.
    /// </summary>
    public LinqExpressionTranslator(RuntimeSymbols runtimeSymbols)
    {
        _runtimeSymbols = runtimeSymbols;
    }

    /// <summary>
    /// Translate <see cref="LambdaExpression"/> to LINQ <see cref="L.LambdaExpression"/> type,
    /// optionally specifying the delegate type to use.
    /// </summary>
    public L.LambdaExpression TranslateToLambda(LambdaExpression expression, Type? delegateType = null)
    {
        return (L.LambdaExpression)Translate(expression);
    }

    /// <summary>
    /// Translates <see cref="LambdaExpression"/> to LINQ <see cref="L.Expression{TDelegate}"/> type.
    /// </summary>
    public L.Expression<TDelegate> TranslateToLambda<TDelegate>(LambdaExpression expression)
        where TDelegate : Delegate
    {
        return (L.Expression<TDelegate>)TranslateToLambda(expression, typeof(TDelegate));
    }

    /// <summary>
    /// Translates <see cref="Expression"/> to LINQ <see cref="L.Expression"/> type.
    /// </summary>
    public virtual L.Expression Translate(Expression expression)
    {
        if (expression.IsUnbound)
            throw new InvalidOperationException("Cannot translation unbound expressions");

        switch (expression)
        {
            case ArrayExpression array:
                return TranslateArray(array);
            case ArityExpression arity:
                return TranslateArity(arity);
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
            case MemberExpression path:
                return TranslateMember(path);
            case NameReferenceExpression nameRef:
                return TranslateNameReference(nameRef);
            case NewExpression @new:
                return TranslateNew(@new);
            case NewArraySizeExpression newArraySize:
                return TranslateNewArraySize(newArraySize);
            case NewArrayInitExpression newArrayInit:
                return TranslateNewArrayInit(newArrayInit);
            case SymbolReferenceExpression symbolRef:
                return TranslateSymbolReference(symbolRef);
            case TypeArgumentsExpression typeArgs:
                return TranslateTypeArguments(typeArgs);
            case VariableExpression variable:
                return TranslateVariable(variable);
            case VoidExpression @void:
                return TranslateVoid(@void);
            default:
                throw new InvalidOperationException($"Unhandled semantic type '{expression.GetType().Name}' in {nameof(LinqExpressionTranslator)}.Translate");
        }
    }

    private Type TranslateType(TypeSymbol typeSymbol)
    {
        if (_runtimeSymbols.TryGetRuntimeType(typeSymbol, out var type))
            return type;

        throw new InvalidOperationException($"Could not determine runtime type for symbol '{typeSymbol.FullName}' in {nameof(LinqExpressionTranslator)}.{nameof(TranslateType)}");
    }

    private MethodInfo TranslateMethod(MethodSymbol methodSymbol)
    {
        if (_runtimeSymbols.TryGetRuntimeMember(methodSymbol, out var memberInfo)
            && memberInfo is MethodInfo methodInfo)
        {
            return methodInfo;
        }

        throw new InvalidOperationException($"Could not determine runtime method for symbol '{methodSymbol.FullName}' in {nameof(LinqExpressionTranslator)}.{nameof(TranslateMethod)}");
    }

    private ConstructorInfo TranslateConstructor(ConstructorSymbol constructorSymbol)
    {
        if (_runtimeSymbols.TryGetRuntimeMember(constructorSymbol, out var memberInfo)
            && memberInfo is ConstructorInfo constructorInfo)
        {
            return constructorInfo;
        }

        throw new InvalidOperationException($"Could not determine runtime constructor for symbol '{constructorSymbol.FullName}' in {nameof(LinqExpressionTranslator)}.{nameof(TranslateConstructor)}");
    }

    private FieldInfo TranslateField(FieldSymbol fieldSymbol)
    {
        if (_runtimeSymbols.TryGetRuntimeMember(fieldSymbol, out var memberInfo)
            && memberInfo is FieldInfo fieldInfo)
        {
            return fieldInfo;
        }

        throw new InvalidOperationException($"Could not determine runtime field for symbol '{fieldSymbol.FullName}' in {nameof(LinqExpressionTranslator)}.{nameof(TranslateField)}");
    }

    private PropertyInfo TranslateProperty(PropertySymbol propertySymbol)
    {
        if (_runtimeSymbols.TryGetRuntimeMember(propertySymbol, out var memberInfo)
            && memberInfo is PropertyInfo propertyInfo)
        {
            return propertyInfo;
        }

        throw new InvalidOperationException($"Could not determine runtime property for symbol '{propertySymbol.FullName}' in {nameof(LinqExpressionTranslator)}.{nameof(TranslateProperty)}");
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
            var labelType = TranslateType(label.ResultType);
            var labelTarget = L.Expression.Label(labelType, label.Name);
            SetCurrentBranchTarget(label.LabelSymbol!, labelTarget);
        }

        var blockType = TranslateType(block.ResultType);
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
                {
                    var mi = TranslateMethod(method);
                    var callInstance = GetCallInstance(call.Expression);
                    var instance = callInstance != null ? Translate(callInstance) : null;
                    var parameterTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
                    var arguments = TranslateArguments(call.Arguments, parameterTypes);
                    return L.Expression.Call(instance, mi, arguments);
                }

            case OperatorSymbol opsym:
                return TranslateOperatorCall(call, opsym);

            case LambdaSymbol function:
                {
                    var fn = Translate(call.Expression);
                    var parameterTypes = function.Parameters.Select(p => TranslateType(p.ParameterType)).ToArray();
                    var arguments = TranslateArguments(call.Arguments, parameterTypes);
                    return L.Expression.Invoke(fn, arguments);
                }
        }

        throw new InvalidOperationException($"Cannot translate call for symbol '{calledSymbol.Name}'");
    }

    private Expression? GetCallInstance(Expression expression)
    {
        switch (expression)
        {
            case MemberExpression member:
                return member.Expression;
            case AdjustedReferenceExpression adjust:
                return GetCallInstance(adjust.Expression);
            default:
                return expression;
        }
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
        var type = TranslateType(condition.ResultType);
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
        L.Expression.Constant(constant.Value, TranslateType(constant.ResultType));

    private L.Expression TranslateConvert(ConvertExpression convert) =>
        L.Expression.Convert(
            Translate(convert.Expression),
            TranslateType(convert.ResultType));

    private L.Expression TranslateDefault(DefaultExpression dex)
    {
        var type = TranslateType(dex.ResultType);
        return L.Expression.Default(type);
    }

    private L.Expression TranslateLabel(LabelExpression label)
    {
        var labelTarget = GetCurrentBranchTarget(label.LabelSymbol!);
        if (labelTarget == null)
        {
            var labelType = TranslateType(label.ResultType);
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
            var returnTarget = L.Expression.Label(lambda.ReturnLabel!.Name);
            SetCurrentBranchTarget(lambda.ReturnLabel!, returnTarget);
            var body = Translate(lambda.Body);
            lambdaBody = L.Expression.Block(body, L.Expression.Label(returnTarget));
        }
        else
        {
            var returnType = TranslateType(lambda.ReturnType);
            var returnTarget = L.Expression.Label(returnType, lambda.ReturnLabel!.Name);
            SetCurrentBranchTarget(lambda.ReturnLabel, returnTarget);

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
        var breakType = TranslateType(loop.BreakTarget!.Type);
        var loopBreak = L.Expression.Label(breakType, loop.BreakTarget!.Name);

        SetCurrentBranchTarget(loop.ContinueTarget, loopContinue);
        SetCurrentBranchTarget(loop.BreakTarget, loopBreak);

        var body = Translate(loop.Body);

        return
            L.Expression.Loop(
                body,
                loopBreak, loopContinue);
    }


    private L.Expression TranslateMember(MemberExpression member)
    {
        switch (member.ReferencedSymbol)
        {
            case PropertySymbol prop:
                var pi = TranslateProperty(prop);
                if (prop.IsStatic)
                {
                    return L.Expression.Property(null, pi);
                }
                else
                {
                    var expression = Translate(member.Expression);
                    return L.Expression.Property(expression, pi);
                }
            case FieldSymbol field:
                var fi = TranslateField(field);
                if (field.IsStatic)
                {
                    return L.Expression.Field(null, fi);
                }
                else
                {
                    var expression = Translate(member.Expression);
                    return L.Expression.Field(expression, fi);
                }
            default:
                if (member.ReferencedSymbol == null)
                    throw new InvalidOperationException($"The reference has no symbol");
                throw new InvalidOperationException($"Unhandled symbol '{member.ReferencedSymbol?.Name ?? "?"}' in {nameof(LinqExpressionTranslator)}.{nameof(TranslateMember)}");
        }
    }

    private L.Expression TranslateNew(NewExpression @new)
    {
        var constructor = TranslateConstructor(@new.ConstructorSymbol!);
        return L.Expression.New(constructor);
    }

    private L.Expression TranslateNewArraySize(NewArraySizeExpression newArray)
    {
        var elementType = TranslateType(newArray.ElementTypeSymbol!);
        var size = Translate(newArray.Size);
        return L.Expression.NewArrayBounds(elementType, size);
    }

    private L.Expression TranslateNewArrayInit(NewArrayInitExpression newArray)
    {
        var elementType = TranslateType(newArray.ElementTypeSymbol!);
        var expressions = newArray.Expressions.Select(e => Translate(e)).ToArray();
        return L.Expression.NewArrayInit(elementType, expressions);
    }

    private L.Expression TranslateNameReference(NameReferenceExpression rex)
    {
        return TranslateReferencedSymbol(rex.ReferencedSymbol);
    }

    private L.Expression TranslateSymbolReference(SymbolReferenceExpression rex)
    {
        return TranslateReferencedSymbol(rex.ReferencedSymbol);
    }

    private L.Expression TranslateArity(ArityExpression arity)
    {
        return TranslateReferencedSymbol(arity.ReferencedSymbol);
    }

    private L.Expression TranslateArray(ArrayExpression array)
    {
        return TranslateReferencedSymbol(array.ReferencedSymbol);
    }

    private L.Expression TranslateTypeArguments(TypeArgumentsExpression typeArgs)
    {
        return TranslateReferencedSymbol(typeArgs.ReferencedSymbol);
    }

    private L.Expression TranslateReferencedSymbol(Symbol? symbol)
    {
        switch (symbol)
        {
            case VariableSymbol _:
            case ParameterSymbol _:
                var v = GetVariable(symbol);
                if (v == null)
                    throw new InvalidOperationException($"The symbol '{symbol.Name}' has no matching variable.");
                return v;

            case GroupSymbol gs:
                return L.Expression.NewArrayInit(
                    typeof(object),
                    gs.Symbols.Select(s => TranslateReferencedSymbol(s)).ToArray());

            case TypeSymbol ts:
                return L.Expression.Constant(TranslateType(ts));

            case MethodSymbol ms:
                return L.Expression.Constant(TranslateMethod(ms));

            case ConstructorSymbol cs:
                return L.Expression.Constant(TranslateConstructor(cs));

            case null:
                throw new InvalidOperationException("Reference has no symbol");

            default:
                throw new InvalidOperationException($"Unhandled symbol '{symbol.Name}' in {nameof(LinqExpressionTranslator)}.{nameof(TranslateReferencedSymbol)}");
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
        var variable = L.Expression.Parameter(TranslateType(type), symbol.Name);
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
            case OperatorKind.Add:
                return L.Expression.Add(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.Subtract:
                return L.Expression.Subtract(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.Multiply:
                return L.Expression.Multiply(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.Divide:
                return L.Expression.Divide(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.Remainder:
                return L.Expression.Modulo(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.Negate:
                return L.Expression.Negate(Translate(c.Arguments[0]));
            case OperatorKind.BitwiseAnd:
                return L.Expression.And(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.BitwiseOr:
                return L.Expression.Or(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.BitwiseNot:
                return L.Expression.Not(Translate(c.Arguments[0]));
            case OperatorKind.Equal:
                return L.Expression.Equal(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.NotEqual:
                return L.Expression.NotEqual(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.LessThan:
                return L.Expression.LessThan(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.LessThanOrEqual:
                return L.Expression.LessThanOrEqual(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.GreaterThan:
                return L.Expression.GreaterThan(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.GreaterThanOrEqual:
                return L.Expression.GreaterThanOrEqual(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.LogicalAnd:
                return L.Expression.And(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.LogicalAndAlso:
                return L.Expression.AndAlso(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.LogicalOr:
                return L.Expression.Or(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.LogicalOrElse:
                return L.Expression.OrElse(Translate(c.Arguments[0]), Translate(c.Arguments[1]));
            case OperatorKind.LogicalNot:
                return L.Expression.Not(Translate(c.Arguments[0]));
        }

        throw new InvalidOperationException($"Unhandled operator kind '{opsym.Kind}'");
    }
}
