namespace Parkour.Binding;
using Semantics;
using Symbols;
using System;

public class ExpressionBinder
{
    private readonly SymbolCache _symbols;
    private readonly Operators _operators;
    private readonly BindingScope _defaultScope;

    #region pools
    private readonly ObjectPool<List<Symbol>> _symbolListPool =
        new ObjectPool<List<Symbol>>(() => new List<Symbol>(), list => list.Clear());

    private readonly ObjectPool<List<TypeSymbol>> _typeListPool =
        new ObjectPool<List<TypeSymbol>>(() => new List<TypeSymbol>(), list => list.Clear());

    private readonly ObjectPool<List<MemberSymbol>> _memberListPool =
        new ObjectPool<List<MemberSymbol>>(() => new List<MemberSymbol>(), list => list.Clear());

    private readonly ObjectPool<List<LabelExpression>> _labelListPool =
        new ObjectPool<List<LabelExpression>>(() => new List<LabelExpression>(), list => list.Clear());

    private readonly ObjectPool<List<BranchExpression>> _branchListPool =
        new ObjectPool<List<BranchExpression>>(() => new List<BranchExpression>(), list => list.Clear());

    private readonly ObjectPool<List<Diagnostic>> _diagnosticListPool =
        new ObjectPool<List<Diagnostic>>(() => new List<Diagnostic>(), list => list.Clear());
    #endregion

    public ExpressionBinder(NamespaceSymbol externalSymbols)
    {
        _symbols = SymbolCache.From(externalSymbols);
        _operators = Operators.From(_symbols);
        _defaultScope = BindingScope.Default.AddMembers(externalSymbols);
    }

    /// <summary>
    /// Binds all unbound expressions.
    /// </summary>
    public Expression Bind(Expression expression, BindingScope? scope = null, bool rebind = false)
    {
        return BindExpression(expression, new BindingContext(scope ?? _defaultScope, rebind, null, _symbols.Void));
    }

    /// <summary>
    /// Binds all the expressions in a list.
    /// </summary>
    public ImmutableList<TExpression> BindList<TExpression>(
        ImmutableList<TExpression> expressions, BindingScope? scope = null, bool rebind = false)
        where TExpression : Expression
    {
        var context = new BindingContext(scope ?? _defaultScope, rebind, null, _symbols.Void);
        return BindExpressionList(expressions, context);
    }

    /// <summary>
    /// Gets the type of a type expression.
    /// </summary>
    public virtual TypeSymbol? GetType(Expression typeExpression, BindingScope? scope = null, bool rebind = false)
    {
        var context = new BindingContext(scope ?? _defaultScope, rebind, null, _symbols.Void);
        return BindType(typeExpression, context);
    }

    protected struct BindingContext
    {
        public BindingScope Scope { get; }
        public bool Rebind { get; }
        public TypeSymbol InflowType { get; }
        public TypeSymbol? TargetType { get; }

        public BindingContext(BindingScope scope, bool rebind, TypeSymbol? targetType, TypeSymbol inflowType)
        {
            this.Scope = scope;
            this.Rebind = rebind;
            this.TargetType = targetType;
            this.InflowType = inflowType;
        }

        public BindingContext WithScope(BindingScope scope) =>
            new BindingContext(scope, this.Rebind, this.TargetType, this.InflowType);

        public BindingContext WithRebind(bool rebind) =>
            new BindingContext(this.Scope, this.Rebind, this.TargetType, this.InflowType);

        public BindingContext WithInflowType(TypeSymbol inflowType) =>
            new BindingContext(this.Scope, this.Rebind, this.TargetType, inflowType);

        public BindingContext WithTargetType(TypeSymbol? targetType) =>
            new BindingContext(this.Scope, this.Rebind, targetType, this.InflowType);
    };

    /// <summary>
    /// Binds all unbound expressions.
    /// </summary>
    private Expression BindExpression(Expression expression, BindingContext context)
    {
        if (!(context.Rebind || expression.IsUnbound))
            return expression;

        switch (expression)
        {
            case ArrayExpression array:
                return BindArray(array, context);

            case ArityExpression arity:
                return BindArity(arity, context);

            case AssignExpression assign:
                return BindAssign(assign, context);

            case BlockExpression block:
                return BindBlock(block, context);

            case BranchExpression branch:
                return BindBranch(branch, context);

            case CallExpression call:
                return BindCall(call, context);

            case ConditionExpression condition:
                return BindCondition(condition, context);

            case ConstantExpression constant:
                return BindConstant(constant, context);

            case TypeArgumentsExpression construct:
                return BindConstruct(construct, context);

            case ConvertExpression convert:
                return BindConvert(convert, context);

            case DefaultExpression dex:
                return BindDefault(dex, context);

            case LabelExpression label:
                return BindLabel(label, context);

            case LambdaExpression lambda:
                return BindLambda(lambda, context);

            case LoopExpression loop:
                return BindLoop(loop, context);

            case MemberExpression member:
                return BindMember(member, context);

            case NameReferenceExpression nameRef:
                return BindNameReference(nameRef, context);

            case NewExpression @new:
                return BindNew(@new, context);

            case NewArrayInitExpression newArrayInit:
                return BindNewArrayInit(newArrayInit, context);

            case NewArraySizeExpression newArraySize:
                return BindNewArraySize(newArraySize, context);

            case OperatorExpression opex:
                return BindOperator(opex, context);

            case SymbolReferenceExpression symbolRef:
                return BindSymbolReference(symbolRef, context);

            case VariableExpression variable:
                return BindVariable(variable, context);

            case VoidExpression vex:
                return BindVoid(vex, context);

            default:
                throw new InvalidOperationException($"Unhandled semantic '{expression.GetType().Name}' in {nameof(ExpressionBinder)}.BindExpression");
        }
    }

    protected virtual ImmutableList<TExpression> BindExpressionList<TExpression>(
        ImmutableList<TExpression> expressions, BindingContext context)
        where TExpression : Expression
    {
        if (expressions.Count == 0)
            return expressions;
        return expressions.Rewrite(e => (TExpression)BindExpression(e, context));
    }

