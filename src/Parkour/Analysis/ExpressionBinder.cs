namespace Parkour.Analysis;
using Expressions;
using Symbols;

public class ExpressionBinder<TScope>
    where TScope : IBindingScope<TScope>
{
    private readonly SymbolModel _symbols;
    private readonly Intrinsics _intrinsics;
    private bool _rebind;

    private readonly ObjectPool<List<Symbol>> _symbolListPool =
        new ObjectPool<List<Symbol>>(() => new List<Symbol>(), list => list.Clear());

    private readonly ObjectPool<List<MemberSymbol>> _memberListPool =
        new ObjectPool<List<MemberSymbol>>(() => new List<MemberSymbol>(), list => list.Clear());

    private readonly ObjectPool<List<Diagnostic>> _diagnosticListPool =
        new ObjectPool<List<Diagnostic>>(() => new List<Diagnostic>(), list => list.Clear());


    public ExpressionBinder(SymbolModel symbols, Intrinsics intrinsics)
    {
        _symbols = symbols;
        _intrinsics = intrinsics;
    }

    /// <summary>
    /// Rebinds all expressions.
    /// </summary>
    public Expression Rebind(Expression expression, in TScope scope)
    {
        var oldRebind = _rebind;
        _rebind = true;
        var rebound = Bind(expression, scope);
        _rebind = oldRebind;
        return rebound;
    }

    /// <summary>
    /// Binds all unbound expressions.
    /// </summary>
    public Expression Bind(Expression expression, in TScope scope)
    {
        if (!(_rebind || expression.ContainsUnknowns))
            return expression;

        switch (expression)
        {
            case BlockExpression block:
                return BindBlock(block, scope);

            case BranchExpression branch:
                return BindBranch(branch, scope);

            case CallExpression call:
                return BindCall(call, scope);

            case ConditionExpression condition:
                return BindCondition(condition, scope);

            case ConstantExpression constant:
                return BindConstant(constant, scope);

            case ConvertExpression convert:
                return BindConvert(convert, scope);

            case DeclarationExpression declaration:
                return BindDeclaration(declaration, scope);

            case FunctionExpression function:
                return BindFunction(function, scope);

            case PathExpression path:
                return BindPath(path, scope);

            case ReferenceExpression reference:
                return BindReference(reference, scope);

            default:
                throw new InvalidOperationException($"Unhandled semantic '{expression.GetType().Name}' in {nameof(ExpressionBinder<TScope>)} BindSymbols");
        }
    }

    public virtual Expression BindBlock(BlockExpression block, in TScope scope)
    {
        var rebind = false;

        // rebind expressions 
        var (newList, finalScope) = block.Expressions.Rewrite(scope, (expression, scope) =>
        {
            var boundExpression = rebind ? Rebind(expression, scope) : Bind(expression, scope);

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

        if (newList == block.Expressions)
            return block;

        return new BlockExpression(newList);
    }

    public virtual Expression BindBranch(BranchExpression branch, in TScope scope)
    {
        var expression = branch.Expression != null
            ? Bind(branch.Expression, scope)
            : null;

        var targetSymbol = scope.FindSymbol<TargetSymbol>(s => s.Name == branch.TargetName);
        var diagnostics =
            targetSymbol == null ? ImmutableList.Create(DiagnosticFactory.NoMatchingTarget(branch.TargetName))
            : null;

        if (expression == branch.Expression
            && targetSymbol == branch.Target)
            return branch;

        return new BranchExpression(branch.TargetName, expression, targetSymbol, diagnostics);
    }

    public virtual Expression BindCall(CallExpression call, in TScope scope)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = Bind(call.Expression, scope);
            var arguments = Bind(call.Arguments, scope);

            var instance = expression;

            if (expression is PathExpression path) // xxx.Method?
            {
                instance = path.Expression;
                GetCalledSymbolCandidates(path.Expression, path.Reference.Name, arguments, scope, candidates);
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

                if (calledSymbol == null || calledSymbol == SymbolModel.Unknown)
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

    public virtual Expression BindCondition(ConditionExpression condition, in TScope scope)
    {
        var test = Bind(condition.Test, scope);
        var whenTrue = Bind(condition.WhenTrue, scope);
        var whenFalse = Bind(condition.WhenFalse, scope);

        var resultType = _symbols.GetUnion(whenTrue.ResultType, whenFalse.ResultType);

        if (test == condition.Test
            && whenTrue == condition.WhenTrue
            && whenFalse == condition.WhenFalse
            && resultType == condition.ResultType)
            return condition;

        return new ConditionExpression(test, whenTrue, whenFalse, resultType);
    }

    public virtual Expression BindConstant(ConstantExpression constant, in TScope scope)
    {
        var resultType = constant.Value == null
            ? SymbolModel.Null
            : _symbols.GetType(constant.Value.GetType());

        if (resultType == constant.ResultType)
            return constant;

        return new ConstantExpression(constant.Value, resultType);
    }

    public virtual Expression BindConvert(ConvertExpression convert, in TScope scope)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            var expression = Bind(convert.Expression, scope);

            var canConvert = CanConvert(convert.Kind, expression.ResultType, convert.ConvertedType);

            GetConversionOperatorCandidates(convert.Kind, expression.ResultType, convert.ConvertedType, scope, candidates);


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

    public virtual Expression BindDeclaration(DeclarationExpression declaration, in TScope scope)
    {
        var initializer = Bind(declaration.Initializer, scope);

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

    public virtual Expression BindFunction(FunctionExpression function, in TScope scope)
    {
        var parameters = function.Symbol != null
            ? function.Symbol.Parameters
            : function.Parameters.Select(p => new ParameterSymbol(p.Name, p.ParameterType)).ToImmutableList();

        var parameterScope = scope.AddAmbientSymbols(parameters);

        var returnTarget = function.ReturnTarget ?? new TargetSymbol("return", SymbolModel.Unknown);
        var bodyScope = parameterScope.AddAmbientSymbol(returnTarget);

        var body = Bind(function.Body, bodyScope);

        var returnType = GetFunctionResultType(body);

        if (body == function.Body
            && function.Symbol != null
            && function.ResultType == function.Symbol
            && function.ReturnType == returnType)
            return function;

        if (returnTarget.Type != body.ResultType)
        {
            returnTarget = new TargetSymbol(returnTarget.Name, returnType);
            bodyScope = parameterScope.AddAmbientSymbol(returnTarget);
            body = Rebind(body, bodyScope);
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
            returnTarget);
    }

    public virtual TypeSymbol GetFunctionResultType(Expression body)
    {
        var returns = body.SelectWhere(
            s => !HasOwnBody(s),
            s => s is BranchExpression b && b.IsReturn,
            s => (BranchExpression)s);

        if (returns.Count == 0)
            return body.ResultType;

        return _symbols.GetUnion(returns.Select(r => r.ResultType).Concat(new[] { body.ResultType }));
    }

    private static bool HasOwnBody(Expression expression) =>
        expression switch
        {
            FunctionExpression f => true,
            ClassDeclaration m => true,
            _ => false,
        };

    public virtual Expression BindPath(PathExpression path, in TScope scope)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var members = _memberListPool.AllocateFromPool();

        try
        {
            var expression = Bind(path.Expression, scope);

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

    public virtual ReferenceExpression BindPathReference(Expression expression, ReferenceExpression reference)
    {
        var members = _memberListPool.AllocateFromPool();

        try
        {
            if (expression.ReferencedSymbol is TypeSymbol type)
                FindMembers(type, m => m.IsStatic && m.Name == reference.Name, members);
            else
                FindMembers(expression.ResultType, m => !m.IsStatic && m.Name == reference.Name, members);

            Symbol? symbol = _symbols.GetGroup(members);

            return UpdateReference(reference, symbol);
        }
        finally
        {
            _memberListPool.ReturnToPool(members);
        }
    }

    public virtual Expression BindReference(ReferenceExpression reference, in TScope scope)
    {
        if (reference.ReferencedSymbol is FunctionSymbol fn)
        {
            var intrinsics = _intrinsics.GetOperatorIntrinsics(fn);
            if (intrinsics != null)
            {
                return UpdateReference(reference, _symbols.GetGroup(intrinsics));
            }
        }

        var symbol = scope.FindSymbol(reference.Name);

        return UpdateReference(reference, symbol);
    }

    public virtual ReferenceExpression UpdateReference(ReferenceExpression reference, Symbol? referencedSymbol)
    {
        if (referencedSymbol != null && referencedSymbol == reference.ReferencedSymbol)
            return reference;

        if (referencedSymbol == null)
            return new ReferenceExpression(reference.Name, SymbolModel.Unknown, SymbolModel.Unknown, ImmutableList.Create(DiagnosticFactory.UnknownName(reference.Name)));

        var resultType = GetReferenceResultType(referencedSymbol);
        return new ReferenceExpression(reference.Name, referencedSymbol, resultType);
    }

    public virtual TypeSymbol GetReferenceResultType(Symbol? symbol) =>
        symbol switch
        {
            VariableSymbol v => v.VariableType,
            ParameterSymbol p => p.ParameterType,
            FieldSymbol f => f.FieldType,
            FunctionSymbol f => f,
            GroupSymbol g => _symbols.GetGroup(g.Members.Select(m => GetReferenceResultType(m))) as TypeSymbol ?? SymbolModel.Unknown,
            TypeSymbol t => _symbols.Type,
            _ => SymbolModel.Unknown
        };

    public Expression BindWhile(WhileExpression loop, in TScope scope)
    {
        var test = Bind(loop.Test, scope);

        var breakTarget = loop.BreakTarget ?? new TargetSymbol("break", SymbolModel.Void);
        var continueTarget = loop.ContinueTarget ?? new TargetSymbol("continue", SymbolModel.Void);

        var bodyContext = scope.AddAmbientSymbols(breakTarget, continueTarget);
        var body = Bind(loop.Body, bodyContext);

        if (test == loop.Test
            && body == loop.Body
            && breakTarget == loop.BreakTarget
            && continueTarget == loop.ContinueTarget)
            return loop;

        if (breakTarget.Type != body.ResultType && !body.ContainsUnknowns)
        {
            breakTarget = new TargetSymbol(breakTarget.Name, body.ResultType);
            bodyContext = scope.AddAmbientSymbols(breakTarget, continueTarget);
            body = Rebind(loop.Body, bodyContext);
        }

        return new WhileExpression(
            test, 
            body, 
            body.ResultType,
            breakTarget, 
            continueTarget);
    }

    private ImmutableList<Expression> Bind(ImmutableList<Expression> expressions, in TScope scope)
    {
        if (expressions.Count == 0)
            return expressions;

        var tmpContext = scope;
        return expressions.Rewrite(e => Bind(e, tmpContext));
    }

    /// <summary>
    /// Apply additional rewrites that may fix invalid expressions
    /// </summary>
    public virtual Expression Reduce(Expression expression)
    {
        return expression;
    }

    public void FindSymbols<Symbol>(ImmutableList<Symbol> symbols, Func<Symbol, bool> fnMatches, List<Symbol> list)
    {
        foreach (var symbol in symbols)
        {
            if (fnMatches(symbol))
                list.Add(symbol);
        }
    }

    public void FindMembers<TMember>(Symbol container, Func<TMember, bool> fnMatches, List<TMember> members)
        where TMember : Symbol
    {
        var currentContainer = container;
        while (currentContainer != null)
        {
            foreach (var member in currentContainer.Members)
            {
                if (member is TMember tmember && fnMatches(tmember))
                    members.Add(tmember);
            }

            if (currentContainer is MemberSymbol smember)
                currentContainer = smember.Container;
        }
    }

    public void FindMembers(Symbol container, Func<Symbol, bool> fnMatches, List<Symbol> members) =>
        FindMembers<Symbol>(container, fnMatches, members);

    public TMember? FindMember<TMember>(Symbol container, Func<TMember, bool> fnMatches)
        where TMember : Symbol
    {
        var currentContainer = container;
        while (currentContainer != null)
        {
            foreach (var member in currentContainer.Members)
            {
                if (member is TMember tmember && fnMatches(tmember))
                    return tmember;
            }

            if (currentContainer is MemberSymbol smember)
                currentContainer = smember.Container;
        }

        return null;
    }

    public MemberSymbol? FindMember(Symbol container, Func<MemberSymbol, bool> fnMatches) =>
        FindMember<MemberSymbol>(container, fnMatches);

    public virtual bool IsCallableSymbol(Symbol symbol) =>
        symbol is FunctionSymbol or MethodSymbol or ConstructorSymbol;

    public virtual TypeSymbol? GetCalledSymbolReturnType(Symbol symbol) =>
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
    public virtual void GetCalledSymbolCandidates(Expression instance, string name, ImmutableList<Expression> arguments, in TScope scope, List<Symbol> candidates)
    {
        FindMembers(instance.ResultType, s => s.Name == name && MatchesParameters(s, arguments), candidates);
    }

    public virtual Symbol? GetBestCalledSymbol(Expression instance, ImmutableList<Expression> arguments, List<Symbol> candidates)
    {
        // todo: be better
        return candidates.FirstOrDefault(c => MatchesParameters(c, arguments));
    }

    /// <summary>
    /// Returns true if the callable symbol has parameters that are compatible with the arguments
    /// </summary>
    public virtual bool MatchesParameters(Symbol callableSymbol, ImmutableList<Expression> arguments) =>
        callableSymbol switch
        {
            FunctionSymbol function => MatchesParameters(function.Parameters, arguments),
            MethodSymbol method => MatchesParameters(method.Parameters, arguments),
            ConstructorSymbol constructor => MatchesParameters(constructor.Parameters, arguments),
            _ => false
        };

    public virtual bool MatchesParameters(ImmutableList<ParameterSymbol> parameters, ImmutableList<Expression> arguments)
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

    public virtual bool MatchesParameter(ParameterSymbol parameter, Expression argument)
    {
        return parameter.ParameterType == SymbolModel.Any
            || argument.ResultType == SymbolModel.Unknown
            || parameter.ParameterType == argument.ResultType;
    }

    public virtual Expression ConvertTo(Expression expression, TypeSymbol convertedType, ConversionKind kind)
    {
        if (expression.ResultType == convertedType
            || convertedType == SymbolModel.Void
            || convertedType == SymbolModel.Unknown)
            return expression;
        
        if (CanConvert(kind, expression.ResultType, convertedType))
            return new ConvertExpression(kind, expression, convertedType);

        return new ConvertExpression(kind, expression, convertedType, diagnostics: ImmutableList.Create(DiagnosticFactory.CannotConvert(expression.ResultType, convertedType)));
    }

    public virtual bool CanConvert(ConversionKind conversion, TypeSymbol source, TypeSymbol target)
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

    public virtual bool CanDownCast(TypeSymbol source, TypeSymbol target)
    {
        for (var s = source; s != null; s = s.BaseType)
        {
            if (s == target)
                return true;
        }

        return false;
    }

    public virtual bool CanUpCast(TypeSymbol source, TypeSymbol target)
    {
        for (var t = target; t != null; t = t.BaseType)
        {
            if (t == source)
                return true;
        }

        return false;
    }

    public virtual bool IsAssignableTo(TypeSymbol source, TypeSymbol target)
    {
        if (source == target)
            return true;

        if (source.RuntimeType != null && target.RuntimeType != null)
            return source.RuntimeType.IsAssignableTo(target.RuntimeType);

        return false;
    }

    public virtual void GetConversionOperatorCandidates(ConversionKind kind, TypeSymbol source, TypeSymbol target, in TScope scope, List<Symbol> operators)
    {
        FindMembers(source, s => IsMatchingConversionOperator(s, kind, source, target), operators);
        FindMembers(target, s => IsMatchingConversionOperator(s, kind, source, target), operators);
    }

    public virtual bool IsMatchingConversionOperator(Symbol symbol, ConversionKind kind, TypeSymbol source, TypeSymbol target) =>
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

    public virtual Symbol? GetBestConversionOperator(Expression expression, TypeSymbol target, List<Symbol> candidates)
    {
        // todo: do better
        return candidates.FirstOrDefault();
    }
}