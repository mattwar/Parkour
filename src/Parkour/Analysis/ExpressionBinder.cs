namespace Parkour.Analysis;
using Expressions;
using Symbols;
using System;

public class ExpressionBinder
{
    private readonly CommonSymbols _symbols;
    private readonly Operators _operators;
    private bool _rebind;
    private BindingScope _scope;

    private readonly ObjectPool<List<Symbol>> _symbolListPool =
        new ObjectPool<List<Symbol>>(() => new List<Symbol>(), list => list.Clear());

    private readonly ObjectPool<List<MemberSymbol>> _memberListPool =
        new ObjectPool<List<MemberSymbol>>(() => new List<MemberSymbol>(), list => list.Clear());

    private readonly ObjectPool<List<Diagnostic>> _diagnosticListPool =
        new ObjectPool<List<Diagnostic>>(() => new List<Diagnostic>(), list => list.Clear());

    public ExpressionBinder(CommonSymbols symbols, BindingScope scope)
    {
        _symbols = symbols;
        _operators = Operators.From(_symbols);
        _scope = scope;
    }

    protected BindingScope CurrentScope => _scope;

    /// <summary>
    /// Rebinds all expressions.
    /// </summary>
    public Expression Rebind(Expression expression)
    {
        var oldRebind = _rebind;
        _rebind = true;
        var rebound = Bind(expression);
        _rebind = oldRebind;
        return rebound;
    }

    protected Expression RebindInScope(Expression expression, BindingScope scope)
    {
        var oldScope = _scope;
        _scope = scope;
        var rebound = Rebind(expression);
        _scope = oldScope;
        return rebound;
    }

    protected Expression BindInScope(Expression expression, BindingScope scope)
    {
        var oldScope = _scope;
        _scope = scope;
        var bound = Bind(expression);
        _scope = oldScope;
        return bound;
    }

    /// <summary>
    /// Binds all unbound expressions.
    /// </summary>
    public Expression Bind(Expression expression)
    {
        if (!(_rebind || expression.ContainsUnknowns))
            return expression;

        switch (expression)
        {
            case BlockExpression block:
                return BindBlock(block);

            case BranchExpression branch:
                return BindBranch(branch);

            case CallExpression call:
                return BindCall(call);

            case ConditionExpression condition:
                return BindCondition(condition);

            case ConstantExpression constant:
                return BindConstant(constant);

            case ConvertExpression convert:
                return BindConvert(convert);

            case DeclarationExpression declaration:
                return BindDeclaration(declaration);

            case FunctionExpression function:
                return BindFunction(function);

            case OperatorExpression opex:
                return BindOperator(opex);

            case PathExpression path:
                return BindPath(path);

            case ReferenceExpression reference:
                return BindReference(reference);

            default:
                throw new InvalidOperationException($"Unhandled semantic '{expression.GetType().Name}' in {nameof(ExpressionBinder)} BindSymbols");
        }
    }

    protected virtual Expression BindBlock(BlockExpression block)
    {
        var rebind = false;
        var oldScope = _scope;

        // rebind expressions 
        var (newList, _) = block.Expressions.Rewrite(_scope, (expression, scope) =>
        {
            _scope = scope;
            var boundExpression = rebind ? Rebind(expression) : Bind(expression);

            if (boundExpression is DeclarationExpression decl
                && decl.Variable != null)
            {
                // if the declaration changes then the variable is now different 
                // so the rest of the block needs to be rebound in case it references the old variable
                rebind = rebind || boundExpression != expression;
                scope = scope.AddAmbientSymbol(decl.Variable);
            }

            return (boundExpression, scope);
        });

        _scope = oldScope;

        if (newList == block.Expressions)
            return block;

        return new BlockExpression(newList);
    }

    protected virtual Expression BindBranch(BranchExpression branch)
    {
        var expression = branch.Expression != null
            ? Bind(branch.Expression)
            : null;

        var targetSymbol = _scope.FindSymbol<TargetSymbol>(s => s.Name == branch.TargetName);
        var diagnostics =
            targetSymbol == null ? ImmutableList.Create(DiagnosticFactory.NoMatchingTarget(branch.TargetName))
            : null;

        if (expression == branch.Expression
            && targetSymbol == branch.Target)
            return branch;

        return new BranchExpression(branch.TargetName, expression, targetSymbol, diagnostics);
    }

    protected virtual Expression BindCall(CallExpression call)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = Bind(call.Expression);
            var arguments = BindList(call.Arguments);

            var instance = expression;