    protected virtual Expression BindArray(ArrayExpression array, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(array.Expression, context);
            var elementType = expression.ReferencedSymbol as TypeSymbol;
            var referencedSymbol = elementType != null ? _symbols.GetArray(elementType) : null;
            var resultType = GetReferenceResultType(referencedSymbol);

            if (elementType == null)
            {
                diagnostics.Add(BindingDiagnostics.ReferencedSymbolNotType().WithLocation(array.Location));
            }

            if (expression == array.Expression
                && referencedSymbol == array.ReferencedSymbol
                && resultType == array.ResultType
                && diagnostics.Count == 0)
                return array;

            return new ArrayExpression(
                expression,
                array.Location,
                referencedSymbol,
                resultType,
                diagnostics.ToImmutableList()
                );
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Expression BindArity(ArityExpression arity, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(arity.Expression, context);

            Symbol? referencedSymbol = null;
            if (expression.ReferencedSymbol is GroupSymbol group)
            {
                referencedSymbol = _symbols.GetGroup(group.Symbols.Where(s => s.Arity == arity.Arity));
            }
            else if (expression.ReferencedSymbol is Symbol symbol)
            {
                referencedSymbol = symbol.Arity == arity.Arity ? symbol : null;
            }

            if (referencedSymbol == null)
            {
                diagnostics.Add(BindingDiagnostics.NoReferencedSymbolsHaveMatchingArity().WithLocation(arity.Location));

            }

            var resultType = GetReferenceResultType(referencedSymbol);

            if (expression == arity.Expression
                && referencedSymbol == arity.ReferencedSymbol
                && resultType == arity.ResultType
                && diagnostics.Count == 0)
                return arity;

            return new ArityExpression(
                expression,
                arity.Arity,
                arity.Location,
                referencedSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Expression BindAssign(AssignExpression assign, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var target = BindExpression(assign.Target, context.WithTargetType(null));
            var assignType = GetReferenceResultType(target.ReferencedSymbol) ?? _symbols.Object;
            var source = BindExpression(assign.Source, context.WithTargetType(assignType));
            source = ConvertTo(source, assignType, context);

            if (IsValidTargetSymbol(assign.ReferencedSymbol))
            {
                diagnostics.Add(BindingDiagnostics.NotAValidAssignmentTarget().WithLocation(assign.Target.Location));
            }

            if (target == assign.Target
                && source == assign.Source
                && assign.ResultType == target.ResultType
                && assign.Diagnostics.Count == 0
                && diagnostics.Count == 0)
                return target;

            return new AssignExpression(
                target, 
                source,
                assign.Location,
                diagnostics.ToImmutableList()
                );
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual bool IsValidTargetSymbol(Symbol? symbol) =>
        symbol switch
        {
            VariableSymbol => true,
            FieldSymbol => true,
            PropertySymbol => true,
            _ => false
        };

    private readonly Dictionary<LabelExpression, LabelSymbol> _labelToSymbolMap
        = new Dictionary<LabelExpression, LabelSymbol>();

    protected void SetAssociatedLabelSymbol(LabelExpression label, LabelSymbol symbol)
    {
        _labelToSymbolMap[label] = symbol;
    }

    protected LabelSymbol? GetAssociatedLabelSymbol(LabelExpression label)
    {
        _labelToSymbolMap.TryGetValue(label, out var target);
        return target;
    }

    protected virtual Expression BindBlock(BlockExpression block, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var labelSymbols = _symbolListPool.AllocateFromPool();
        var types = _typeListPool.AllocateFromPool();

        try
        {
            // look for labels and put their target symbols into scope
            var labelContext = context.WithTargetType(null);

            foreach (var expr in block.Expressions)
            {
                if (expr is LabelExpression label)
                {
                    var type = label.ReceivingType != null ? BindType(label.ReceivingType, labelContext) : _symbols.Void;
                    var labelSymbol = label.LabelSymbol ?? new LabelSymbol(label.Name, type);
                    labelSymbols.Add(labelSymbol);
                    SetAssociatedLabelSymbol(label, labelSymbol);
                }
            }

            context = context.WithScope(context.Scope.AddSymbols(labelSymbols));

            // bind expressions 
            (var boundExpressions, _) = block.Expressions.Rewrite(context, (expression, _context) =>
            {
                var boundExpression = BindExpression(expression, _context);

                if (boundExpression is VariableExpression decl
                    && decl.Variable != null)
                {
                    // if the declaration changes then the variable is now different 
                    // so the rest of the block needs to be rebound in case it references the old variable
                    _context = _context.WithRebind(context.Rebind || boundExpression != expression);
                    _context = _context.WithScope(context.Scope.AddSymbol(decl.Variable));
                }

                _context = _context.WithInflowType(boundExpression.ResultType);

                return (boundExpression, _context);
            });

            var resultType = boundExpressions.Count > 0
                ? boundExpressions[^1].ResultType
                : SpecialSymbols.Void;

            if (boundExpressions == block.Expressions
                && block.ResultType == resultType
                && diagnostics.Count == 0)
                return block;

            return new BlockExpression(
                boundExpressions,
                block.Location,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _typeListPool.ReturnToPool(types);
        }
    }

    /// <summary>
    /// Gets all the branch expression types.
    /// </summary>
    protected virtual void GetBranchExpressionTypes(Expression body, LabelSymbol target, List<TypeSymbol> types)
    {
        types.AddRange(
            body.SelectWhere(
                s => s is Expression e && !HasBody(e),
                s => s is BranchExpression b && b.LabelSymbol == target,
                s => ((BranchExpression)s).Expression != null ? ((BranchExpression)s).Expression!.ResultType : SpecialSymbols.Void));
    }

    /// <summary>
    /// Gets all the branch expression types.
    /// </summary>
    protected virtual void GetBranchExpressionTypes(ImmutableList<Expression> expressions, LabelSymbol target, List<TypeSymbol> types)
    {
        foreach (var expr in expressions)
        {
            GetBranchExpressionTypes(expr, target, types);
        }
    }

    protected virtual bool HasBody(Expression expression) =>
        expression is LambdaExpression;


    protected virtual Expression BindBranch(BranchExpression branch, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var labelSymbol = GetBranchLabel(branch.LabelName, context);

            var expression = branch.Expression != null
                ? BindExpression(branch.Expression, context.WithTargetType(labelSymbol?.Type))
                : null;

            var expressionType = expression != null ? expression.ResultType : _symbols.Void;

            if (labelSymbol == null)
            {
                diagnostics.Add(BindingDiagnostics.NoMatchingTarget(branch.LabelName).WithLocation(branch.Location));
            }
            else if (expression != null && expressionType != labelSymbol.Type)
            {
                expression = ConvertTo(expression, labelSymbol.Type, context);
                expression = BindExpression(expression, context.WithRebind(true));
            }

            if (expression == branch.Expression
                && labelSymbol == branch.LabelSymbol
                && diagnostics.Count == 0)
                return branch;

            return new BranchExpression(
                branch.LabelName,
                expression,
                branch.Location,
                labelSymbol,
                _symbols.DoesNotReturn,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual LabelSymbol? GetBranchLabel(string name, BindingContext context)
    {
        return context.Scope.FindMatchingSymbol<LabelSymbol>(name, null);
    }

    protected virtual Expression BindCall(CallExpression call, BindingContext context)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithTargetType(null).WithInflowType(_symbols.Void);

            var expression = BindExpression(call.Expression, context);
            var arguments = BindExpressionList(call.Arguments, context);

            if (expression is LambdaExpression lambda)
            {
                if (lambda.LambdaSymbol != null)
                {
                    candidates.Add(lambda.LambdaSymbol);
                }
            }
            else
            {
                var instance = GetCallInstance(expression);
                var referencedSymbol = expression.ReferencedSymbol;
                if (referencedSymbol != null)
                    GetCalledSymbolCandidates(referencedSymbol, arguments, candidates);
            }

            var location = expression.Location;
            Symbol? calledSymbol = null;

            if (candidates.Count == 0)
            {
                diagnostics.Add(BindingDiagnostics.NoCallableSymbol().WithLocation(location));
            }
            else
            {
                calledSymbol = GetBestCalledSymbol(arguments, candidates);

                if (calledSymbol == null)
                {
                    diagnostics.Add(BindingDiagnostics.CallIsAmbiguous().WithLocation(location));
                }
                else
                {
                    var parameters = GetCallableSymbolParameters(calledSymbol);
                    if (parameters.Count != arguments.Count)
                    {
                        diagnostics.Add(BindingDiagnostics.IncorrectNumberOfArguments().WithLocation(location));
                    }
                    else
                    {
                        arguments = ConvertArguments(parameters, arguments, context);
                    }
                }
            }

            var resultType = calledSymbol != null 
                ? GetCalledSymbolReturnType(calledSymbol) 
                : null;

            if (expression == call.Expression
                && arguments == call.Arguments
                && call.CalledSymbol == calledSymbol
                && call.ResultType == resultType
                && diagnostics.Count == 0)
                return call;
            
            return new CallExpression(
                expression, 
                arguments, 
                call.Location,
                calledSymbol, 
                resultType, 
                diagnostics.ToImmutableList());
        }
        finally
        {
            _symbolListPool.ReturnToPool(candidates);
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Expression? GetCallInstance(Expression expression)
    {
        switch (expression)
        {
            case MemberExpression member:
                return member.Expression;
            case AdjustedReferenceExpression filter:
                return GetCallInstance(filter.Expression);
            default:
                return expression;
        }
    }

    protected virtual bool IsCallableSymbol(Symbol symbol) =>
        symbol is LambdaSymbol or MethodSymbol or ConstructorSymbol;

    protected virtual TypeSymbol? GetCalledSymbolReturnType(Symbol symbol) =>
        symbol switch
        {
            LambdaSymbol f => f.ReturnType,
            MethodSymbol m => m.ReturnType,
            ConstructorSymbol c => c.ReturnType,
            _ => null
        };

    /// <summary>
    /// Gets the list of candidate symbols for a call with the supplied arguments
    /// </summary>
    protected virtual void GetCalledSymbolCandidates(
        Symbol symbol,
        ImmutableList<Expression> arguments,
        List<Symbol> candidates)
    {
        if (symbol is GroupSymbol group)
        {
            candidates.AddRange(group.Symbols.Where(s => IsCallableSymbol(s) && MatchesParameters(s, arguments)));
        }
        else if (IsCallableSymbol(symbol))
        {
            candidates.Add(symbol);
        }
    }

    protected virtual Symbol? GetBestCalledSymbol(ImmutableList<Expression> arguments, List<Symbol> candidates)
    {
        // todo: be better
        return candidates.FirstOrDefault(c => MatchesParameters(c, arguments));
    }

    /// <summary>
    /// Get the <see cref="ParameterSymbol"/> declarations for the callable symbol
    /// </summary>
    protected virtual ImmutableList<ParameterSymbol> GetCallableSymbolParameters(Symbol callableSymbol) =>
        callableSymbol switch
        {
            LambdaSymbol function => function.Parameters,
            MethodSymbol method => method.Parameters,
            ConstructorSymbol constructor => constructor.Parameters,
            _ => ImmutableList<ParameterSymbol>.Empty
        };

    /// <summary>
    /// Returns true if the callable symbol has parameters that are compatible with the arguments
    /// </summary>
    protected virtual bool MatchesParameters(Symbol callableSymbol, ImmutableList<Expression> arguments) =>
        MatchesParameters(GetCallableSymbolParameters(callableSymbol), arguments);

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
        return parameter.ParameterType == _symbols.Any
            || argument.ResultType == _symbols.Unknown
            || parameter.ParameterType == argument.ResultType;
    }

    protected virtual ImmutableList<Expression> ConvertArguments(
        ImmutableList<ParameterSymbol> parameters, 
        ImmutableList<Expression> arguments,
        BindingContext context)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var argument = arguments[i];
            var convertedArg = ConvertTo(argument, parameter.ParameterType, context);
            arguments = arguments.SetItem(i, convertedArg);
        }

        return arguments;
    }

    protected virtual Expression BindCondition(ConditionExpression condition, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithInflowType(_symbols.Void);

            var test = BindExpression(condition.Test, context.WithTargetType(_symbols.Boolean));
            test = ConvertTo(test, _symbols.Boolean, context);

            var whenTrue = BindExpression(condition.WhenTrue, context);
            var whenFalse = BindExpression(condition.WhenFalse, context);

            var resultType = GetBestCommonType([whenTrue.ResultType, whenFalse.ResultType, context.TargetType], voidIsBetter: false);
            if (resultType == null)
            {
                diagnostics.Add(BindingDiagnostics.NoCommonTypeFound().WithLocation(condition.Location));
                resultType = _symbols.Object;
            }

            whenTrue = ConvertTo(whenTrue, resultType, context);
            whenFalse = ConvertTo(whenFalse, resultType, context);

            if (test == condition.Test
                && whenTrue == condition.WhenTrue
                && whenFalse == condition.WhenFalse
                && resultType == condition.ResultType
                && diagnostics.Count == 0)
                return condition;

            return new ConditionExpression(
                test,
                whenTrue,
                whenFalse,
                condition.Location,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected TypeSymbol? GetBestCommonType(IReadOnlyList<Expression> expressions, bool voidIsBetter = false)
    {
        var types = _typeListPool.AllocateFromPool();
        try
        {
            types.AddRange(
                expressions
                .Select(e => e.ResultType)
                );

            return GetBestCommonType(types, voidIsBetter);
        }
        finally
        {
            _typeListPool.ReturnToPool(types);
        }
    }

    protected TypeSymbol? GetBestCommonType(params TypeSymbol?[] types) =>
        GetBestCommonType((IReadOnlyList<TypeSymbol?>)types);

    protected virtual TypeSymbol? GetBestCommonType(IReadOnlyList<TypeSymbol?> types, bool voidIsBetter = false)
    {
        TypeSymbol? best = PickBest();

        if (best != null && IsVoidLike(best))
            return best;

        if (best != null && StillBest(best))
            return best;

        return null;

        TypeSymbol? PickBest()
        {
            TypeSymbol? best = null;

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];

                if (type == null || IgnoreType(type))
                    continue;

                if (best == null
                    || (type != best && IsBetterThanOrSame(type, best)))
                {
                    best = type;
                }
            }

            return best;
        }

        // check if best is better than all types
        bool StillBest(TypeSymbol best)
        {
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];

                if (type == null || IgnoreType(type))
                    continue;

                if (!IsBetterThanOrSame(best, type))
                    return false;
            }

            return true;
        }

        bool IsBetterThanOrSame(TypeSymbol type, TypeSymbol? best)
        {
            if (type == best)
                return true;

            if (best == null
                || (IsVoidLike(type) && voidIsBetter)
                || (IsVoidLike(best) && !voidIsBetter))
            {
                return true;
            }

            // if type cannot be assigned to best
            if (!HasConversion(ConversionKind.Widening, type, best)
                && HasConversion(ConversionKind.Widening, best, type))
            {
                return true;
            }

            return false;
        }

        bool IsVoidLike(TypeSymbol type) =>
            type == SpecialSymbols.Void
            || type == SpecialSymbols.DoesNotReturn;

        bool IgnoreType(TypeSymbol type) =>
            type == _symbols.Null
            || type == _symbols.Unknown;
    }

    protected virtual Expression BindConstant(ConstantExpression constant, BindingContext context)
    {
        var resultType = constant.Value == null
            ? _symbols.Null
            : _symbols.GetType(constant.Value.GetType());

        if (resultType == constant.ResultType)
            return constant;

        return new ConstantExpression(
            constant.Value, 
            constant.Location,
            resultType, 
            null);
    }

    protected virtual Expression BindConstruct(TypeArgumentsExpression construct, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(construct.Expression, context);
            var typeArguments = BindExpressionList(construct.TypeArguments, context);

            Symbol? constructedSymbol = null;
            if (expression.ReferencedSymbol is Symbol symbol)
            {
                var typeArgs = typeArguments
                    .Select(ta => BindType(ta, context))
                    .OfType<TypeSymbol>()
                    .ToImmutableList();

                constructedSymbol = ConstructSymbol(symbol, typeArgs, construct.Location, diagnostics);
            }

            var resultType = GetReferenceResultType(constructedSymbol);

            if (expression == construct.Expression
                && typeArguments == construct.TypeArguments
                && constructedSymbol == construct.ConstructedSymbol
                && resultType == construct.ResultType
                && diagnostics.Count == 0)
                return construct;

            return new TypeArgumentsExpression(
                expression,
                typeArguments,
                construct.Location,
                constructedSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    /// <summary>
    /// Makes a constructed symbol from a generic type definition
    /// </summary>
    protected virtual Symbol? ConstructSymbol(
        Symbol symbol, 
        ImmutableList<TypeSymbol> typeArguments, 
        ISourceLocation? location = null, 
        List<Diagnostic>? diagnostics = null)
    {
        if (symbol is GroupSymbol group)
        {
            var constructableSymbols = group.Symbols.Where(s => s.Arity == typeArguments.Count).ToList();
            if (constructableSymbols.Count == 0)
            {
                if (diagnostics != null)
                    diagnostics.Add(BindingDiagnostics.NoTypeOrMethodWithMatchingArityToConstruct().WithLocation(location));
                return null;
            }

            var constructedSymbols = 
                group.Symbols
                .Select(s => ConstructSymbol(s, typeArguments))
                .OfType<Symbol>()
                .ToImmutableList();
            
            return _symbols.GetGroup(constructedSymbols);
        }
        else if (symbol is TypeSymbol type)
        {
            if (type.Arity != typeArguments.Count)
            {
                if (diagnostics != null)
                    diagnostics.Add(BindingDiagnostics.TypeDoesNotHaveMatchingArity().WithLocation(location));
                return null;
            }

            return _symbols.GetConstructed(type, typeArguments);
        }
        else if (symbol is MethodSymbol method)
        {
            if (method.Arity != typeArguments.Count)
            {
                if (diagnostics != null)
                    diagnostics.Add(BindingDiagnostics.MethodDoesNotHaveMatchingArity().WithLocation(location));
                return null;
            }
            return _symbols.GetConstructed(method, typeArguments);
        }
        else
        {
            if (diagnostics != null)
                diagnostics.Add(BindingDiagnostics.NoTypeOrMethodWithMatchingArityToConstruct().WithLocation(location));
        }

        return null;
    }

    #region Conversion
    protected virtual Expression BindConvert(ConvertExpression convert, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            context = context.WithInflowType(_symbols.Void);

            var convertedType = convert.ConvertedType != null
                ? BindExpression(convert.ConvertedType, context.WithTargetType(null))
                : null;

            var type = convertedType != null
                ? convertedType.ReferencedSymbol as TypeSymbol
                : convert.ResultType;

            var expression = BindExpression(convert.Expression, context.WithTargetType(type));

            if (convert.ConvertedType == null
                && convert.ResultType != null
                && IsAssignableTo(expression.ResultType, convert.ResultType))
            {
                // remove unnecessary non-explicit conversion
                return expression;
            }

            TryGetConversion(convert.Kind, expression.ResultType, type, out var conversionSymbol, expression.Location, diagnostics);

            if (convert.Expression == expression
                && convert.ConvertedType == convertedType
                && convert.ConversionSymbol == conversionSymbol
                && convert.ResultType == type
                && diagnostics.Count == 0)
            {
                return convert;
            }

            return new ConvertExpression(
                convert.Kind,
                expression,
                convertedType,
                convert.Location,
                conversionSymbol,
                type,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _symbolListPool.ReturnToPool(candidates);
        }
    }

    protected virtual Expression ConvertTo(Expression expression, TypeSymbol type, BindingContext context)
    {
        // ignore void
        if (type == SpecialSymbols.Void
            || expression.ResultType == SpecialSymbols.Void)
            return expression;

        // remove unnecessary conversions added by this method on prior bindings
        while (expression is ConvertExpression ce
            && ce.ConvertedType == null // added by binding
            && IsAssignableTo(ce.Expression.ResultType, type))
        {
            expression = ce.Expression;
        }

        if (IsAssignableTo(expression.ResultType, type))
            return expression;

        // wrap expression with widening conversion and bind it.
        var convert = new ConvertExpression(
            ConversionKind.Widening,
            expression,
            convertedType: null,
            expression.Location,
            conversionSymbol: null,
            resultType: type,
            diagnostics: null);

        return BindConvert(convert, context);
    }

    protected virtual bool TryGetConversion(
        ConversionKind kind,
        TypeSymbol sourceType,
        TypeSymbol? targetType,
        out Symbol? conversionSymbol,
        ISourceLocation? location,
        List<Diagnostic>? diagnostics)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            if (targetType == null)
            {
                diagnostics?.Add(BindingDiagnostics.CannotConvert(sourceType, _symbols.Unknown).WithLocation(location));
                conversionSymbol = null;
                return false;
            }
            else if (HasIntrinsicConversion(kind, sourceType, targetType))
            {
                conversionSymbol = null;
                return true;
            }
            else
            {
                GetConversionOperatorCandidates(kind, sourceType, targetType, candidates);
                conversionSymbol = GetBestConversionOperator(sourceType, targetType, candidates);

                if (conversionSymbol == null)
                {
                    diagnostics?.Add(BindingDiagnostics.CannotConvert(sourceType, targetType ?? _symbols.Unknown).WithLocation(location));
                    return false;
                }

                return true;
            }
        }
        finally
        {
            _symbolListPool.ReturnToPool(candidates);
        }
    }

    protected virtual bool HasConversion(ConversionKind kind, TypeSymbol sourceType, TypeSymbol targetType) =>
        TryGetConversion(kind, sourceType, targetType, out _, null, null);

    /// <summary>
    /// Determines if the conversion can be done through intrinsic means (not custom conversion).
    /// </summary>
    protected virtual bool HasIntrinsicConversion(ConversionKind kind, TypeSymbol sourceType, TypeSymbol targetType)
    {
        if (IsAssignableTo(sourceType, targetType))
            return true;

        if (CanDownCast(sourceType, targetType))
            return true;

        if (kind == ConversionKind.Narrowing)
        {
            return IsAssignableTo(targetType, sourceType)
                || CanUpCast(sourceType, targetType);
        }

        return sourceType == targetType
            || CanWiden(sourceType, targetType);
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

    protected virtual bool CanWiden(TypeSymbol source, TypeSymbol target)
    {
        if (target == _symbols.Int64)
        {
            return source == _symbols.Int32
                || source == _symbols.Int16
                || source == _symbols.Byte;
        }
        else if (target == _symbols.Int32)
        {
            return source == _symbols.Int16
                || source == _symbols.Byte;
        }
        else if (target == _symbols.Int16)
        {
            return source == _symbols.Byte;
        }
        else if (target == _symbols.Double)
        {
            return source == _symbols.Single
                || CanWiden(source, _symbols.Int64);
        }
        else if (target == _symbols.Single)
        {
            return CanWiden(source, _symbols.Int64);
        }
        else if (target == _symbols.Decimal)
        {
            return CanWiden(source, _symbols.Double);
        }
        return false;
    }

    /// <summary>
    /// The source type is assignable to the target type without conversion.
    /// </summary>
    protected virtual bool IsAssignableTo(TypeSymbol sourceType, TypeSymbol targetType)
    {
        if (sourceType == targetType)
            return true;

        if (sourceType == _symbols.DoesNotReturn)
            return true;

        if (targetType == _symbols.Any)
            return true;

        if (sourceType.RuntimeType != null
            && targetType.RuntimeType != null
            && sourceType.RuntimeType.IsAssignableTo(targetType.RuntimeType))
            return true;

        if (CanDownCast(sourceType, targetType))
            return true;

        return false;
    }

    protected virtual void GetConversionOperatorCandidates(ConversionKind kind, TypeSymbol source, TypeSymbol target, List<Symbol> operators)
    {
        GetMatchingTypeMembers(source, "op_Implicit", s => IsMatchingConversionOperator(s, kind, source, target), operators);
        GetMatchingTypeMembers(target, "op_Implicit", s => IsMatchingConversionOperator(s, kind, source, target), operators);

        if (kind == ConversionKind.Narrowing)
        {
            GetMatchingTypeMembers(source, "op_Explicit", s => IsMatchingConversionOperator(s, kind, source, target), operators);
            GetMatchingTypeMembers(target, "op_Explicit", s => IsMatchingConversionOperator(s, kind, source, target), operators);
        }
    }

    protected virtual bool IsMatchingConversionOperator(Symbol symbol, ConversionKind kind, TypeSymbol source, TypeSymbol target) =>
        symbol switch
        {
            LambdaSymbol function =>
                function.ReturnType == target
                && function.Parameters.Count == 1
                && HasConversion(ConversionKind.Widening, source, function.Parameters[0].ParameterType),
            MethodSymbol method =>
                method.IsStatic
                && method.ReturnType == target
                && method.Parameters.Count == 1
                && HasConversion(ConversionKind.Widening, source, method.Parameters[0].ParameterType),
            _ => false
        };

    protected virtual Symbol? GetBestConversionOperator(TypeSymbol sourceType, TypeSymbol targetType, IReadOnlyList<Symbol> candidates)
    {
        // todo: do better
        return candidates.FirstOrDefault();
    }
    #endregion

    protected virtual Expression BindDefault(DefaultExpression dex, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            Expression? typeExpr = null;
            TypeSymbol? resultType = null;

            if (dex.TypeExpression != null)
            {
                typeExpr = dex.TypeExpression != null ? BindExpression(dex.TypeExpression, context.WithTargetType(null)) : null;
                resultType = typeExpr != null ? typeExpr.ReferencedSymbol as TypeSymbol : null;
            }
            else if (context.TargetType != null)
            {
                resultType = context.TargetType;
            }
            else
            {
                diagnostics.Add(BindingDiagnostics.DefaultTypeCannotBeInferred().WithLocation(dex.Location));
                resultType = _symbols.Any;
            }

            if (typeExpr == dex.TypeExpression
                && resultType == dex.ResultType
                && diagnostics.Count == 0)
                return dex;

            return new DefaultExpression(typeExpr, dex.Location, resultType, diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Expression BindLabel(LabelExpression label, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var receivingType = label.ReceivingType != null ? BindExpression(label.ReceivingType, context) : null;
            var resultType = receivingType != null ? receivingType.ReferencedSymbol as TypeSymbol : _symbols.Void;
            var targetSymbol = GetAssociatedLabelSymbol(label) ?? new LabelSymbol(label.Name, resultType);

            // check for inflow types into labels
            if (resultType != _symbols.Void)
            {
                // if label is expecting to receive a value, then inflow type must match 
                if (!TryGetConversion(ConversionKind.Widening, context.InflowType, resultType, out _, label.Location, null))
                {
                    diagnostics.Add(BindingDiagnostics.FlowIntoLabelDoesNotMatchType().WithLocation(label.Location));
                }
            }

            if (label.ReceivingType == receivingType
                && label.LabelSymbol == targetSymbol
                && label.ResultType == resultType)
                return label;

            return new LabelExpression(
                label.Name,
                receivingType,
                label.Location,
                targetSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Expression BindLambda(LambdaExpression lambda, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var types = _typeListPool.AllocateFromPool();
        try
        {
            LambdaSymbol? lambdaSymbol = lambda.LambdaSymbol;
            LabelSymbol? returnTarget = lambda.ReturnTarget 
                ?? new LabelSymbol(LabelSymbol.ReturnLabelName, _symbols.Any);
            ImmutableList<ParameterDeclaration> parameters = lambda.Parameters;
            Expression body = lambda.Body;
            TypeSymbol? returnType = null;

            context = context.WithInflowType(_symbols.Void);

            BindLambdaSymbol(context);

            if (returnTarget.Type != returnType)
            {
                context = context.WithRebind(true);
                returnTarget = new LabelSymbol(LabelSymbol.ReturnLabelName, returnType);
                BindLambdaSymbol(context);
            }
           
            if (parameters == lambda.Parameters
                && body == lambda.Body
                && lambda.LambdaSymbol != null
                && lambda.LambdaSymbol.ReturnType == body.ResultType
                && lambda.ReturnType == returnType
                && diagnostics.Count == 0)
                return lambda;

            return new LambdaExpression(
                lambda.Name,
                parameters ?? ImmutableList<ParameterDeclaration>.Empty,
                body,
                lambda.Location,
                returnType,
                lambdaSymbol,
                returnTarget,
                diagnostics.ToImmutableList());

            void BindLambdaSymbol(BindingContext context)
            {
                // bind and evalute new function symbol at the same time
                lambdaSymbol = new LambdaSymbol(
                    lambda.Name,
                    me =>
                    {
                        var pms = CreateParameterSymbols(me, context);
                        BindBodyAndReturnType(pms, context);
                        return pms;
                    },
                    () => returnType!,
                    null);
                // force eval of deferred parameters and return type
                // for side-effect assignment to locals  (Erik Meijer said it was okay.)
                var _ = lambdaSymbol.Parameters;
                returnType = lambdaSymbol.ReturnType;
            }

            ImmutableList<ParameterSymbol> CreateParameterSymbols(
                Symbol? declaringSymbol,
                BindingContext context)
            {
                if (parameters.Count == 0)
                    return ImmutableList<ParameterSymbol>.Empty;

                var symbols = new List<ParameterSymbol>();
                var declarations = new List<ParameterDeclaration>();

                context = context.WithTargetType(null);

                foreach (var p in parameters)
                {
                    var type = p.ParameterType != null ? BindExpression(p.ParameterType, context) : null;
                    var ptype = type?.ReferencedSymbol as TypeSymbol ?? _symbols.Any;
                    var psymbol = new ParameterSymbol(p.Name, declaringSymbol, ptype, runtimeParameter: null);
                    var pdecl = new ParameterDeclaration(p.Name, type, p.Location, psymbol, null);
                    symbols.Add(psymbol);
                    declarations.Add(pdecl);
                }

                parameters = declarations.ToImmutableList();
                return symbols.ToImmutableList();
            }

            void BindBodyAndReturnType(ImmutableList<ParameterSymbol> parameters, BindingContext context)
            {
                var bodyContext = context.WithScope(
                    context.Scope
                        .AddSymbols(parameters)
                        .AddSymbol(returnTarget));
                body = BindExpression(lambda.Body, bodyContext);
                returnType = GetLambdaResultType(body, returnTarget, diagnostics);
            }
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _typeListPool.ReturnToPool(types);
        }           
    }



    protected virtual TypeSymbol GetLambdaResultType(Expression body, LabelSymbol returnTarget, List<Diagnostic> diagnostics)
    {
        var types = _typeListPool.AllocateFromPool();
        try
        {
            GetBranchExpressionTypes(body, returnTarget, types);
            types.Add(body.ResultType);

            var best = GetBestCommonType(types, voidIsBetter: false) ?? _symbols.Object;
            return best;
        }
        finally
        {
            _typeListPool.ReturnToPool(types);
        }
    }

    protected virtual Expression BindLoop(LoopExpression loop, BindingContext context)
    {
        var diagostics = _diagnosticListPool.AllocateFromPool();
        var types = _typeListPool.AllocateFromPool();
        try
        {
            var breakTarget = loop.BreakTarget ?? new LabelSymbol(LabelSymbol.BreakLabelName, _symbols.Any);
            var continueTarget = loop.ContinueTarget ?? new LabelSymbol(LabelSymbol.ContinueLabelName, _symbols.Void);

            var bodyContext = context.WithScope(
                context.Scope.AddSymbols(new[] { breakTarget, continueTarget }));

            context = context.WithInflowType(_symbols.Void);
            var body = BindExpression(loop.Body, bodyContext);

            // result type is the common type of all the break branches.
            GetBranchExpressionTypes(body, breakTarget, types);
            var resultType = GetBestCommonType(types, voidIsBetter: false) ?? SpecialSymbols.Void;

            if (breakTarget.Type != resultType)
            {
                breakTarget = new LabelSymbol(breakTarget.Name, resultType);
                bodyContext = context
                    .WithRebind(true)
                    .WithScope(context.Scope.AddSymbols(new[] { breakTarget, continueTarget }));
                body = BindExpression(loop.Body, bodyContext);
            }

            if (body == loop.Body
                && resultType == loop.ResultType
                && breakTarget == loop.BreakTarget
                && continueTarget == loop.ContinueTarget
                && diagostics.Count == 0)
                return loop;

            return new LoopExpression(
                body,
                loop.Location,
                resultType,
                breakTarget,
                continueTarget,
                diagostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagostics);
            _typeListPool.ReturnToPool(types);
        }
    }

    protected virtual Expression BindOperator(OperatorExpression opex, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var ops = _operators.GetOperators(opex.Kind);
            var referencedSymbol = _symbols.GetGroup(ops);

            if (referencedSymbol != null && referencedSymbol == opex.ReferencedSymbol)
                return opex;

            if (referencedSymbol == null)
                diagnostics.Add(BindingDiagnostics.UnknownOperator(opex.Kind).WithLocation(opex.Location));

            var resultType = GetReferenceResultType(referencedSymbol) ?? _symbols.Object;

            return new OperatorExpression(
                opex.Kind,
                opex.Location,
                referencedSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Expression BindMember(MemberExpression member, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithInflowType(_symbols.Void);

            var expression = BindExpression(member.Expression, context.WithTargetType(null));

            if (expression.ResultType is GroupSymbol)
                diagnostics.Add(BindingDiagnostics.UnknownName(member.Name).WithLocation(member.Location));

            var referencedSymbol = GetReferencedMember(expression, member.Name, diagnostics);

            var resultType = GetReferenceResultType(referencedSymbol);

            if (member.Expression == expression
                && member.ReferencedSymbol == referencedSymbol
                && member.ResultType == resultType
                && diagnostics.Count == 0)
                return member;

            return new MemberExpression(
                expression, 
                member.Name,
                member.Location,
                referencedSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Symbol? GetReferencedMember(
        Expression expression, 
        string name,
        List<Diagnostic>? diagnositcs)
    {
        var members = _symbolListPool.AllocateFromPool();
        try
        {
            if (expression.ReferencedSymbol is TypeSymbol type)
            {
                GetMatchingTypeMembers(
                    type,
                    name,
                    s => s is MemberSymbol m && m.IsStatic,
                    members);
            }
            else if (expression.ReferencedSymbol is ContainerSymbol container)
            {
                container.GetMembers(
                    name, 
                    s => s is MemberSymbol m,
                    members);
            }
            else
            {
                GetMatchingTypeMembers(
                    expression.ResultType,
                    name,
                    s => s is MemberSymbol m && !m.IsStatic,
                    members);
            }

            return _symbols.GetGroup(members);
        }
        finally
        {
            _symbolListPool.ReturnToPool(members);
        }
    }

    protected virtual Expression BindNameReference(NameReferenceExpression nameRef, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var referencedSymbol = GetNameReference(nameRef.Name, context);

            if (referencedSymbol != null && referencedSymbol == nameRef.ReferencedSymbol)
                return nameRef;

            if (referencedSymbol == null)
                diagnostics.Add(BindingDiagnostics.UnknownName(nameRef.Name).WithLocation(nameRef.Location));

            var resultType = GetReferenceResultType(referencedSymbol) ?? _symbols.Object;

            return new NameReferenceExpression(
                nameRef.Name,
                nameRef.Location,
                referencedSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    /// <summary>
    /// Get the symbol that is referenced by the name.
    /// </summary>
    protected virtual Symbol? GetNameReference(string name, BindingContext context)
    {
        var symbols = _symbolListPool.AllocateFromPool();
        try
        {
            context.Scope.FindMatchingSymbols(name, null, symbols, FindScope.First);
            return _symbols.GetGroup(symbols);
        }
        finally
        {
            _symbolListPool.ReturnToPool(symbols);
        }
    }

    protected virtual Expression BindNew(NewExpression nex, BindingContext context)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var argContext = context.WithTargetType(null).WithInflowType(_symbols.Void);

            var typeExpression = nex.TypeExpression != null ? BindExpression(nex.TypeExpression, argContext) : null;
            var arguments = BindExpressionList(nex.Arguments, argContext);
            var referencedType = (typeExpression?.ReferencedSymbol ?? context.TargetType) as TypeSymbol;

            if (referencedType != null)
                GetConstructorCandidates(referencedType, arguments, candidates);

            var location = nex.Location;
            ConstructorSymbol? constructorSymbol = null;

            if (candidates.Count == 0)
            {
                diagnostics.Add(BindingDiagnostics.NoConstructorFound().WithLocation(location));
            }
            else
            {
                constructorSymbol = GetBestConstructor(candidates);

                if (constructorSymbol == null)
                {
                    diagnostics.Add(BindingDiagnostics.ConstructorsAreAmbiguous().WithLocation(location));
                }
                else if (constructorSymbol.Parameters.Count != arguments.Count)
                {
                    diagnostics.Add(BindingDiagnostics.IncorrectNumberOfArguments().WithLocation(location));
                }
                else
                {
                    arguments = ConvertArguments(constructorSymbol.Parameters, arguments, context);
                }
            }

            var resultType = constructorSymbol?.ReturnType;

            if (typeExpression == nex.TypeExpression
                && arguments == nex.Arguments
                && constructorSymbol == nex.ConstructorSymbol
                && resultType == nex.ResultType
                && diagnostics.Count == 0)
                return nex;

            return new NewExpression(
                typeExpression,
                arguments,
                nex.Location,
                constructorSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _symbolListPool.ReturnToPool(candidates);
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    /// <summary>
    /// Gets the list of candidate symbols for a call with the supplied arguments
    /// </summary>
    protected virtual void GetConstructorCandidates(
        TypeSymbol type,
        ImmutableList<Expression> arguments,
        List<Symbol> candidates)
    {
        var symbols = _symbolListPool.AllocateFromPool();
        try
        {
            type.GetMembers(".ctor", symbols);

            candidates.AddRange(
                symbols
                .OfType<ConstructorSymbol>()
                .Where(c => !c.IsStatic && MatchesParameters(c, arguments))
                );
        }
        finally
        {
            _symbolListPool.ReturnToPool(symbols);
        }
    }

    protected virtual ConstructorSymbol? GetBestConstructor(List<Symbol> candidates)
    {
        // TODO: get good
        return candidates.OfType<ConstructorSymbol>().FirstOrDefault();
    }

    protected virtual Expression BindNewArraySize(NewArraySizeExpression newArraySize, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var targetType = context.TargetType;
            context = context.WithTargetType(null);
            var elementType = newArraySize.ElementType != null ? BindExpression(newArraySize.ElementType, context) : null;
            var size = BindExpression(newArraySize.Size, context);

            var targetElementType = targetType is ArraySymbol asym ? asym.ElementType : null;
            var elementTypeSymbol = elementType?.ReferencedSymbol as TypeSymbol
                ?? targetElementType;
            var resultType = elementTypeSymbol != null ? _symbols.GetArray(elementTypeSymbol) : null;

            if (elementTypeSymbol == null)
            {
                diagnostics.Add(BindingDiagnostics.CannotInferElementType().WithLocation(newArraySize.Location));
            }

            if (elementType == newArraySize.ElementType
                && size == newArraySize.Size
                && elementTypeSymbol == newArraySize.ElementTypeSymbol
                && resultType == newArraySize.ResultType
                && diagnostics.Count == 0)
                return newArraySize;

            return new NewArraySizeExpression(
                elementType,
                size,
                newArraySize.Location,
                elementTypeSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Expression BindNewArrayInit(NewArrayInitExpression newArrayInit, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var targetType = context.TargetType;
            context = context.WithTargetType(null);
            var elementType = newArrayInit.ElementType != null ? BindExpression(newArrayInit.ElementType, context) : null;
            var expressions = BindExpressionList(newArrayInit.Expressions, context);

            var targetElementType = targetType is ArraySymbol asym ? asym.ElementType : null;
            var elementTypeSymbol = elementType?.ReferencedSymbol as TypeSymbol
                ?? targetElementType
                ?? GetBestCommonType(expressions);
            var resultType = elementTypeSymbol != null ? _symbols.GetArray(elementTypeSymbol) : null;

            if (elementTypeSymbol == null)
            {
                diagnostics.Add(BindingDiagnostics.CannotInferElementType().WithLocation(newArrayInit.Location));
            }

            if (elementType == newArrayInit.ElementType
                && expressions == newArrayInit.Expressions
                && elementTypeSymbol == newArrayInit.ElementTypeSymbol
                && resultType == newArrayInit.ResultType
                && diagnostics.Count == 0)
                return newArrayInit;

            return new NewArrayInitExpression(
                elementType,
                expressions,
                newArrayInit.Location,
                elementTypeSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual Expression BindSymbolReference(SymbolReferenceExpression symbolRef, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var referencedSymbol = GetSymbolReference(symbolRef.FullName);

            if (referencedSymbol != null && referencedSymbol == symbolRef.ReferencedSymbol)
                return symbolRef;

            if (referencedSymbol == null)
                diagnostics.Add(BindingDiagnostics.UnknownName(symbolRef.FullName).WithLocation(symbolRef.Location));

            var resultType = GetReferenceResultType(referencedSymbol) ?? _symbols.Object;

            return new SymbolReferenceExpression(
                symbolRef.FullName,
                symbolRef.Location,
                referencedSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    /// <summary>
    /// Gets the symbol from the symbol's full name
    /// </summary>
    protected virtual Symbol? GetSymbolReference(string fullName)
    {
        return _symbols.GetSymbol<Symbol>(fullName);
    }

    /// <summary>
    /// Determines the result type of a referenced <see cref="Symbol"/>.
    /// </summary>
    protected virtual TypeSymbol? GetReferenceResultType(Symbol? referencedSymbol) =>
        referencedSymbol switch
        {
            VariableSymbol v => v.VariableType,
            ParameterSymbol p => p.ParameterType,
            FieldSymbol f => f.FieldType,
            PropertySymbol p => p.PropertyType,
            LambdaSymbol f => f,
            GroupSymbol g => g,
            MethodSymbol => _symbols.Void,
            TypeSymbol => _symbols.Type,
            NamespaceSymbol => _symbols.Namespace,
            _ => null
        };

    protected virtual Expression BindVariable(VariableExpression declaration, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            Expression? variableType = null;
            TypeSymbol? vtype = null;
            Expression? initializer = null;

            context = context.WithInflowType(_symbols.Void);

            if (declaration.VariableType != null)
            {
                variableType = BindExpression(declaration.VariableType, context.WithTargetType(null));
                vtype = variableType?.ReferencedSymbol as TypeSymbol ?? _symbols.Object;
                if (declaration.Initializer != null)
                {
                    initializer = BindExpression(declaration.Initializer, context.WithTargetType(vtype));
                    initializer = ConvertTo(initializer, vtype, context);
                }
            }
            else if (declaration.Initializer != null)
            {
                // target type can carry through declaration
                initializer = BindExpression(declaration.Initializer, context);
                vtype = initializer.ResultType;
            }
            else
            {
                diagnostics.Add(BindingDiagnostics.DeclarationMustHaveTypeOrInitializer().WithLocation(declaration.Location));
                vtype = _symbols.Object;
            }

            if (variableType == declaration.VariableType
                && initializer == declaration.Initializer
                && declaration.Variable != null
                && declaration.Variable.VariableType == vtype
                && declaration.ResultType == vtype)
            {
                return declaration;
            }

            var variable = declaration.Variable != null
                && declaration.Variable.VariableType == vtype
                ? declaration.Variable
                : new VariableSymbol(declaration.Name, vtype ?? _symbols.Any);

            return new VariableExpression(
                declaration.Name,
                variableType,
                initializer,
                declaration.Location,
                variable,
                vtype,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual VoidExpression BindVoid(VoidExpression vex, BindingContext context)
    {
        // nothing to actually bind
        return vex;
    }

    protected virtual TypeSymbol? BindType(Expression typeExpression, BindingContext context)
    {
        var expr = BindExpression(typeExpression, context);
        return expr.ReferencedSymbol as TypeSymbol;
    }

    protected virtual void GetMatchingTypeMembers(TypeSymbol type, string? name, Func<Symbol, bool>? predicate, List<Symbol> members)
    {
        TypeSymbol? symbol = type;
        int initialCount = members.Count;

        while (symbol != null)
        {
            if (name != null)
            {
                symbol.GetMembers(name, predicate, members);
            }
            else if (predicate != null)
            {
                symbol.GetMembers(predicate, members);
            }

            if (members.Count > initialCount)
                break;

            // look in base type
            // todo: handle interfaces separately
            symbol = symbol.BaseTypes.FirstOrDefault();
        }
    }
}