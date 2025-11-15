using L=System.Linq.Expressions;
using System.Collections.Immutable;
using System.Reflection;

namespace Parkour.Linq;

using Parkour;
using Reflection;
using Semantics;
using Symbols;

/// <summary>
/// Translates <see cref="Expression"/> elements into LINQ expressions.
/// </summary>
public class LinqTranslator
{
    private readonly ReflectionSymbols _runtimeSymbols;

    /// <summary>
    /// Constructs a <see cref="LinqTranslator"/>
    /// that translates <see cref="Expression"/> instances into <see cref="L.Expression"/> types.
    /// </summary>
    public LinqTranslator(
        ReflectionSymbols runtimeSymbols)
    {
        _runtimeSymbols = runtimeSymbols;
    }

    /// <summary>
    /// Translate <see cref="LambdaExpression"/> to LINQ <see cref="L.LambdaExpression"/> type,
    /// optionally specifying the delegate type to use.
    /// </summary>
    public L.LambdaExpression TranslateToLambda(
        LambdaExpression expression, 
        Type? delegateType = null)
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
            throw new InvalidOperationException("Cannot translate unbound expressions");

        switch (expression)
        {
            case ArrayExpression array:
                return TranslateArray(array);
            case ArityExpression arity:
                return TranslateArity(arity);
            case AssignExpression assign:
                return TranslateAssign(assign);
            case AsTypeExpression asType:
                return TranslateAsType(asType);
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
            case ConstructExpression construct:
                return TranslateConstruct(construct);
            case ConvertExpression convert:
                return TranslateConvert(convert);
            case DefaultExpression dex:
                return TranslateDefault(dex);
            case IsTypeExpression isType:
                return TranslateIsType(isType);
            case LabelExpression label:
                return TranslateLabel(label);
            case LambdaExpression lambda:
                return TranslateLambda(lambda);
            case LoopExpression loop:
                return TranslateLoop(loop);
            case MemberExpression path:
                return TranslateMember(path);
            case NameExpression nameRef:
                return TranslateNameReference(nameRef);
            case NewArrayExpression newArray:
                return TranslateNewArray(newArray);
            case NewExpression @new:
                return TranslateNew(@new);
            case OperatorExpression opex:
                return TranslateOperator(opex);
            case SymbolExpression symbolRef:
                return TranslateSymbolReference(symbolRef);
            case VariableExpression variable:
                return TranslateVariable(variable);
            default:
                throw new InvalidOperationException($"Unhandled semantic type '{expression.GetType().Name}' in {nameof(LinqTranslator)}.Translate");
        }
    }

    #region expressions

    private L.Expression TranslateAssign(AssignExpression assign)
    {
        var target = Translate(assign.Target);
        var source = ConvertVoidToValue(Translate(assign.Source), target.Type);
        return L.Expression.Assign(target, source);
    }

    private L.Expression TranslateAsType(AsTypeExpression asType)
    {
        var expr = Translate(asType.Expression);
        var type = TranslateType(asType.TypeSymbol!);
        return L.Expression.TypeAs(expr, type);
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
            var v = DeclareVariable(decl.VariableSymbol!, decl.VariableSymbol!.Type);
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

        if (branch.Expression == null || branch.Expression.ResultType == _runtimeSymbols.Void)
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
                return TranslateOperator(opsym, call.Arguments);

            case DelegateSymbol function:
                {
                    var fn = Translate(call.Expression);
                    var parameterTypes = function.Parameters.Select(p => TranslateType(p.Type)).ToArray();
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
                return member.Instance;
            case AugmentedReferenceExpression adjust:
                return GetCallInstance(adjust.TypeOrMember);
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

    private L.Expression TranslateIsType(IsTypeExpression isType)
    {
        var expr = Translate(isType.Expression);
        var type = TranslateType(isType.TypeSymbol!);
        return L.Expression.TypeIs(expr, type);
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

        if (label.ResultType == _runtimeSymbols.Void)
            return L.Expression.Label(labelTarget);
        return L.Expression.Label(labelTarget, L.Expression.Default(labelTarget.Type));
    }

    private L.Expression TranslateLambda(LambdaExpression lambda)
    {
        var parameters = lambda.FunctionSymbol!.Parameters
            .Select(p => DeclareVariable(p, p.Type))
            .ToArray();

        L.Expression lambdaBody;

        if (lambda.FunctionSymbol == null
            || lambda.FunctionSymbol.ReturnType == null
            || lambda.FunctionSymbol.ReturnType == _runtimeSymbols.Void)
        {
            var returnTarget = L.Expression.Label(lambda.ReturnLabel!.Name);
            SetCurrentBranchTarget(lambda.ReturnLabel!, returnTarget);
            var body = Translate(lambda.Body);
            lambdaBody = L.Expression.Block(body, L.Expression.Label(returnTarget));
        }
        else
        {
            var returnType = TranslateType(lambda.FunctionSymbol.ReturnType);

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

        var body = Translate(loop.Expression);

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
                    var expression = Translate(member.Instance);
                    return L.Expression.Property(expression, pi);
                }
            case IndexerSymbol indexer:
                var ii = TranslateIndexer(indexer);
                if (indexer.IsStatic)
                {
                    return L.Expression.Property(null, ii);
                }
                else
                {
                    var expression = Translate(member.Instance);
                    return L.Expression.Property(expression, ii);
                }

            case FieldSymbol field:
                var fi = TranslateField(field);
                if (field.IsStatic)
                {
                    return L.Expression.Field(null, fi);
                }
                else
                {
                    var expression = Translate(member.Instance);
                    return L.Expression.Field(expression, fi);
                }
            default:
                if (member.ReferencedSymbol == null)
                    throw new InvalidOperationException($"The reference has no symbol");
                throw new InvalidOperationException($"Unhandled symbol '{member.ReferencedSymbol?.Name ?? "?"}' in {nameof(LinqTranslator)}.{nameof(TranslateMember)}");
        }
    }

    private L.Expression TranslateNew(NewExpression @new)
    {
        var constructor = TranslateConstructor(@new.ConstructorSymbol!);
        return L.Expression.New(constructor);
    }

    private L.Expression TranslateNewArray(NewArrayExpression newArray)
    {
        var elementType = TranslateType(newArray.ElementTypeSymbol!);
        if (newArray.Values.Count == 0)
        {
            var sizes = newArray.Sizes.Select(s => Translate(s)).ToArray();
            return L.Expression.NewArrayBounds(elementType, sizes);
        }
        else
        {
            var values = newArray.Values.Select(e => Translate(e)).ToArray();
            return L.Expression.NewArrayInit(elementType, values);
        }
    }

    private L.Expression TranslateNameReference(NameExpression rex)
    {
        return TranslateReferencedSymbol(rex.ReferencedSymbol);
    }

    private L.Expression TranslateOperator(OperatorExpression opex)
    {
        var operatorSymbol = opex.OperatorSymbol;
        if (operatorSymbol == null)
            throw new InvalidOperationException($"Cannot translate unknown operator");

        switch (operatorSymbol)
        {
            case MethodSymbol method:
                {
                    var mi = TranslateMethod(method);
                    var parameterTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
                    var arguments = TranslateArguments(opex.Arguments, parameterTypes);
                    return L.Expression.Call(null, mi, arguments);
                }

            case OperatorSymbol opsym:
                return TranslateOperator(opsym, opex.Arguments);
        }

        throw new InvalidOperationException($"Cannot translate call for symbol '{operatorSymbol.Name}'");
    }

    private L.Expression TranslateOperator(OperatorSymbol opsym, ImmutableList<Expression> arguments)
    {
        switch (opsym.Operator)
        {
            case RuntimeOperator.Add:
                return L.Expression.Add(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.Subtract:
                return L.Expression.Subtract(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.Multiply:
                return L.Expression.Multiply(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.Divide:
                return L.Expression.Divide(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.Remainder:
                return L.Expression.Modulo(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.Negate:
                return L.Expression.Negate(Translate(arguments[0]));
            case RuntimeOperator.BitwiseAnd:
            case RuntimeOperator.LogicalAnd:
                return L.Expression.And(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.BitwiseOr:
            case RuntimeOperator.LogicalOr:
                return L.Expression.Or(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.BitwiseNot:
            case RuntimeOperator.LogicalNot:
                return L.Expression.Not(Translate(arguments[0]));
            case RuntimeOperator.LogicalAndAlso:
                return L.Expression.AndAlso(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.LogicalOrElse:
                return L.Expression.OrElse(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.Equal:
                return L.Expression.Equal(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.NotEqual:
                return L.Expression.NotEqual(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.LessThan:
                return L.Expression.LessThan(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.LessThanOrEqual:
                return L.Expression.LessThanOrEqual(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.GreaterThan:
                return L.Expression.GreaterThan(Translate(arguments[0]), Translate(arguments[1]));
            case RuntimeOperator.GreaterThanOrEqual:
                return L.Expression.GreaterThanOrEqual(Translate(arguments[0]), Translate(arguments[1]));
        }

        throw new InvalidOperationException($"Unhandled operator kind '{opsym.Operator}'");
    }

    private L.Expression TranslateSymbolReference(SymbolExpression rex)
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

    private L.Expression TranslateConstruct(ConstructExpression construct)
    {
        return TranslateReferencedSymbol(construct.ReferencedSymbol);
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
                throw new InvalidOperationException($"Unhandled symbol '{symbol.Name}' in {nameof(LinqTranslator)}.{nameof(TranslateReferencedSymbol)}");
        }
    }

    private L.Expression TranslateVariable(VariableExpression declaration)
    {
        var variable = GetVariable(declaration.VariableSymbol!);
        if (variable == null)
        {
            // variable must be associated with a block or parameter,
            // but was not predeclared in map from an outer block or parameter
            // so add wrapper block and try again.
            var block = new BlockExpression([declaration], declaration.Location).WithResultType(declaration.ResultType);
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

    #endregion

    #region symbols

    private Type TranslateType(TypeSymbol typeSymbol)
    {
        if (_runtimeSymbols.TryGetType(typeSymbol, out var type))
            return type;

        throw new InvalidOperationException($"Could not determine runtime type for symbol '{typeSymbol.FullName}' in {nameof(LinqTranslator)}.{nameof(TranslateType)}");
    }

    private MethodInfo TranslateMethod(MethodSymbol methodSymbol)
    {
        if (_runtimeSymbols.TryGetMemberInfo(methodSymbol, out var memberInfo)
            && memberInfo is MethodInfo methodInfo)
        {
            return methodInfo;
        }

        throw new InvalidOperationException($"Could not determine runtime method for symbol '{methodSymbol.FullName}' in {nameof(LinqTranslator)}.{nameof(TranslateMethod)}");
    }

    private ConstructorInfo TranslateConstructor(ConstructorSymbol constructorSymbol)
    {
        if (_runtimeSymbols.TryGetMemberInfo(constructorSymbol, out var memberInfo)
            && memberInfo is ConstructorInfo constructorInfo)
        {
            return constructorInfo;
        }

        throw new InvalidOperationException($"Could not determine runtime constructor for symbol '{constructorSymbol.FullName}' in {nameof(LinqTranslator)}.{nameof(TranslateConstructor)}");
    }

    private FieldInfo TranslateField(FieldSymbol fieldSymbol)
    {
        if (_runtimeSymbols.TryGetMemberInfo(fieldSymbol, out var memberInfo)
            && memberInfo is FieldInfo fieldInfo)
        {
            return fieldInfo;
        }

        throw new InvalidOperationException($"Could not determine runtime field for symbol '{fieldSymbol.FullName}' in {nameof(LinqTranslator)}.{nameof(TranslateField)}");
    }

    private PropertyInfo TranslateProperty(PropertySymbol propertySymbol)
    {
        if (_runtimeSymbols.TryGetMemberInfo(propertySymbol, out var memberInfo)
            && memberInfo is PropertyInfo propertyInfo)
        {
            return propertyInfo;
        }

        throw new InvalidOperationException($"Could not determine runtime property for symbol '{propertySymbol.FullName}' in {nameof(LinqTranslator)}.{nameof(TranslateProperty)}");
    }

    private PropertyInfo TranslateIndexer(IndexerSymbol indexerSymbol)
    {
        if (_runtimeSymbols.TryGetMemberInfo(indexerSymbol, out var memberInfo)
            && memberInfo is PropertyInfo propertyInfo)
        {
            return propertyInfo;
        }

        throw new InvalidOperationException($"Could not determine runtime property for symbol '{indexerSymbol.FullName}' in {nameof(LinqTranslator)}.{nameof(TranslateProperty)}");
    }

    #endregion
}