            if (expression is PathExpression path) // xxx.Method?
            {
                instance = path.Expression;
                GetCalledSymbolCandidates(path.Expression, path.Reference.Name, arguments, candidates);
            }

            if (candidates.Count == 0)
            {
                if (expression.ReferencedSymbol is GroupSymbol refGroup
                    && refGroup.Symbols.Any(IsCallableSymbol))
                {
                    candidates.AddRange(refGroup.Symbols);
                }
                else if (expression.ReferencedSymbol != null
                    && IsCallableSymbol(expression.ReferencedSymbol))
                {
                    candidates.Add(expression.ReferencedSymbol);
                }
                else if (expression.ResultType is GroupSymbol group)
                {
                    candidates.AddRange(group.Symbols);
                }
                else
                {
                    candidates.Add(expression.ResultType);
                }
            }

            Symbol? calledSymbol = null;

            if (candidates.Count == 0)
            {
                diagnostics.Add(DiagnosticFactory.NoCallableSymbol());
            }
            else
            {
                calledSymbol = GetBestCalledSymbol(instance, arguments, candidates);

                if (calledSymbol == null || calledSymbol == CommonSymbols.Unknown)
                {
                    if (candidates.Count > 1)
                        diagnostics.Add(DiagnosticFactory.CallIsAmbiguous());
                }
                else if (!IsCallableSymbol(calledSymbol))
                {
                    diagnostics.Add(DiagnosticFactory.SymbolNotCallable(calledSymbol.Name));
                }
            }

            var resultType = calledSymbol != null ? GetCalledSymbolReturnType(calledSymbol) : null;

            if (expression == call.Expression
                && arguments == call.Arguments
                && call.CalledSymbol == calledSymbol
                && call.ResultType == resultType)
                return call;
            
