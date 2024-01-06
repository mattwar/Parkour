namespace Parkour.Semantics;

public class SemanticBinder<TScope>
    where TScope : IBindingScope<TScope>
{
    private readonly SymbolModel _symbols;
    private readonly Intrinsics _intrinsics;
    private bool _rebind;

    private readonly ObjectPool<List<Symbol>> _symbolListPool =
        new ObjectPool<List<Symbol>>(() => new List<Symbol>(), list => list.Clear());

    private readonly ObjectPool<List<Symbol.Member>> _memberListPool =
        new ObjectPool<List<Symbol.Member>>(() => new List<Symbol.Member>(), list => list.Clear());

    private readonly ObjectPool<List<Diagnostic>> _diagnosticListPool =
        new ObjectPool<List<Diagnostic>>(() => new List<Diagnostic>(), list => list.Clear());


    public SemanticBinder(SymbolModel symbols, Intrinsics intrinsics)
    {
        _symbols = symbols;
        _intrinsics = intrinsics;
    }

    /// <summary>
    /// Rebinds all expressions.
    /// </summary>
    public Semantic Rebind(Semantic expression, in TScope scope)
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
    public Semantic Bind(Semantic expression, in TScope scope)
    {
        if (!(_rebind || expression.ContainsUnknowns))
            return expression;

        switch (expression)
        {
            case Semantic.Block block:
                return BindBlock(block, scope);

            case Semantic.Branch branch:
                return BindBranch(branch, scope);

            case Semantic.Call call:
                return BindCall(call, scope);

            case Semantic.Condition condition:
                return BindCondition(condition, scope);

            case Semantic.Constant constant:
                return BindConstant(constant, scope);

            case Semantic.Convert convert:
                return BindConvert(convert, scope);

            case Semantic.Declaration declaration:
                return BindDeclaration(declaration, scope);

            case Semantic.Function function:
                return BindFunction(function, scope);

            case Semantic.Path path:
                return BindPath(path, scope);

            case Semantic.Reference reference:
                return BindReference(reference, scope);

            default:
                throw new InvalidOperationException($"Unhandled semantic '{expression.GetType().Name}' in {nameof(SemanticBinder<TScope>)} BindSymbols");
        }
    }

    public virtual Semantic BindBlock(Semantic.Block block, in TScope scope)
    {
        var rebind = false;

        // rebind expressions 
        var (newList, finalScope) = block.Expressions.Rewrite(scope, (expression, scope) =>
        {
            var boundExpression = rebind ? Rebind(expression, scope) : Bind(expression, scope);

            if (boundExpression is Semantic.Declaration decl
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

        return new Semantic.Block(newList);
    }

    public virtual Semantic BindBranch(Semantic.Branch branch, in TScope scope)
    {
        var expression = branch.Expression != null
            ? Bind(branch.Expression, scope)
            : null;

        var targetSymbol = scope.FindSymbol<Symbol.Target>(s => s.Name == branch.TargetName);
        var diagnostics =
            targetSymbol == null ? ImmutableList.Create(DiagnosticFactory.NoMatchingTarget(branch.TargetName))
            : null;

        if (expression == branch.Expression
            && targetSymbol == branch.Target)
            return branch;

        return new Semantic.Branch(branch.TargetName, expression, targetSymbol, diagnostics);
    }

    public virtual Semantic BindCall(Semantic.Call call, in TScope scope)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = Bind(call.Expression, scope);
            var arguments = Bind(call.Arguments, scope);

            var instance = expression;

            if (expression is Semantic.Path path) // xxx.Method?
            {
                instance = path.Expression;
                GetCalledSymbolCandidates(path.Expression, path.Reference.Name, arguments, scope, candidates);
            }

            if (candidates.Count == 0)
            {
                if (expression.ReferencedSymbol is Symbol.Group refGroup
                    && refGroup.Symbols.Any(IsCallableSymbol))
                {
                    candidates.AddRange(refGroup.Symbols);
                }
                else if (expression.ReferencedSymbol != null
                    && IsCallableSymbol(expression.ReferencedSymbol))
                {
                    candidates.Add(expression.ReferencedSymbol);
                }
                else if (expression.ResultType is Symbol.Group group)
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
            
            return new Semantic.Call(expression, arguments, calledSymbol, resultType, diagnostics.ToImmutableList());
        }
        finally
        {
            _symbolListPool.ReturnToPool(candidates);
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    public virtual Semantic BindCondition(Semantic.Condition condition, in TScope scope)
    {
        var test = Bind(condition.Test, scope);
        var whenTrue = Bind(condition.WhenTrue, scope);
        var whenFalse = Bind(condition.WhenFalse, scope);

        if (test == condition.Test
            && whenTrue == condition.WhenTrue
            && whenFalse == condition.WhenFalse)
            return condition;

        return SemanticFactory.Condition(test, whenTrue, whenFalse);
    }

    public virtual Semantic BindConstant(Semantic.Constant constant, in TScope scope)
    {
        var resultType = constant.Value == null
            ? SymbolModel.Null
            : _symbols.GetType(constant.Value.GetType());

        if (resultType == constant.ResultType)
            return constant;

        return new Semantic.Constant(constant.Value, resultType);
    }

    public virtual Semantic BindConvert(Semantic.Convert convert, in TScope scope)
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

            return new Semantic.Convert(
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

    public virtual Semantic BindDeclaration(Semantic.Declaration declaration, in TScope scope)
    {
        var initializer = Bind(declaration.Initializer, scope);

        if (initializer == declaration.Initializer
            && declaration.Variable != null
            && declaration.Variable.VariableType == initializer.ResultType)
        {
            return declaration;
        }

        var variable = declaration.Variable != null 
            && declaration.Variable.VariableType == initializer.ResultType
            ? declaration.Variable
            : new Symbol.Variable(declaration.Name, initializer.ResultType);

        return SemanticFactory.Declare(declaration.Name, initializer);
    }

    public virtual Semantic BindFunction(Semantic.Function function, in TScope scope)
    {
        var parameters = function.Symbol != null
            ? function.Symbol.Parameters
            : function.Parameters.Select(p => new Symbol.Parameter(p.Name, p.ParameterType)).ToImmutableList();

        var parameterScope = scope.AddAmbientSymbols(parameters);

        var returnTarget = function.ReturnTarget ?? new Symbol.Target("return", SymbolModel.Unknown);
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
            returnTarget = new Symbol.Target(returnTarget.Name, returnType);
            bodyScope = parameterScope.AddAmbientSymbol(returnTarget);
            body = Rebind(body, bodyScope);
        }

        var symbol = (function.Symbol == null || function.Symbol.ReturnType != returnType)
            ? new Symbol.Function(function.Name, parameters, returnType)
            : function.Symbol;

        return new Semantic.Function(
            function.Name, 
            function.Parameters, 
            body,
            returnType,
            symbol, 
            returnTarget);
    }

    public virtual Symbol.Type GetFunctionResultType(Semantic body)
    {
        var returns = body.SelectWhere(
            s => !HasOwnBody(s),
            s => s is Semantic.Branch b && b.IsReturn,
            s => (Semantic.Branch)s);

        if (returns.Count == 0)
            return body.ResultType;

        return _symbols.GetUnion(returns.Select(r => r.ResultType).Concat(new[] { body.ResultType }));
    }

    private static bool HasOwnBody(Semantic expression) =>
        expression switch
        {
            Semantic.Function f => true,
            Semantic.Class m => true,
            _ => false,
        };

    public virtual Semantic BindPath(Semantic.Path path, in TScope scope)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var members = _memberListPool.AllocateFromPool();

        try
        {
            var expression = Bind(path.Expression, scope);

            if (expression.ResultType is Symbol.Group)
                diagnostics.Add(DiagnosticFactory.UnknownName(path.Reference.Name));

            var reference = BindPathReference(expression, path.Reference);

            if (path.Expression == expression
                && path.Reference == reference)
                return path;

            return new Semantic.Path(
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

    public virtual Semantic.Reference BindPathReference(Semantic expression, Semantic.Reference reference)
    {
        var members = _memberListPool.AllocateFromPool();

        try
        {
            if (expression.ReferencedSymbol is Symbol.Type type)
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

    public virtual Semantic BindReference(Semantic.Reference reference, in TScope scope)
    {
        if (reference.ReferencedSymbol is Symbol.Function fn)
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

    public virtual Semantic.Reference UpdateReference(Semantic.Reference reference, Symbol? referencedSymbol)
    {
        if (referencedSymbol != null && referencedSymbol == reference.ReferencedSymbol)
            return reference;

        if (referencedSymbol == null)
            return new Semantic.Reference(reference.Name, SymbolModel.Unknown, SymbolModel.Unknown, ImmutableList.Create(DiagnosticFactory.UnknownName(reference.Name)));

        var resultType = GetReferenceResultType(referencedSymbol);
        return new Semantic.Reference(reference.Name, referencedSymbol, resultType);
    }

    public virtual Symbol.Type GetReferenceResultType(Symbol? symbol) =>
        symbol switch
        {
            Symbol.Variable v => v.VariableType,
            Symbol.Parameter p => p.ParameterType,
            Symbol.Field f => f.FieldType,
            Symbol.Function f => f,
            Symbol.Group g => _symbols.GetGroup(g.Members.Select(m => GetReferenceResultType(m))) as Symbol.Type ?? SymbolModel.Unknown,
            Symbol.Type t => _symbols.Type,
            _ => SymbolModel.Unknown
        };


    public Semantic BindWhile(Semantic.While loop, in TScope scope)
    {
        var test = Bind(loop.Test, scope);

        var breakTarget = loop.BreakTarget ?? new Symbol.Target("break", SymbolModel.Void);
        var continueTarget = loop.ContinueTarget ?? new Symbol.Target("continue", SymbolModel.Void);

        var bodyContext = scope.AddAmbientSymbols(breakTarget, continueTarget);
        var body = Bind(loop.Body, bodyContext);

        if (test == loop.Test
            && body == loop.Body
            && breakTarget == loop.BreakTarget
            && continueTarget == loop.ContinueTarget)
            return loop;

        if (breakTarget.Type != body.ResultType && !body.ContainsUnknowns)
        {
            breakTarget = new Symbol.Target(breakTarget.Name, body.ResultType);
            bodyContext = scope.AddAmbientSymbols(breakTarget, continueTarget);
            body = Rebind(loop.Body, bodyContext);
        }

        return new Semantic.While(
            test, 
            body, 
            body.ResultType,
            breakTarget, 
            continueTarget);
    }

    private ImmutableList<Semantic> Bind(ImmutableList<Semantic> expressions, in TScope scope)
    {
        if (expressions.Count == 0)
            return expressions;

        var tmpContext = scope;
        return expressions.Rewrite(e => Bind(e, tmpContext));
    }

    /// <summary>
    /// Apply additional rewrites that may fix invalid expressions
    /// </summary>
    public virtual Semantic Reduce(Semantic expression)
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

            if (currentContainer is Symbol.Member smember)
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

            if (currentContainer is Symbol.Member smember)
                currentContainer = smember.Container;
        }

        return null;
    }

    public Symbol.Member? FindMember(Symbol container, Func<Symbol.Member, bool> fnMatches) =>
        FindMember<Symbol.Member>(container, fnMatches);

    public virtual bool IsCallableSymbol(Symbol symbol) =>
        symbol is Symbol.Function or Symbol.Method or Symbol.Constructor;

    public virtual Symbol.Type? GetCalledSymbolReturnType(Symbol symbol) =>
        symbol switch
        {
            Symbol.Function f => f.ReturnType,
            Symbol.Method m => m.ReturnType,
            Symbol.Constructor c => c.ReturnType,
            _ => null
        };

    /// <summary>
    /// Gets the list of candidate symbols for a call with the supplied arguments
    /// </summary>
    public virtual void GetCalledSymbolCandidates(Semantic instance, string name, ImmutableList<Semantic> arguments, in TScope scope, List<Symbol> candidates)
    {
        FindMembers(instance.ResultType, s => s.Name == name && MatchesParameters(s, arguments), candidates);
    }

    public virtual Symbol? GetBestCalledSymbol(Semantic instance, ImmutableList<Semantic> arguments, List<Symbol> candidates)
    {
        // todo: be better
        return candidates.FirstOrDefault(c => MatchesParameters(c, arguments));
    }

    /// <summary>
    /// Returns true if the callable symbol has parameters that are compatible with the arguments
    /// </summary>
    public virtual bool MatchesParameters(Symbol callableSymbol, ImmutableList<Semantic> arguments) =>
        callableSymbol switch
        {
            Symbol.Function function => MatchesParameters(function.Parameters, arguments),
            Symbol.Method method => MatchesParameters(method.Parameters, arguments),
            Symbol.Constructor constructor => MatchesParameters(constructor.Parameters, arguments),
            _ => false
        };

    public virtual bool MatchesParameters(ImmutableList<Symbol.Parameter> parameters, ImmutableList<Semantic> arguments)
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

    public virtual bool MatchesParameter(Symbol.Parameter parameter, Semantic argument)
    {
        return parameter.ParameterType == SymbolModel.Any
            || argument.ResultType == SymbolModel.Unknown
            || parameter.ParameterType == argument.ResultType;
    }

    public virtual Semantic ConvertTo(Semantic expression, Symbol.Type convertedType, Semantic.ConversionKind kind)
    {
        if (expression.ResultType == convertedType
            || convertedType == SymbolModel.Void
            || convertedType == SymbolModel.Unknown)
            return expression;
        
        if (CanConvert(kind, expression.ResultType, convertedType))
            return new Semantic.Convert(kind, expression, convertedType);

        return new Semantic.Convert(kind, expression, convertedType, diagnostics: ImmutableList.Create(DiagnosticFactory.CannotConvert(expression.ResultType, convertedType)));
    }

    public virtual bool CanConvert(Semantic.ConversionKind conversion, Symbol.Type source, Symbol.Type target)
    {
        if (IsAssignableTo(source, target))
            return true;

        if (CanDownCast(source, target))
            return true; 

        if (conversion == Semantic.ConversionKind.Narrowing)
        {
            return IsAssignableTo(target, source)
                || CanUpCast(source, target);
        }

        // does an implicit conversion exist
        return source == target; // other rule?
    }

    public virtual bool CanDownCast(Symbol.Type source, Symbol.Type target)
    {
        for (var s = source; s != null; s = s.BaseType)
        {
            if (s == target)
                return true;
        }

        return false;
    }

    public virtual bool CanUpCast(Symbol.Type source, Symbol.Type target)
    {
        for (var t = target; t != null; t = t.BaseType)
        {
            if (t == source)
                return true;
        }

        return false;
    }

    public virtual bool IsAssignableTo(Symbol.Type source, Symbol.Type target)
    {
        if (source == target)
            return true;

        if (source.RuntimeType != null && target.RuntimeType != null)
            return source.RuntimeType.IsAssignableTo(target.RuntimeType);

        return false;
    }

    public virtual void GetConversionOperatorCandidates(Semantic.ConversionKind kind, Symbol.Type source, Symbol.Type target, in TScope scope, List<Symbol> operators)
    {
        FindMembers(source, s => IsMatchingConversionOperator(s, kind, source, target), operators);
        FindMembers(target, s => IsMatchingConversionOperator(s, kind, source, target), operators);
    }

    public virtual bool IsMatchingConversionOperator(Symbol symbol, Semantic.ConversionKind kind, Symbol.Type source, Symbol.Type target) =>
        symbol switch
        {
            Symbol.Function function =>
                function.ReturnType == target
                && function.Parameters.Count == 1
                && CanConvert(Semantic.ConversionKind.Widening, source, function.Parameters[0].ParameterType),
            Symbol.Method method =>
                method.IsStatic
                && method.ReturnType == target
                && method.Parameters.Count == 1
                && CanConvert(Semantic.ConversionKind.Widening, source, method.Parameters[0].ParameterType),
            _ => false
        };

    public virtual Symbol? GetBestConversionOperator(Semantic expression, Symbol.Type target, List<Symbol> candidates)
    {
        // todo: do better
        return candidates.FirstOrDefault();
    }
}