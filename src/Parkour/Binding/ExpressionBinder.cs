namespace Parkour.Binding;
using Semantics;
using Symbols;
using System;
using static System.Net.Mime.MediaTypeNames;

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
        _defaultScope = BindingScope.Default.AddSymbolMembers(externalSymbols);
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
        if (!(context.Rebind || expression.ContainsUnknowns))
            return expression;

        switch (expression)
        {
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

            case ConvertExpression convert:
                return BindConvert(convert, context);

            case DeclarationExpression declaration:
                return BindDeclaration(declaration, context);

            case DefaultExpression dex:
                return BindDefault(dex, context);

            case LabelExpression label:
                return BindLabel(label, context);

            case LambdaExpression lambda:
                return BindLambda(lambda, context);

            case LoopExpression loop:
                return BindLoop(loop, context);

            case OperatorExpression opex:
                return BindOperator(opex, context);

            case PathExpression path:
                return BindPath(path, context);

            case ReferenceExpression reference:
                return BindReference(reference, context);

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

                if (boundExpression is DeclarationExpression decl
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
            var targetSymbol = context.Scope.FindSymbol<LabelSymbol>(s => s.Name == branch.LabelName);

            var expression = branch.Expression != null
                ? BindExpression(branch.Expression, context.WithTargetType(targetSymbol?.Type))
                : null;

            var expressionType = expression != null ? expression.ResultType : _symbols.Void;

            if (targetSymbol == null)
            {
                diagnostics.Add(BindingDiagnostics.NoMatchingTarget(branch.LabelName).WithLocation(branch.Location));
            }
            else if (expression != null && expressionType != targetSymbol.Type)
            {
                expression = ConvertTo(expression, targetSymbol.Type, context);
                expression = BindExpression(expression, context.WithRebind(true));
                //TryGetConversion(expressionType, targetSymbol.Type, ConversionKind.Widening, out _, branch.Location, diagnostics);
            }

            if (expression == branch.Expression
                && targetSymbol == branch.LabelSymbol
                && diagnostics.Count == 0)
                return branch;

            return new BranchExpression(
                branch.LabelName,
                expression,
                branch.Location,
                targetSymbol,
                _symbols.DoesNotReturn,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
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
            var location = expression is PathExpression ep ? ep.Reference.Location : expression.Location;

            if (candidates.Count == 0)
            {
                diagnostics.Add(BindingDiagnostics.NoCallableSymbol().WithLocation(location));
            }
            else
            {
                calledSymbol = GetBestCalledSymbol(instance, arguments, candidates);

                if (calledSymbol == null || calledSymbol == _symbols.Unknown)
                {
                    if (candidates.Count > 1)
                        diagnostics.Add(BindingDiagnostics.CallIsAmbiguous().WithLocation(location));
                }
                else if (!IsCallableSymbol(calledSymbol))
                {
                    diagnostics.Add(BindingDiagnostics.SymbolNotCallable(calledSymbol.Name).WithLocation(location));
                }
                else
                {
                    var parameters = GetCallableSymbolParameters(calledSymbol);
                    if (parameters.Count != arguments.Count)
                    {
                        diagnostics.Add(BindingDiagnostics.CallHasIncorrectNumberOfArguments(calledSymbol.Name).WithLocation(location));
                    }
                    else 
                    {
                        arguments = ConvertArguments(parameters, arguments, context);
                    }
                }
            }

            var resultType = calledSymbol != null ? GetCalledSymbolReturnType(calledSymbol) : null;

            if (expression == call.Expression
                && arguments == call.Arguments
                && call.CalledSymbol == calledSymbol
                && call.ResultType == resultType)
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

            var resultType = (whenTrue.ResultType == whenFalse.ResultType) ? whenTrue.ResultType
                : (whenTrue.ResultType == _symbols.Void) ? _symbols.Void
                : (whenFalse.ResultType == _symbols.Void) ? _symbols.Void
                : null;

            if (resultType == null && whenTrue.ResultType == _symbols.DoesNotReturn)
                resultType = whenFalse.ResultType;

            if (resultType == null && whenFalse.ResultType == _symbols.DoesNotReturn)
                resultType = whenTrue.ResultType;

            if (resultType == null)
                resultType = GetBestCommonType(whenTrue.ResultType, whenFalse.ResultType, context.TargetType);

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

    protected virtual TypeSymbol? GetBestCommonType(params TypeSymbol?[] types) =>
        GetBestCommonType((IReadOnlyList<TypeSymbol?>)types);

    protected virtual TypeSymbol? GetBestCommonType(IReadOnlyList<TypeSymbol?> types, bool voidIsBetter = false)
    {
        TypeSymbol? best = PickBest();

        if (best == _symbols.Void)
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

                if (type != best
                    && IsBetterThanOrSame(type, best))
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
                || (type == SpecialSymbols.Void && voidIsBetter)
                || (best == SpecialSymbols.Void && !voidIsBetter))
            {
                return true;
            }

            // if type cannot be assigned to best
            if (!CanAutoConvert(ConversionKind.Widening, type, best)
                && CanAutoConvert(ConversionKind.Widening, best, type))
            {
                return true;
            }

            return false;
        }

        bool IgnoreType(TypeSymbol type) =>
            type == _symbols.DoesNotReturn
            || type == _symbols.Null
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

    protected virtual Expression BindConvert(ConvertExpression convert, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            context = context.WithInflowType(_symbols.Void);

            var convertedType = BindExpression(convert.ConvertedType, context.WithTargetType(null));
            var type = convertedType.ReferencedSymbol as TypeSymbol;
            var expression = BindExpression(convert.Expression, context.WithTargetType(type));
            TryGetConversion(expression.ResultType, type, convert.Kind, out var conversionSymbol, expression.Location, diagnostics);

            if (convert.Expression == expression
                && convert.ConvertedType == convertedType
                && convert.ConversionSymbol == conversionSymbol
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

    protected virtual Expression BindDeclaration(DeclarationExpression declaration, BindingContext context)
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

            return new DeclarationExpression(
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

    protected virtual TypeSymbol? BindType(Expression typeExpression, BindingContext context)
    {
        var expr = BindExpression(typeExpression, context);
        return expr.ReferencedSymbol as TypeSymbol;
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
                if (!TryGetConversion(context.InflowType, resultType, ConversionKind.Widening, out _, label.Location, null))
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
            FunctionSymbol? lambdaSymbol = lambda.LambdaSymbol;
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
                lambdaSymbol = new FunctionSymbol(
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

    protected virtual Expression BindPath(PathExpression path, BindingContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var members = _memberListPool.AllocateFromPool();

        try
        {
            context = context.WithInflowType(_symbols.Void);

            var expression = BindExpression(path.Expression, context.WithTargetType(null));

            if (expression.ResultType is GroupSymbol)
                diagnostics.Add(BindingDiagnostics.UnknownName(path.Reference.Name).WithLocation(path.Reference.Location));

            var reference = BindPathReference(expression, path.Reference, context);

            if (path.Expression == expression
                && path.Reference == reference)
                return path;

            return new PathExpression(
                expression, 
                reference,
                path.Location,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _memberListPool.ReturnToPool(members);
        }
    }

    protected virtual ReferenceExpression BindPathReference(
        Expression expression, ReferenceExpression reference, BindingContext context)
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

    protected virtual Expression BindReference(ReferenceExpression reference, BindingContext context)
    {
        Symbol? symbol = null;

        context = context.WithInflowType(_symbols.Void);

        if (NamespaceSymbol.IsDottedPath(reference.Name))
        {
            symbol = _symbols.GetSymbol<Symbol>(reference.Name);
        }
        else
        {
            symbol = context.Scope.FindSymbol<Symbol>(s => s.Name == reference.Name);
        }

        return UpdateReference(reference, symbol);
    }

    protected virtual ReferenceExpression UpdateReference(ReferenceExpression reference, Symbol? referencedSymbol)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            if (referencedSymbol != null && referencedSymbol == reference.ReferencedSymbol)
                return reference;

            if (referencedSymbol == null)
                diagnostics.Add(BindingDiagnostics.UnknownName(reference.Name).WithLocation(reference.Location));

            var resultType = GetReferenceResultType(referencedSymbol) ?? _symbols.Object;

            return new ReferenceExpression(
                reference.Name, 
                reference.Location,
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
    /// Determines the result type of a <see cref="ReferenceExpression"/> given the referenced symbol.
    /// </summary>
    protected virtual TypeSymbol? GetReferenceResultType(Symbol? referencedSymbol) =>
        referencedSymbol switch
        {
            VariableSymbol v => v.VariableType,
            ParameterSymbol p => p.ParameterType,
            FieldSymbol f => f.FieldType,
            PropertySymbol p => p.PropertyType,
            FunctionSymbol f => f,
            GroupSymbol g => g,
            MethodSymbol => _symbols.Void,
            TypeSymbol => _symbols.Type,
            NamespaceSymbol => _symbols.Namespace,
            _ => null
        };

    protected virtual VoidExpression BindVoid(VoidExpression vex, BindingContext context)
    {
        return vex;
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
    /// Get the <see cref="ParameterSymbol"/> declarations for the callable symbol
    /// </summary>
    protected virtual ImmutableList<ParameterSymbol> GetCallableSymbolParameters(Symbol callableSymbol) =>
        callableSymbol switch
        {
            FunctionSymbol function => function.Parameters,
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

    protected virtual Expression ConvertTo(Expression expression, TypeSymbol type, BindingContext context)
    {
        // no conversion required?
        if (type == SpecialSymbols.Void
            || type == SpecialSymbols.Any
            || type == SpecialSymbols.Unknown
            || type == _symbols.Object
            || type == expression.ResultType)
            //|| IsAssignableTo(expression.ResultType, type))
            return expression;

        // wrap expression with widening conversion and bind it.
        var convert = SemanticFactory.Convert(
            ConversionKind.Widening,
            expression,
            SemanticFactory.Type(type),
            expression.Location);

        return BindExpression(convert, context);
    }

    protected virtual bool TryGetConversion(
        TypeSymbol sourceType, 
        TypeSymbol? targetType, 
        ConversionKind kind, 
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
            else if (CanAutoConvert(kind, sourceType, targetType))
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

    /// <summary>
    /// Determines if the conversion is allowed through automatic means, numeric widening/narrowing or object up/down casting.
    /// </summary>
    protected virtual bool CanAutoConvert(ConversionKind conversion, TypeSymbol source, TypeSymbol target)
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

        return source == target
            || CanWiden(source, target);
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
            FunctionSymbol function =>
                function.ReturnType == target
                && function.Parameters.Count == 1
                && CanAutoConvert(ConversionKind.Widening, source, function.Parameters[0].ParameterType),
            MethodSymbol method =>
                method.IsStatic
                && method.ReturnType == target
                && method.Parameters.Count == 1
                && CanAutoConvert(ConversionKind.Widening, source, method.Parameters[0].ParameterType),
            _ => false
        };

    protected virtual Symbol? GetBestConversionOperator(TypeSymbol sourceType, TypeSymbol targetType, IReadOnlyList<Symbol> candidates)
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