            return new CallExpression(expression, arguments, calledSymbol, resultType, diagnostics.ToImmutableList());
        }
        finally
        {
            _symbolListPool.ReturnToPool(candidates);
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Expression BindCondition(ConditionExpression condition)
    {
        var test = Bind(condition.Test);
        var whenTrue = Bind(condition.WhenTrue);
        var whenFalse = Bind(condition.WhenFalse);

        var resultType = _symbols.GetUnion(whenTrue.ResultType, whenFalse.ResultType);

        if (test == condition.Test
            && whenTrue == condition.WhenTrue
            && whenFalse == condition.WhenFalse
            && resultType == condition.ResultType)
            return condition;

        return new ConditionExpression(test, whenTrue, whenFalse, resultType);
    }

    protected virtual Expression BindConstant(ConstantExpression constant)
    {
        var resultType = constant.Value == null
            ? CommonSymbols.Null
            : _symbols.GetType(constant.Value.GetType());

        if (resultType == constant.ResultType)
            return constant;

        return new ConstantExpression(constant.Value, resultType);
    }

    protected virtual Expression BindConvert(ConvertExpression convert)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            var expression = Bind(convert.Expression);

            var canConvert = CanConvert(convert.Kind, expression.ResultType, convert.ConvertedType);

            GetConversionOperatorCandidates(convert.Kind, expression.ResultType, convert.ConvertedType, candidates);

            if (convert.Expression == expression
                && (canConvert || convert.HasDiagnostics))
            {
                return convert;
            }

            return new ConvertExpression(
                convert.Kind,
                expression,
                convert.ConvertedType,
                diagnostics: !canConvert ? ImmutableList.Create(DiagnosticFactory.CannotConvert(expression.ResultType, convert.ConvertedType)) : null);
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _symbolListPool.ReturnToPool(candidates);
        }
    }

    protected virtual Expression BindDeclaration(DeclarationExpression declaration)
    {
        var initializer = Bind(declaration.Initializer);

        var resultType = initializer.ResultType;

        if (initializer == declaration.Initializer
            && declaration.Variable != null
            && declaration.Variable.VariableType == initializer.ResultType
            && declaration.ResultType == resultType)
        {
            return declaration;
        }

        var variable = declaration.Variable != null 
            && declaration.Variable.VariableType == initializer.ResultType
            ? declaration.Variable
            : new VariableSymbol(declaration.Name, initializer.ResultType);

        return new DeclarationExpression(declaration.Name, initializer, variable, resultType);
    }

    public virtual TypeSymbol? BindType(Expression typeExpression, List<Diagnostic>? diagnostics)
    {
        var expr = Bind(typeExpression);
        if (expr.ReferencedSymbol is TypeSymbol type)
        {
            return type;
        }
        else
        {
            // TODO: add diagnostic?
            return null;
        }
    }

    protected virtual ImmutableList<ParameterSymbol> BindParameters(ImmutableList<ParameterDeclaration> parameters, List<Diagnostic> diagnostics)
    {
        if (parameters.Count == 0)
            return ImmutableList<ParameterSymbol>.Empty;

        var list = new List<ParameterSymbol>();

        foreach (var p in parameters)
        {
            var type = p.ParameterType != null
                ? BindType(p.ParameterType, diagnostics) ?? CommonSymbols.Unknown
                : CommonSymbols.Any;

            list.Add(new ParameterSymbol(p.Name, type));
        }

        return list.ToImmutableList();
    }

    protected virtual Expression BindFunction(FunctionExpression function)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var parameters = function.Symbol != null
                ? function.Symbol.Parameters
                : BindParameters(function.Parameters, diagnostics);

            var parameterScope = this.CurrentScope.AddAmbientSymbols(parameters);

            var returnTarget = function.ReturnTarget ?? new TargetSymbol("return", CommonSymbols.Unknown);
            var bodyScope = parameterScope.AddAmbientSymbol(returnTarget);

            var body = BindInScope(function.Body, bodyScope);

            var returnType = GetFunctionResultType(body);

            if (body == function.Body
                && function.Symbol != null
                && function.ResultType == function.Symbol
                && function.ReturnType == returnType
                && diagnostics.Count == 0)
                return function;

            if (returnTarget.Type != body.ResultType)
            {
                returnTarget = new TargetSymbol(returnTarget.Name, returnType);
                bodyScope = parameterScope.AddAmbientSymbol(returnTarget);
                body = RebindInScope(body, bodyScope);
            }

            var symbol = (function.Symbol == null || function.ReturnType != returnType)
                ? new FunctionSymbol(function.Name, parameters, returnType)
                : function.Symbol;

            return new FunctionExpression(
                function.Name,
                function.Parameters,
                body,
                returnType,
                symbol,
                returnTarget,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }           
    }

    protected virtual TypeSymbol GetFunctionResultType(Expression body)
    {
        var returns = body.SelectWhere(
            s => !HasOwnBody(s),
            s => s is BranchExpression b && b.IsReturn,
            s => (BranchExpression)s);

        if (returns.Count == 0)
            return body.ResultType;

        return _symbols.GetUnion(returns.Select(r => r.ResultType).Concat(new[] { body.ResultType }));

        static bool HasOwnBody(Expression expression) =>
            expression switch
            {
                FunctionExpression f => true,
                ClassDeclaration m => true,
                _ => false,
            };
    }

    protected virtual Expression BindOperator(OperatorExpression opex)
    {
        var ops = _operators.GetOperators(opex.Kind);
        var referencedSymbol = _symbols.GetGroup(ops);

        if (referencedSymbol != null && referencedSymbol == opex.ReferencedSymbol)
            return opex;

        if (referencedSymbol == null)
        {
            return new OperatorExpression(
                opex.Kind,
                CommonSymbols.Unknown,
                CommonSymbols.Unknown,
                ImmutableList.Create(DiagnosticFactory.UnknownOperator(opex.Kind)));
        }
        else
        {
            var resultType = GetReferenceResultType(referencedSymbol);
            return new OperatorExpression(
                opex.Kind,
                referencedSymbol,
                resultType);
        }
    }

    protected virtual Expression BindPath(PathExpression path)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var members = _memberListPool.AllocateFromPool();

        try
        {
            var expression = Bind(path.Expression);

            if (expression.ResultType is GroupSymbol)
                diagnostics.Add(DiagnosticFactory.UnknownName(path.Reference.Name));

            var reference = BindPathReference(expression, path.Reference);

            if (path.Expression == expression
                && path.Reference == reference)
                return path;

            return new PathExpression(
                expression, 
                reference,
                diagnostics: diagnostics.Count > 0 ? diagnostics.ToImmutableList() : null);
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _memberListPool.ReturnToPool(members);
        }
    }

    protected virtual ReferenceExpression BindPathReference(Expression expression, ReferenceExpression reference)
    {
        var members = _symbolListPool.AllocateFromPool();

        try
        {
            if (expression.ReferencedSymbol is TypeSymbol type)
            {
                GetMatchingTypeMembers(
                    type, 
                    reference.Name, 
                    s => s is MemberSymbol m && m.IsStatic, 
                    members);
            }
            else
            {
                GetMatchingTypeMembers(
                    expression.ResultType, 
                    reference.Name, 
                    s => s is MemberSymbol m && !m.IsStatic, 
                    members);
            }

            Symbol? symbol = _symbols.GetGroup(members);

            return UpdateReference(reference, symbol);
        }
        finally
        {
            _symbolListPool.ReturnToPool(members);
        }
    }

    protected virtual Expression BindReference(ReferenceExpression reference)
    {
        var symbol = this.CurrentScope.FindSymbol<Symbol>(s => s.Name == reference.Name);
        return UpdateReference(reference, symbol);
    }

    protected virtual ReferenceExpression UpdateReference(ReferenceExpression reference, Symbol? referencedSymbol)
    {
        if (referencedSymbol != null && referencedSymbol == reference.ReferencedSymbol)
            return reference;

        if (referencedSymbol == null)
            return new ReferenceExpression(reference.Name, CommonSymbols.Unknown, CommonSymbols.Unknown, ImmutableList.Create(DiagnosticFactory.UnknownName(reference.Name)));

        var resultType = GetReferenceResultType(referencedSymbol);
        return new ReferenceExpression(reference.Name, referencedSymbol, resultType);
    }

    /// <summary>
    /// Determines the result type of a <see cref="ReferenceExpression"/> given the referenced symbol.
    /// </summary>
    protected virtual TypeSymbol GetReferenceResultType(Symbol? referencedSymbol) =>
        referencedSymbol switch
        {
            VariableSymbol v => v.VariableType,
            ParameterSymbol p => p.ParameterType,
            FieldSymbol f => f.FieldType,
            PropertySymbol p => p.PropertyType,
            FunctionSymbol f => f,
            GroupSymbol g => g,
            MethodSymbol => CommonSymbols.Void,
            TypeSymbol => _symbols.Type,
            _ => CommonSymbols.Unknown
        };

    private Expression BindWhile(WhileExpression loop)
    {
        var test = Bind(loop.Test);

        var breakTarget = loop.BreakTarget ?? new TargetSymbol("break", CommonSymbols.Void);
        var continueTarget = loop.ContinueTarget ?? new TargetSymbol("continue", CommonSymbols.Void);

        var bodyContext = this.CurrentScope.AddAmbientSymbols(breakTarget, continueTarget);
        var body = BindInScope(loop.Body, bodyContext);

        if (test == loop.Test
            && body == loop.Body
            && breakTarget == loop.BreakTarget
            && continueTarget == loop.ContinueTarget)
            return loop;

        if (breakTarget.Type != body.ResultType && !body.ContainsUnknowns)
        {
            breakTarget = new TargetSymbol(breakTarget.Name, body.ResultType);
            bodyContext = this.CurrentScope.AddAmbientSymbols(breakTarget, continueTarget);
            body = RebindInScope(loop.Body, bodyContext);
        }

        return new WhileExpression(
            test, 
            body, 
            body.ResultType,
            breakTarget, 
            continueTarget);
    }

    protected virtual ImmutableList<Expression> BindList(ImmutableList<Expression> expressions)
    {
        if (expressions.Count == 0)
            return expressions;

        return expressions.Rewrite(e => Bind(e));
    }

    /// <summary>
    /// Apply additional rewrites that may fix invalid expressions
    /// </summary>
    protected virtual Expression Reduce(Expression expression)
    {
        return expression;
    }

    protected virtual bool IsCallableSymbol(Symbol symbol) =>
        symbol is FunctionSymbol or MethodSymbol or ConstructorSymbol;

    protected virtual TypeSymbol? GetCalledSymbolReturnType(Symbol symbol) =>
        symbol switch
        {
            FunctionSymbol f => f.ReturnType,
            MethodSymbol m => m.ReturnType,
            ConstructorSymbol c => c.ReturnType,
            _ => null
        };

    /// <summary>
    /// Gets the list of candidate symbols for a call with the supplied arguments
    /// </summary>
    protected virtual void GetCalledSymbolCandidates(Expression instance, string name, ImmutableList<Expression> arguments, List<Symbol> candidates)
    {
        GetMatchingTypeMembers(instance.ResultType, name, s => MatchesParameters(s, arguments), candidates);
    }

    protected virtual Symbol? GetBestCalledSymbol(Expression instance, ImmutableList<Expression> arguments, List<Symbol> candidates)
    {
        // todo: be better
        return candidates.FirstOrDefault(c => MatchesParameters(c, arguments));
    }

    /// <summary>
    /// Returns true if the callable symbol has parameters that are compatible with the arguments
    /// </summary>
    protected virtual bool MatchesParameters(Symbol callableSymbol, ImmutableList<Expression> arguments) =>
        callableSymbol switch
        {
            FunctionSymbol function => MatchesParameters(function.Parameters, arguments),
            MethodSymbol method => MatchesParameters(method.Parameters, arguments),
            ConstructorSymbol constructor => MatchesParameters(constructor.Parameters, arguments),
            _ => false
        };

    protected virtual bool MatchesParameters(ImmutableList<ParameterSymbol> parameters, ImmutableList<Expression> arguments)
    {
        if (parameters.Count != arguments.Count)
            return false;

        for (int i = 0; i < parameters.Count; i++)
        {
            if (!MatchesParameter(parameters[i], arguments[i]))
                return false;
        }

        return true;
    }

    protected virtual bool MatchesParameter(ParameterSymbol parameter, Expression argument)
    {
        return parameter.ParameterType == CommonSymbols.Any
            || argument.ResultType == CommonSymbols.Unknown
            || parameter.ParameterType == argument.ResultType;
    }

    protected virtual Expression ConvertTo(Expression expression, TypeSymbol convertedType, ConversionKind kind)
    {
        if (expression.ResultType == convertedType
            || convertedType == CommonSymbols.Void
            || convertedType == CommonSymbols.Unknown)
            return expression;
        
        if (CanConvert(kind, expression.ResultType, convertedType))
            return new ConvertExpression(kind, expression, convertedType);

        return new ConvertExpression(kind, expression, convertedType, diagnostics: ImmutableList.Create(DiagnosticFactory.CannotConvert(expression.ResultType, convertedType)));
    }

    protected virtual bool CanConvert(ConversionKind conversion, TypeSymbol source, TypeSymbol target)
    {
        if (IsAssignableTo(source, target))
            return true;

        if (CanDownCast(source, target))
            return true; 

        if (conversion == ConversionKind.Narrowing)
        {
            return IsAssignableTo(target, source)
                || CanUpCast(source, target);
        }

        // does an implicit conversion exist
        return source == target; // other rule?
    }

    protected virtual bool CanDownCast(TypeSymbol source, TypeSymbol target)
    {
        if (source == target)
            return true;

        foreach (var sbt in source.BaseTypes)
        {
            if (CanDownCast(sbt, target))
                return true;
        }

        return false;
    }

    protected virtual bool CanUpCast(TypeSymbol source, TypeSymbol target)
    {
        if (target == source)
            return true;

        foreach (var tbt in target.BaseTypes)
        {
            if (CanUpCast(source, tbt))
                return true;
        }

        return false;
    }

    protected virtual bool IsAssignableTo(TypeSymbol source, TypeSymbol target)
    {
        if (source == target)
            return true;

        if (source.RuntimeType != null && target.RuntimeType != null)
            return source.RuntimeType.IsAssignableTo(target.RuntimeType);

        return false;
    }

    protected virtual void GetConversionOperatorCandidates(ConversionKind kind, TypeSymbol source, TypeSymbol target, List<Symbol> operators)
    {
        GetMatchingTypeMembers(source, null, s => IsMatchingConversionOperator(s, kind, source, target), operators);
        GetMatchingTypeMembers(target, null, s => IsMatchingConversionOperator(s, kind, source, target), operators);
    }

    protected virtual bool IsMatchingConversionOperator(Symbol symbol, ConversionKind kind, TypeSymbol source, TypeSymbol target) =>
        symbol switch
        {
            FunctionSymbol function =>
                function.ReturnType == target
                && function.Parameters.Count == 1
                && CanConvert(ConversionKind.Widening, source, function.Parameters[0].ParameterType),
            MethodSymbol method =>
                method.IsStatic
                && method.ReturnType == target
                && method.Parameters.Count == 1
                && CanConvert(ConversionKind.Widening, source, method.Parameters[0].ParameterType),
            _ => false
        };

    protected virtual Symbol? GetBestConversionOperator(Expression expression, TypeSymbol target, List<Symbol> candidates)
    {
        // todo: do better
        return candidates.FirstOrDefault();
    }

    protected virtual void GetMatchingTypeMembers(TypeSymbol type, string? name, Func<Symbol, bool>? fnMatch, List<Symbol> members)
    {
        TypeSymbol? symbol = type;
        int initialCount = members.Count;

        while (symbol != null)
        {
            if (name != null)
            {
                symbol.GetMembers(name, fnMatch, members);
            }
            else if (fnMatch != null)
            {
                symbol.GetMembers(fnMatch, members);
            }

            if (members.Count > initialCount)
                break;

            // look in base type
            // todo: handle interfaces separately
            symbol = symbol.BaseTypes.FirstOrDefault();
        }
    }
}