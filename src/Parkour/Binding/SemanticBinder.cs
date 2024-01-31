using System.Diagnostics.CodeAnalysis;

namespace Parkour.Binding;

using Semantics;
using Symbols;

public class SemanticBinder
{
    public SemanticBinder()
    {
    }

    #region Declarations
    /// <summary>
    /// Bind a set of declarations given external symbols.
    /// </summary>
    public DeclarationBinding BindDeclarations(
        IEnumerable<Declaration> declarations,
        NamespaceSymbol externalSymbols)
    {
        NamespaceSymbol? declarationSymbols = null;
        SymbolContext? context = null;

        // create combined global namespace and construct declaration symbols
        var combinedSymbols = CombinedSymbols.CreateCombinedGlobalNamespace(
            globals =>
            {
                declarationSymbols = new NamespaceSymbol("", null,
                me =>
                {
                    var globalNamespaces = declarations.Where(d =>
                        d is NamespaceDeclaration nd && nd.Name == "")
                        .ToList();

                    var globalNamespaceMembers = declarations.SelectMany(d =>
                        d is NamespaceDeclaration nd && nd.Name == ""
                            ? (IEnumerable<Declaration>)nd.Declarations
                            : new[] { d })
                        .ToImmutableList();

                    return CombineMembers(me, globalNamespaceMembers, context!);
                });

                return [externalSymbols, declarationSymbols];
            });

        var cache = SymbolCache.From(combinedSymbols);
        var scope = CreateBindingScope().AddMembers(combinedSymbols);
        context = new SymbolContext(cache, scope);

        // force evaluation of declarationNamespace
        var globalMembers = combinedSymbols.Members;
        var declarationNamespaceMembers = declarationSymbols!.Members;

        // resolve all declared symbols which creates symbol<->declaration maps
        declarationSymbols.WalkDeclarations(s => { });

        return new DeferredBinding(
            this,
            context.DeclarationContext,
            externalSymbols,
            declarationSymbols,
            declarations.ToImmutableList());
    }

    /// <summary>
    /// returns the symbols for the declarations, 
    /// with same named namespace declarations combined into a single namespace symbol.
    /// </summary>
    private ImmutableList<Symbol> CombineMembers(
        NamespaceSymbol container,
        IEnumerable<Declaration> members,
        SymbolContext context)
    {
        var newMembers = new List<Symbol>();

        var namespaceMemberGroups = members
            .OfType<NamespaceDeclaration>()
            .GroupBy(d => d.Name)
            .ToList();

        var newMemberNamespaces = namespaceMemberGroups
            .Select(g => new NamespaceSymbol(
                g.Key,
                container,
                me =>
                {
                    context.Map(g, me);
                    var newContext = context.WithScope(context.Scope.AddMembers(me).AddSymbol(me));
                    return CombineMembers(me, g.SelectMany(n => n.Declarations), newContext);
                }))
            .ToList();

        newMembers.AddRange(newMemberNamespaces);

        var otherMembers = members
            .Where(d => !(d is NamespaceDeclaration))
            .ToList();

        var otherMemberSymbols =
            otherMembers
            .Select(d => CreateDeclarationSymbol(null, d, context))
            .OfType<Symbol>()
            .ToList();

        newMembers.AddRange(otherMemberSymbols);

        return newMembers.ToImmutableList();
    }

    /// <summary>
    /// Creates the symbol associated with the declaration.
    /// </summary>
    protected virtual Symbol? CreateDeclarationSymbol(
        Symbol? declaringSymbol,
        Declaration declaration,
        SymbolContext context)
    {
        Symbol? symbol = null;

        switch (declaration)
        {
            case TypeParameterDeclaration tp:
                symbol = new TypeParameterSymbol(tp.Name);
                break;

            case ClassDeclaration cd:
                var classContext = context;
                var classSymbol = new TypeSymbol(
                    cd.Name,
                    declaringSymbol,
                    cd.Access,
                    cd.Modifiers,
                    me => cd.TypeParameters.Select(tp => (TypeParameterSymbol)CreateDeclarationSymbol(me, tp, context)!).ToImmutableList()!,
                    () => ImmutableList<TypeSymbol>.Empty,
                    () => cd.BaseTypes.Select(bt => GetType(bt, classContext) ?? SpecialSymbols.Unknown).ToImmutableList()!,
                    me => cd.Declarations.Select(d => CreateDeclarationSymbol(me, d, classContext)).Where(s => s != null).ToImmutableList()!,
                    constructedFrom: null);

                classContext = classContext.WithScope(
                    classContext.Scope
                        .AddMembers(classSymbol)
                        .AddSymbol(classSymbol)
                        .AddSymbols(classSymbol.TypeParameters));

                symbol = classSymbol;
                break;

            case MethodDeclaration md:
                symbol = new MethodSymbol(
                    md.Name,
                    declaringSymbol,
                    md.Access,
                    md.Modifiers,
                    me => ImmutableList<TypeParameterSymbol>.Empty,
                    () => ImmutableList<TypeSymbol>.Empty,
                    me => md.Parameters.Select(p => (ParameterSymbol)CreateDeclarationSymbol(me, p, context)!).ToImmutableList()!,
                    () => GetType(md.ReturnType, context) ?? SpecialSymbols.Unknown,
                    constructedFrom: null
                    );
                break;

            case ParameterDeclaration pd:
                symbol = new ParameterSymbol(
                    pd.Name,
                    declaringSymbol,
                    () => pd.ParameterType != null 
                        ? GetType(pd.ParameterType, context) ?? SpecialSymbols.Unknown 
                        : SpecialSymbols.Any
                    );
                break;

            case FieldDeclaration fd:
                symbol = new FieldSymbol(
                    fd.Name,
                    declaringSymbol as TypeSymbol,
                    fd.Access,
                    fd.Modifiers,
                    () => GetType(fd.FieldType, context) ?? SpecialSymbols.Unknown
                    );
                break;

            case PropertyDeclaration pd:
                symbol = new PropertySymbol(
                    pd.Name,
                    declaringSymbol as TypeSymbol,
                    pd.Access,
                    pd.Modifiers,
                    () => GetType(pd.PropertyType, context) ?? SpecialSymbols.Unknown,
                    pd.BackingField != null
                        ? me => (FieldSymbol)CreateDeclarationSymbol(me, pd.BackingField, context)!
                        : null,
                    me => (MethodSymbol)CreateDeclarationSymbol(me, pd.GetMethod, context)!,
                    pd.SetMethod != null
                        ? me => (MethodSymbol)CreateDeclarationSymbol(me, pd.SetMethod, context)!
                        : null
                );
                break;
        }

        if (symbol != null)
        {
            context.Map(declaration, symbol);
        }

        return symbol;
    }

    /// <summary>
    /// Binds a declaration to its associated symbol
    /// </summary>
    protected virtual Declaration BindDeclaration(
        Declaration declaration,
        DeclarationContext context)
    {
        switch (declaration)
        {
            case FieldDeclaration fd:
                return BindField(fd, context);
            case PropertyDeclaration pd:
                return BindProperty(pd, context);
            case ParameterDeclaration prd:
                return BindParameter(prd, context);
            case MethodDeclaration md:
                return BindMethod(md, context);
            case ClassDeclaration cld:
                return BindClass(cld, context);
            case NamespaceDeclaration nd:
                return BindNamespace(nd, context);
            case TypeParameterDeclaration tp:
                return BindTypeParameter(tp, context);
            case UsingDeclaration ud:
                return BindUsing(ud, context);
            default:
                throw new InvalidCastException($"Unhandled declaration '{declaration.GetType().Name}' in {nameof(SemanticBinder)}.{nameof(BindDeclaration)}");
        }
    }

    protected virtual ImmutableList<TDeclaration> BindDeclarations<TDeclaration>(
        ImmutableList<TDeclaration> list, 
        DeclarationContext context)
        where TDeclaration : Declaration
    {
        return list.Rewrite(d => (TDeclaration)BindDeclaration(d, context));
    }

    protected virtual FieldDeclaration BindField(
        FieldDeclaration fd, 
        DeclarationContext context)
    {
        var fieldSymbol = context.TryGetSymbol(fd, out var symbol) ? symbol as FieldSymbol : null;
        var fieldType = BindExpression(fd.FieldType, context.ExpressionContext);
        var initializer = fd.Initializer != null
            ? BindExpression(fd.Initializer, context.ExpressionContext)
            : null;
        return new FieldDeclaration(
            fd.Name,
            fd.Access,
            fd.Modifiers,
            fieldType,
            initializer,
            fd.Location,
            fieldSymbol,
            fd.Diagnostics
            );
    }

    protected virtual PropertyDeclaration BindProperty(
        PropertyDeclaration pd, 
        DeclarationContext context)
    {
        var propertySymbol = context.TryGetSymbol(pd, out var symbol) ? symbol as PropertySymbol : null;

        var propertyType = BindExpression(pd.PropertyType, context.ExpressionContext);

        var backingField = pd.BackingField != null
            ? (FieldDeclaration)BindDeclaration(pd.BackingField, context)
            : null;

        var methodContext = context;

        if (propertySymbol?.BackingField != null)
        {
            methodContext = methodContext.WithScope(
                methodContext.Scope.AddSymbol(propertySymbol.BackingField));
        }

        var getMethod = (MethodDeclaration)BindDeclaration(pd.GetMethod, methodContext);

        var setMethod = pd.SetMethod != null
            ? (MethodDeclaration)BindDeclaration(pd.SetMethod, methodContext)
            : null;

        return new PropertyDeclaration(
            pd.Name,
            pd.Access,
            pd.Modifiers,
            propertyType,
            backingField,
            getMethod,
            setMethod,
            pd.Location,
            propertySymbol,
            pd.Diagnostics
            );
    }

    protected virtual ParameterDeclaration BindParameter(
        ParameterDeclaration pd,
        DeclarationContext context)
    {
        var parameterSymbol = context.TryGetSymbol(pd, out var symbol) ? symbol as ParameterSymbol : null;

        var parameterType = pd.ParameterType != null
            ? BindExpression(pd.ParameterType, context.ExpressionContext)
            : null;

        return new ParameterDeclaration(
            pd.Name,
            parameterType,
            pd.Location,
            parameterSymbol,
            pd.Diagnostics
            );
    }

    protected virtual MethodDeclaration BindMethod(
        MethodDeclaration md,
        DeclarationContext context)
    {
        var methodSymbol = context.TryGetSymbol(md, out var symbol) ? symbol as MethodSymbol : null;

        var typeParameters = BindDeclarations(md.TypeParameters, context);
        var parameters = BindDeclarations(md.Parameters, context);
        var returnType = BindExpression(md.ReturnType, context.ExpressionContext);

        var bodyContext = context.ExpressionContext;

        // add parameters to scope for body
        if (methodSymbol != null
            && methodSymbol.Parameters.Count > 0)
        {
            bodyContext = bodyContext.WithScope(bodyContext.Scope.AddSymbols(methodSymbol.Parameters));
        }

        var body = BindExpression(md.Body, bodyContext);

        return new MethodDeclaration(
            md.Name,
            md.Access,
            md.Modifiers,
            typeParameters,
            parameters,
            body,
            returnType,
            md.Location,
            methodSymbol,
            md.Diagnostics
            );
    }

    protected virtual ClassDeclaration BindClass(
        ClassDeclaration cd,
        DeclarationContext context)
    {
        var classSymbol = context.TryGetSymbol(cd, out var symbol) ? symbol as TypeSymbol : null;

        var typeParameters = BindDeclarations(cd.TypeParameters, context);

        // put class symbol in scope for baseTypes and members
        var classContext = classSymbol != null
            ? context.WithScope(context.Scope.AddMembers(classSymbol).AddSymbol(classSymbol))
            : context;

        var baseTypes = BindExpressionList(cd.BaseTypes, classContext.ExpressionContext);

        // add all class members to scope
        var bodyContext = classContext;
        if (classSymbol != null)
        {
            bodyContext = bodyContext.WithScope(bodyContext.Scope.AddMembers(classSymbol).AddSymbol(classSymbol));
        }

        var declarations = BindDeclarations(cd.Declarations, bodyContext);

        if (typeParameters == cd.TypeParameters
            && baseTypes == cd.BaseTypes
            && declarations == cd.Declarations)
            return cd;

        return new ClassDeclaration(
            cd.Name,
            cd.Access,
            cd.Modifiers,
            typeParameters,
            baseTypes,
            declarations,
            cd.Location,
            classSymbol,
            cd.Diagnostics
            );
    }

    protected virtual TypeParameterDeclaration BindTypeParameter(
        TypeParameterDeclaration tp,
        DeclarationContext context)
    {
        var tpSymbol = context.TryGetSymbol(tp, out var symbol) ? symbol as TypeParameterSymbol : null;

        if (tp.TypeParameterSymbol == tpSymbol)
            return tp;

        return new TypeParameterDeclaration(
            tp.Name,
            tp.Location,
            tpSymbol,
            tp.Diagnostics);
    }

    protected virtual NamespaceDeclaration BindNamespace(
        NamespaceDeclaration nd,
        DeclarationContext context)
    {
        var nsSymbol = context.TryGetSymbol(nd, out var symbol) ? symbol as NamespaceSymbol : null;

        var bodyContext = context;
        if (nsSymbol != null)
        {
            bodyContext = bodyContext.WithScope(bodyContext.Scope.AddMembers(nsSymbol).AddSymbol(nsSymbol));
        }

        var (declarations, finalContext) = nd.Declarations.Rewrite(bodyContext, (d, _context) =>
        {
            var nd = BindDeclaration(d, _context);

            // handle using declarations
            if (nd is UsingDeclaration ud
                && ud.Expression.ReferencedSymbol != null)
            {
                if (ud.AliasedSymbol != null)
                {
                    _context = _context.WithScope(_context.Scope.AddSymbol(ud.AliasedSymbol));
                }
                else if (ud.Expression.ReferencedSymbol is NamespaceSymbol ns)
                {
                    _context = _context.WithScope(_context.Scope.AddSymbol(ns).AddMembers(ns));
                }
            }

            return (nd, _context);
        });

        return new NamespaceDeclaration(
            nd.Name,
            declarations,
            nd.Location,
            nsSymbol,
            nd.Diagnostics
            );
    }

    protected virtual UsingDeclaration BindUsing(
        UsingDeclaration ud, 
        DeclarationContext context)
    {
        var expression = BindExpression(ud.Expression, context.ExpressionContext);

        if (expression == ud.Expression)
            return ud;

        var aliasedSymbol = expression.ReferencedSymbol as ContainerSymbol;

        var aliasSymbol = ud.Name.Length > 0 && aliasedSymbol != null
            ? new AliasSymbol(ud.Name, aliasedSymbol)
            : null;

        return new UsingDeclaration(
            ud.Name,
            expression,
            ud.Location,
            aliasSymbol,
            null);
    }
    #endregion

    #region Expressions
    /// <summary>
    /// Binds all unbound expressions.
    /// </summary>
    public Expression BindExpression(
        Expression expression, 
        NamespaceSymbol globalNamespace)
    {
        var scope = CreateBindingScope().AddSymbol(globalNamespace);
        return BindExpression(expression, globalNamespace, scope);
    }

    /// <summary>
    /// Binds all unbound expressions.
    /// </summary>
    public Expression BindExpression(
        Expression expression, 
        NamespaceSymbol globalNamespace, 
        BindingScope scope)
    {
        var context = new ExpressionContext(globalNamespace, scope);
        return BindExpression(expression, context);
    }

    /// <summary>
    /// Binds all the expressions in a list.
    /// </summary>
    public ImmutableList<TExpression> BindExpressionList<TExpression>(
        ImmutableList<TExpression> expressions, 
        NamespaceSymbol globalNamespace)
        where TExpression : Expression
    {
        var scope = CreateBindingScope().AddSymbol(globalNamespace);
        return BindExpressionList(expressions, globalNamespace, scope);
    }

    /// <summary>
    /// Binds all the expressions in a list.
    /// </summary>
    public ImmutableList<TExpression> BindExpressionList<TExpression>(
        ImmutableList<TExpression> expressions, 
        NamespaceSymbol globalNamespace, 
        BindingScope scope)
        where TExpression : Expression
    {
        var context = new ExpressionContext(globalNamespace, scope);
        return BindExpressionList(expressions, context);
    }

    /// <summary>
    /// Gets the type of an type expression
    /// </summary>
    protected virtual TypeSymbol? GetType(Expression typeExpression, ExpressionContext context)
    {
        if (typeExpression.IsUnbound)
            typeExpression = BindExpression(typeExpression, context);
        return typeExpression.ReferencedSymbol as TypeSymbol;
    }

    protected TypeSymbol? GetType(Expression typeExpression, DeclarationContext context) =>
        GetType(typeExpression, context.ExpressionContext);

    protected TypeSymbol? GetType(Expression typeExpression, SymbolContext context) =>
        GetType(typeExpression, context.DeclarationContext);

    /// <summary>
    /// Gets a binding scope without any state.
    /// </summary>
    public virtual BindingScope CreateBindingScope() =>
        SimpleBindingScope.Empty;

    /// <summary>
    /// Binds all unbound expressions.
    /// </summary>
    protected virtual Expression BindExpression(Expression expression, ExpressionContext context)
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
                throw new InvalidOperationException($"Unhandled semantic '{expression.GetType().Name}' in {nameof(SemanticBinder)}.BindExpression");
        }
    }

    protected virtual ImmutableList<TExpression> BindExpressionList<TExpression>(
        ImmutableList<TExpression> expressions, ExpressionContext context)
        where TExpression : Expression
    {
        if (expressions.Count == 0)
            return expressions;
        return expressions.Rewrite(e => (TExpression)BindExpression(e, context));
    }

    protected virtual Expression BindArray(ArrayExpression array, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(array.Expression, context);
            var elementType = expression.ReferencedSymbol as TypeSymbol;
            var referencedSymbol = elementType != null ? context.Symbols.GetArray(elementType) : null;
            var resultType = GetReferenceResultType(referencedSymbol, context);

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

    protected virtual Expression BindArity(ArityExpression arity, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(arity.Expression, context);

            Symbol? referencedSymbol = null;
            if (expression.ReferencedSymbol is GroupSymbol group)
            {
                referencedSymbol = context.Symbols.GetGroup(group.Symbols.Where(s => s.Arity == arity.Arity));
            }
            else if (expression.ReferencedSymbol is Symbol symbol)
            {
                referencedSymbol = symbol.Arity == arity.Arity ? symbol : null;
            }

            if (referencedSymbol == null)
            {
                diagnostics.Add(BindingDiagnostics.NoReferencedSymbolsHaveMatchingArity().WithLocation(arity.Location));
            }

            var resultType = GetReferenceResultType(referencedSymbol, context);

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

    protected virtual Expression BindAssign(AssignExpression assign, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var target = BindExpression(assign.Target, context.WithTargetType(null));
            var assignType = GetReferenceResultType(target.ReferencedSymbol, context) ?? context.Symbols.Object;
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

    protected virtual Expression BindBlock(BlockExpression block, ExpressionContext context)
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
                    var type = label.ReceivingType != null 
                        ? GetType(label.ReceivingType, labelContext) 
                        : context.Symbols.Void;
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


    protected virtual Expression BindBranch(BranchExpression branch, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var labelSymbol = GetBranchLabel(branch.LabelName, context);

            var expression = branch.Expression != null
                ? BindExpression(branch.Expression, context.WithTargetType(labelSymbol?.Type))
                : null;

            var expressionType = expression != null 
                ? expression.ResultType 
                : SpecialSymbols.Void;

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
                SpecialSymbols.DoesNotReturn,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    protected virtual LabelSymbol? GetBranchLabel(string name, ExpressionContext context)
    {
        return context.Scope.FindMatchingSymbol<LabelSymbol>(name, null);
    }

    protected virtual Expression BindCall(CallExpression call, ExpressionContext context)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithTargetType(null).WithInflowType(SpecialSymbols.Void);

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
        return parameter.ParameterType == SpecialSymbols.Any
            || argument.ResultType == SpecialSymbols.Unknown
            || parameter.ParameterType == argument.ResultType;
    }

    protected virtual ImmutableList<Expression> ConvertArguments(
        ImmutableList<ParameterSymbol> parameters,
        ImmutableList<Expression> arguments,
        ExpressionContext context)
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

    protected virtual Expression BindCondition(ConditionExpression condition, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithInflowType(SpecialSymbols.Void);

            var test = BindExpression(condition.Test, context.WithTargetType(context.Symbols.Boolean));
            test = ConvertTo(test, context.Symbols.Boolean, context);

            var whenTrue = BindExpression(condition.WhenTrue, context);
            var whenFalse = BindExpression(condition.WhenFalse, context);

            var resultType = GetBestCommonType(context, [whenTrue.ResultType, whenFalse.ResultType, context.TargetType], voidIsBetter: false);
            if (resultType == null)
            {
                diagnostics.Add(BindingDiagnostics.NoCommonTypeFound().WithLocation(condition.Location));
                resultType = context.Symbols.Object;
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

    protected TypeSymbol? GetBestCommonType(ExpressionContext context, IReadOnlyList<Expression> expressions, bool voidIsBetter = false)
    {
        var types = _typeListPool.AllocateFromPool();
        try
        {
            types.AddRange(expressions.Select(e => e.ResultType));
            return GetBestCommonType(context, types, voidIsBetter);
        }
        finally
        {
            _typeListPool.ReturnToPool(types);
        }
    }

    protected TypeSymbol? GetBestCommonType(ExpressionContext context, params TypeSymbol?[] types) =>
        GetBestCommonType(context, (IReadOnlyList<TypeSymbol?>)types);

    protected virtual TypeSymbol? GetBestCommonType(
        ExpressionContext context,
        IReadOnlyList<TypeSymbol?> types, 
        bool voidIsBetter = false)
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
            if (!HasConversion(ConversionKind.Widening, type, best, context)
                && HasConversion(ConversionKind.Widening, best, type, context))
            {
                return true;
            }

            return false;
        }

        bool IsVoidLike(TypeSymbol type) =>
            type == SpecialSymbols.Void
            || type == SpecialSymbols.DoesNotReturn;

        bool IgnoreType(TypeSymbol type) =>
            type == SpecialSymbols.Null
            || type == SpecialSymbols.Unknown;
    }

    protected virtual Expression BindConstant(ConstantExpression constant, ExpressionContext context)
    {
        var resultType = constant.Value == null
            ? SpecialSymbols.Null
            : context.Symbols.GetType(constant.Value.GetType());

        if (resultType == constant.ResultType)
            return constant;

        return new ConstantExpression(
            constant.Value,
            constant.Location,
            resultType,
            null);
    }

    protected virtual Expression BindConstruct(TypeArgumentsExpression construct, ExpressionContext context)
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
                    .Select(ta => GetType(ta, context))
                    .OfType<TypeSymbol>()
                    .ToImmutableList();

                constructedSymbol = ConstructSymbol(symbol, typeArgs, context, construct.Location, diagnostics);
            }

            var resultType = GetReferenceResultType(constructedSymbol, context);

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
        ExpressionContext context,
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
                .Select(s => ConstructSymbol(s, typeArguments, context))
                .OfType<Symbol>()
                .ToImmutableList();

            return context.Symbols.GetGroup(constructedSymbols);
        }
        else if (symbol is TypeSymbol type)
        {
            if (type.Arity != typeArguments.Count)
            {
                if (diagnostics != null)
                    diagnostics.Add(BindingDiagnostics.TypeDoesNotHaveMatchingArity().WithLocation(location));
                return null;
            }

            return context.Symbols.GetConstructed(type, typeArguments);
        }
        else if (symbol is MethodSymbol method)
        {
            if (method.Arity != typeArguments.Count)
            {
                if (diagnostics != null)
                    diagnostics.Add(BindingDiagnostics.MethodDoesNotHaveMatchingArity().WithLocation(location));
                return null;
            }
            return context.Symbols.GetConstructed(method, typeArguments);
        }
        else
        {
            if (diagnostics != null)
                diagnostics.Add(BindingDiagnostics.NoTypeOrMethodWithMatchingArityToConstruct().WithLocation(location));
        }

        return null;
    }

    #region Conversion
    protected virtual Expression BindConvert(ConvertExpression convert, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            context = context.WithInflowType(SpecialSymbols.Void);

            var convertedType = convert.ConvertedType != null
                ? BindExpression(convert.ConvertedType, context.WithTargetType(null))
                : null;

            var type = convertedType != null
                ? convertedType.ReferencedSymbol as TypeSymbol
                : convert.ResultType;

            var expression = BindExpression(convert.Expression, context.WithTargetType(type));

            if (convert.ConvertedType == null
                && convert.ResultType != null
                && IsAssignableTo(expression.ResultType, convert.ResultType, context))
            {
                // remove unnecessary non-explicit conversion
                return expression;
            }

            TryGetConversion(convert.Kind, expression.ResultType, type, context, out var conversionSymbol, expression.Location, diagnostics);

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

    protected virtual Expression ConvertTo(Expression expression, TypeSymbol type, ExpressionContext context)
    {
        // ignore void
        if (type == SpecialSymbols.Void
            || expression.ResultType == SpecialSymbols.Void)
            return expression;

        // remove unnecessary conversions added by this method on prior bindings
        while (expression is ConvertExpression ce
            && ce.ConvertedType == null // added by binding
            && IsAssignableTo(ce.Expression.ResultType, type, context))
        {
            expression = ce.Expression;
        }

        if (IsAssignableTo(expression.ResultType, type, context))
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
        ExpressionContext context,
        out Symbol? conversionSymbol,
        ISourceLocation? location,
        List<Diagnostic>? diagnostics)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            if (targetType == null)
            {
                diagnostics?.Add(BindingDiagnostics.CannotConvert(sourceType, SpecialSymbols.Unknown).WithLocation(location));
                conversionSymbol = null;
                return false;
            }
            else if (HasIntrinsicConversion(kind, sourceType, targetType, context))
            {
                conversionSymbol = null;
                return true;
            }
            else
            {
                GetConversionOperatorCandidates(kind, sourceType, targetType, context, candidates);
                conversionSymbol = GetBestConversionOperator(sourceType, targetType, context, candidates);

                if (conversionSymbol == null)
                {
                    diagnostics?.Add(BindingDiagnostics.CannotConvert(sourceType, targetType ?? SpecialSymbols.Unknown).WithLocation(location));
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

    protected virtual bool HasConversion(
        ConversionKind kind, 
        TypeSymbol sourceType, 
        TypeSymbol targetType,
        ExpressionContext context)
    {
        return TryGetConversion(kind, sourceType, targetType, context, out _, null, null);
    }

    /// <summary>
    /// Determines if the conversion can be done through intrinsic means (not custom conversion).
    /// </summary>
    protected virtual bool HasIntrinsicConversion(
        ConversionKind kind, 
        TypeSymbol sourceType, 
        TypeSymbol targetType,
        ExpressionContext context)
    {
        if (IsAssignableTo(sourceType, targetType, context))
            return true;

        if (CanDownCast(sourceType, targetType, context))
            return true;

        if (kind == ConversionKind.Narrowing)
        {
            return IsAssignableTo(targetType, sourceType, context)
                || CanUpCast(sourceType, targetType, context);
        }

        return sourceType == targetType
            || CanWiden(sourceType, targetType, context);
    }

    protected virtual bool CanDownCast(TypeSymbol source, TypeSymbol target, ExpressionContext context)
    {
        if (source == target)
            return true;

        foreach (var sbt in source.BaseTypes)
        {
            if (CanDownCast(sbt, target, context))
                return true;
        }

        return false;
    }

    protected virtual bool CanUpCast(TypeSymbol source, TypeSymbol target, ExpressionContext context)
    {
        if (target == source)
            return true;

        foreach (var tbt in target.BaseTypes)
        {
            if (CanUpCast(source, tbt, context))
                return true;
        }

        return false;
    }

    protected virtual bool CanWiden(TypeSymbol source, TypeSymbol target, ExpressionContext context)
    {
        if (target == context.Symbols.Int64)
        {
            return source == context.Symbols.Int32
                || source == context.Symbols.Int16
                || source == context.Symbols.Byte;
        }
        else if (target == context.Symbols.Int32)
        {
            return source == context.Symbols.Int16
                || source == context.Symbols.Byte;
        }
        else if (target == context.Symbols.Int16)
        {
            return source == context.Symbols.Byte;
        }
        else if (target == context.Symbols.Double)
        {
            return source == context.Symbols.Single
                || CanWiden(source, context.Symbols.Int64, context);
        }
        else if (target == context.Symbols.Single)
        {
            return CanWiden(source, context.Symbols.Int64, context);
        }
        else if (target == context.Symbols.Decimal)
        {
            return CanWiden(source, context.Symbols.Double, context);
        }
        return false;
    }

    /// <summary>
    /// The source type is assignable to the target type without conversion.
    /// </summary>
    protected virtual bool IsAssignableTo(TypeSymbol sourceType, TypeSymbol targetType, ExpressionContext context)
    {
        if (sourceType == targetType)
            return true;

        if (sourceType == SpecialSymbols.DoesNotReturn)
            return true;

        if (targetType == SpecialSymbols.Any)
            return true;

        if (sourceType.RuntimeType != null
            && targetType.RuntimeType != null
            && sourceType.RuntimeType.IsAssignableTo(targetType.RuntimeType))
            return true;

        if (CanDownCast(sourceType, targetType, context))
            return true;

        return false;
    }

    protected virtual void GetConversionOperatorCandidates(
        ConversionKind kind, 
        TypeSymbol source, 
        TypeSymbol target, 
        ExpressionContext context,
        List<Symbol> operators)
    {
        GetMatchingTypeMembers(source, "op_Implicit", s => IsMatchingConversionOperator(s, kind, source, target, context), operators);
        GetMatchingTypeMembers(target, "op_Implicit", s => IsMatchingConversionOperator(s, kind, source, target, context), operators);

        if (kind == ConversionKind.Narrowing)
        {
            GetMatchingTypeMembers(source, "op_Explicit", s => IsMatchingConversionOperator(s, kind, source, target, context), operators);
            GetMatchingTypeMembers(target, "op_Explicit", s => IsMatchingConversionOperator(s, kind, source, target, context), operators);
        }
    }

    protected virtual bool IsMatchingConversionOperator(
        Symbol symbol,
        ConversionKind kind,
        TypeSymbol source,
        TypeSymbol target,
        ExpressionContext context)
    {
        return symbol switch
        {
            LambdaSymbol function =>
                function.ReturnType == target
                && function.Parameters.Count == 1
                && HasConversion(ConversionKind.Widening, source, function.Parameters[0].ParameterType, context),
            MethodSymbol method =>
                method.IsStatic
                && method.ReturnType == target
                && method.Parameters.Count == 1
                && HasConversion(ConversionKind.Widening, source, method.Parameters[0].ParameterType, context),
            _ => false
        };
    }

    protected virtual Symbol? GetBestConversionOperator(
        TypeSymbol sourceType, 
        TypeSymbol targetType, 
        ExpressionContext context,
        IReadOnlyList<Symbol> candidates)
    {
        // todo: do better
        return candidates.FirstOrDefault();
    }
    #endregion

    protected virtual Expression BindDefault(DefaultExpression dex, ExpressionContext context)
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
                resultType = SpecialSymbols.Any;
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

    protected virtual Expression BindLabel(LabelExpression label, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var receivingType = label.ReceivingType != null ? BindExpression(label.ReceivingType, context) : null;
            var resultType = receivingType != null ? receivingType.ReferencedSymbol as TypeSymbol : SpecialSymbols.Void;
            var targetSymbol = GetAssociatedLabelSymbol(label) ?? new LabelSymbol(label.Name, resultType);

            // check for inflow types into labels
            if (resultType != SpecialSymbols.Void)
            {
                // if label is expecting to receive a value, then inflow type must match 
                if (!TryGetConversion(ConversionKind.Widening, context.InflowType, resultType, context, out _, label.Location, null))
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

    protected virtual Expression BindLambda(LambdaExpression lambda, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var types = _typeListPool.AllocateFromPool();
        try
        {
            LambdaSymbol? lambdaSymbol = lambda.LambdaSymbol;
            LabelSymbol? returnTarget = lambda.ReturnTarget
                ?? new LabelSymbol(LabelSymbol.ReturnLabelName, SpecialSymbols.Any);
            ImmutableList<ParameterDeclaration> parameters = lambda.Parameters;
            Expression body = lambda.Body;
            TypeSymbol? returnType = null;

            context = context.WithInflowType(SpecialSymbols.Void);

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

            void BindLambdaSymbol(ExpressionContext context)
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
                ExpressionContext context)
            {
                if (parameters.Count == 0)
                    return ImmutableList<ParameterSymbol>.Empty;

                var symbols = new List<ParameterSymbol>();
                var declarations = new List<ParameterDeclaration>();

                context = context.WithTargetType(null);

                foreach (var p in parameters)
                {
                    var type = p.ParameterType != null ? BindExpression(p.ParameterType, context) : null;
                    var ptype = type?.ReferencedSymbol as TypeSymbol ?? SpecialSymbols.Any;
                    var psymbol = new ParameterSymbol(p.Name, declaringSymbol, ptype);
                    var pdecl = new ParameterDeclaration(p.Name, type, p.Location, psymbol, null);
                    symbols.Add(psymbol);
                    declarations.Add(pdecl);
                }

                parameters = declarations.ToImmutableList();
                return symbols.ToImmutableList();
            }

            void BindBodyAndReturnType(ImmutableList<ParameterSymbol> parameters, ExpressionContext context)
            {
                var bodyContext = context.WithScope(
                    context.Scope
                        .AddSymbols(parameters)
                        .AddSymbol(returnTarget));
                body = BindExpression(lambda.Body, bodyContext);
                returnType = GetLambdaResultType(body, returnTarget, context, diagnostics);
            }
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _typeListPool.ReturnToPool(types);
        }
    }



    protected virtual TypeSymbol GetLambdaResultType(
        Expression body, 
        LabelSymbol returnTarget, 
        ExpressionContext context,
        List<Diagnostic> diagnostics)
    {
        var types = _typeListPool.AllocateFromPool();
        try
        {
            GetBranchExpressionTypes(body, returnTarget, types);
            types.Add(body.ResultType);

            var best = GetBestCommonType(context, types, voidIsBetter: false) ?? context.Symbols.Object;
            return best;
        }
        finally
        {
            _typeListPool.ReturnToPool(types);
        }
    }

    protected virtual Expression BindLoop(LoopExpression loop, ExpressionContext context)
    {
        var diagostics = _diagnosticListPool.AllocateFromPool();
        var types = _typeListPool.AllocateFromPool();
        try
        {
            var breakTarget = loop.BreakTarget ?? new LabelSymbol(LabelSymbol.BreakLabelName, SpecialSymbols.Any);
            var continueTarget = loop.ContinueTarget ?? new LabelSymbol(LabelSymbol.ContinueLabelName, SpecialSymbols.Void);

            var bodyContext = context.WithScope(
                context.Scope.AddSymbols(new[] { breakTarget, continueTarget }));

            context = context.WithInflowType(SpecialSymbols.Void);
            var body = BindExpression(loop.Body, bodyContext);

            // result type is the common type of all the break branches.
            GetBranchExpressionTypes(body, breakTarget, types);
            var resultType = GetBestCommonType(context, types, voidIsBetter: false) ?? SpecialSymbols.Void;

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

    protected virtual Expression BindOperator(OperatorExpression opex, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var ops = GetOperators(opex.Kind, context);
            var referencedSymbol = context.Symbols.GetGroup(ops);

            if (referencedSymbol != null && referencedSymbol == opex.ReferencedSymbol)
                return opex;

            if (referencedSymbol == null)
                diagnostics.Add(BindingDiagnostics.UnknownOperator(opex.Kind).WithLocation(opex.Location));

            var resultType = GetReferenceResultType(referencedSymbol, context) ?? context.Symbols.Object;

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

    protected virtual ImmutableList<OperatorSymbol> GetOperators(string operatorKind, ExpressionContext context)
    {
        var operators = Operators.From(context.Symbols);
        return operators.GetOperators(operatorKind);
    }

    protected virtual Expression BindMember(MemberExpression member, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithInflowType(SpecialSymbols.Void);

            var expression = BindExpression(member.Expression, context.WithTargetType(null));

            if (expression.ResultType is GroupSymbol)
                diagnostics.Add(BindingDiagnostics.UnknownName(member.Name).WithLocation(member.Location));

            var referencedSymbol = GetReferencedMember(expression, member.Name, context, diagnostics);

            var resultType = GetReferenceResultType(referencedSymbol, context);

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
        ExpressionContext context,
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

            return context.Symbols.GetGroup(members);
        }
        finally
        {
            _symbolListPool.ReturnToPool(members);
        }
    }

    protected virtual Expression BindNameReference(NameReferenceExpression nameRef, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var referencedSymbol = GetNameReference(nameRef.Name, context);

            if (referencedSymbol != null && referencedSymbol == nameRef.ReferencedSymbol)
                return nameRef;

            if (referencedSymbol == null)
                diagnostics.Add(BindingDiagnostics.UnknownName(nameRef.Name).WithLocation(nameRef.Location));

            var resultType = GetReferenceResultType(referencedSymbol, context) ?? context.Symbols.Object;

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
    protected virtual Symbol? GetNameReference(string name, ExpressionContext context)
    {
        var symbols = _symbolListPool.AllocateFromPool();
        try
        {
            context.Scope.FindMatchingSymbols(name, null, symbols, FindScope.First);
            return context.Symbols.GetGroup(symbols);
        }
        finally
        {
            _symbolListPool.ReturnToPool(symbols);
        }
    }

    protected virtual Expression BindNew(NewExpression nex, ExpressionContext context)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var argContext = context.WithTargetType(null).WithInflowType(SpecialSymbols.Void);

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

    protected virtual Expression BindNewArraySize(NewArraySizeExpression newArraySize, ExpressionContext context)
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
            var resultType = elementTypeSymbol != null ? context.Symbols.GetArray(elementTypeSymbol) : null;

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

    protected virtual Expression BindNewArrayInit(NewArrayInitExpression newArrayInit, ExpressionContext context)
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
                ?? GetBestCommonType(context, expressions);
            var resultType = elementTypeSymbol != null ? context.Symbols.GetArray(elementTypeSymbol) : null;

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

    protected virtual Expression BindSymbolReference(SymbolReferenceExpression symbolRef, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var referencedSymbol = GetSymbolReference(symbolRef.FullName, context);

            if (referencedSymbol != null && referencedSymbol == symbolRef.ReferencedSymbol)
                return symbolRef;

            if (referencedSymbol == null)
                diagnostics.Add(BindingDiagnostics.UnknownName(symbolRef.FullName).WithLocation(symbolRef.Location));

            var resultType = GetReferenceResultType(referencedSymbol, context) ?? context.Symbols.Object;

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
    protected virtual Symbol? GetSymbolReference(string fullName, ExpressionContext context)
    {
        return context.Symbols.GetSymbol<Symbol>(fullName);
    }

    /// <summary>
    /// Determines the result type of a referenced <see cref="Symbol"/>.
    /// </summary>
    protected virtual TypeSymbol? GetReferenceResultType(Symbol? referencedSymbol, ExpressionContext context) =>
        referencedSymbol switch
        {
            VariableSymbol v => v.VariableType,
            ParameterSymbol p => p.ParameterType,
            FieldSymbol f => f.FieldType,
            PropertySymbol p => p.PropertyType,
            LambdaSymbol f => f,
            GroupSymbol g => g,
            MethodSymbol => SpecialSymbols.Void,
            TypeSymbol => context.Symbols.Type,
            NamespaceSymbol => context.Symbols.Namespace,
            _ => null
        };

    protected virtual Expression BindVariable(VariableExpression declaration, ExpressionContext context)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            Expression? variableType = null;
            TypeSymbol? vtype = null;
            Expression? initializer = null;

            context = context.WithInflowType(SpecialSymbols.Void);

            if (declaration.VariableType != null)
            {
                variableType = BindExpression(declaration.VariableType, context.WithTargetType(null));
                vtype = variableType?.ReferencedSymbol as TypeSymbol ?? context.Symbols.Object;
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
                vtype = context.Symbols.Object;
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
                : new VariableSymbol(declaration.Name, vtype ?? SpecialSymbols.Any);

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

    protected virtual VoidExpression BindVoid(VoidExpression vex, ExpressionContext context)
    {
        // nothing to actually bind
        return vex;
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
    #endregion

    #region DeferredBinding
    private class DeferredBinding : DeclarationBinding
    {
        private readonly SemanticBinder _binder;
        private readonly DeclarationContext _context;

        /// <summary>
        /// All the declarations before binding
        /// </summary>
        public override ImmutableList<Declaration> UnboundDeclarations { get; }

        /// <summary>
        /// The namespace including only external symbols
        /// </summary>
        public override NamespaceSymbol ExternalSymbols { get; }

        /// <summary>
        /// The namespace including only declared symbols
        /// </summary>
        public override NamespaceSymbol DeclarationSymbols { get; }

        /// <summary>
        /// The namespace including both declared and external symbol.
        /// </summary>
        public override NamespaceSymbol GlobalNamespace => _context.GlobalNamespace;

        public DeferredBinding(
            SemanticBinder binder,
            DeclarationContext context,
            NamespaceSymbol externalSymbols,
            NamespaceSymbol declarationSymbols,
            ImmutableList<Declaration> unboundDeclarations)
        {
            _binder = binder;
            _context = context;
            ExternalSymbols = externalSymbols;
            DeclarationSymbols = declarationSymbols;
            UnboundDeclarations = unboundDeclarations;
        }

        private ImmutableList<Declaration>? _boundDeclarations;

        /// <summary>
        /// All the declarations after binding
        /// </summary>
        public override ImmutableList<Declaration> BoundDeclarations
        {
            get
            {
                if (_boundDeclarations == null)
                {
                    var tmp = UnboundDeclarations
                        .Select(u => GetBoundDeclaration(u))
                        .OfType<Declaration>()
                        .ToImmutableList();
                    Interlocked.CompareExchange(ref _boundDeclarations, tmp, null);
                }

                return _boundDeclarations ?? ImmutableList<Declaration>.Empty;
            }
        }

        private ImmutableDictionary<Declaration, Declaration> _unboundToBoundMap =
            ImmutableDictionary<Declaration, Declaration>.Empty;

        public override Declaration? GetBoundDeclaration(Declaration unboundDeclaration)
        {
            if (!_unboundToBoundMap.TryGetValue(unboundDeclaration, out var boundDeclaration))
            {
                var tmp = _binder.BindDeclaration(unboundDeclaration, _context);
                boundDeclaration = ImmutableInterlocked.GetOrAdd(ref _unboundToBoundMap, unboundDeclaration, tmp);
            }

            return boundDeclaration;
        }
    }
    #endregion

    #region BindingContext

    protected abstract class BindingContext
    {
        /// <summary>
        /// A cache of common symbols.
        /// </summary>
        public abstract SymbolCache Symbols { get; }

        /// <summary>
        /// The global namespace for all declared and external symbols
        /// </summary>
        public NamespaceSymbol GlobalNamespace => this.Symbols.GlobalNamespace;

        /// <summary>
        /// The current binding scope.
        /// </summary>
        public abstract BindingScope Scope { get; }

        public abstract BindingContext WithScope(BindingScope scope);
    }

    protected abstract class DeclarationContext : BindingContext
    {
        public abstract bool TryGetSymbol(Declaration declaration, [NotNullWhen(true)] out Symbol? symbol);
        public abstract ExpressionContext ExpressionContext { get; }

        public override DeclarationContext WithScope(BindingScope scope)
        {
            throw new NotImplementedException();
        }
    }

    protected class SymbolContext : BindingContext
    {
        public override SymbolCache Symbols { get; }
        public override BindingScope Scope { get; }
        private readonly Dictionary<Declaration, Symbol> _map;

        private SymbolContext(SymbolCache symbols, BindingScope scope, Dictionary<Declaration, Symbol> map)
        {
            Symbols = symbols;
            Scope = scope;
            _map = map;
        }

        public SymbolContext(SymbolCache symbols, BindingScope scope)
            : this(symbols, scope, new Dictionary<Declaration, Symbol>())
        {
        }

        public override SymbolContext WithScope(BindingScope scope)
        {
            return new SymbolContext(this.Symbols, scope, _map);
        }

        public void Map(Declaration declaration, Symbol symbol)
        {
            _map.Add(declaration, symbol);
        }

        public void Map(IEnumerable<Declaration> declarations, Symbol symbol)
        {
            foreach (var decl in declarations)
            {
                Map(decl, symbol);
            }
        }

        private DeclContext? _declContext;

        public DeclarationContext DeclarationContext
        {
            get
            {
                if (_declContext == null)
                {
                    var tmp = new DeclContext(this.Symbols, this.Scope, _map);
                    Interlocked.CompareExchange(ref _declContext, tmp, null);
                }

                return _declContext;
            }
        }

        private class DeclContext : DeclarationContext
        {
            public override SymbolCache Symbols { get; }
            public override BindingScope Scope { get; }
            private readonly Dictionary<Declaration, Symbol> _map;

            public DeclContext(
                SymbolCache symbols, 
                BindingScope scope, 
                Dictionary<Declaration, Symbol> map)
            {
                Symbols = symbols;
                Scope = scope;
                _map = map;
            }

            public override DeclarationContext WithScope(BindingScope scope)
            {
                if (scope == this.Scope)
                    return this;
                return new DeclContext(this.Symbols, scope, _map);
                throw new NotImplementedException();
            }

            public override bool TryGetSymbol(Declaration declaration, [NotNullWhen(true)] out Symbol? symbol)
            {
                return _map.TryGetValue(declaration, out symbol);
            }

            private ExpressionContext? _exprContext;

            public override ExpressionContext ExpressionContext
            {
                get
                {
                    if (_exprContext == null)
                    {
                        var tmp = new ExpressionContext(this.Symbols, this.Scope);
                        Interlocked.CompareExchange(ref _exprContext, tmp, null);
                    }

                    return _exprContext;
                }
            }
        }
    }

    protected class ExpressionContext : BindingContext
    {
        /// <summary>
        /// A cache of common symbols.
        /// </summary>
        public override SymbolCache Symbols { get; }

        /// <summary>
        /// The current binding scope.
        /// </summary>
        public override BindingScope Scope { get; }

        /// <summary>
        /// If true the binder will attempt to rebind an already bound semantic subtree.
        /// </summary>
        public bool Rebind { get; }

        /// <summary>
        /// The type inflowing to a label from the previous expression in a block.
        /// </summary>
        public TypeSymbol InflowType { get; }

        /// <summary>
        /// The current target type available for an expression.
        /// </summary>
        public TypeSymbol? TargetType { get; }

        private ExpressionContext(
            SymbolCache symbols,
            BindingScope scope,
            bool rebind,
            TypeSymbol? targetType,
            TypeSymbol inflowType)
        {
            this.Symbols = symbols;
            this.Scope = scope;
            this.Rebind = rebind;
            this.TargetType = targetType;
            this.InflowType = inflowType;
        }

        public ExpressionContext(SymbolCache symbols, BindingScope scope)
            : this(symbols, scope, false, null, SpecialSymbols.Void)
        {
        }

        public ExpressionContext(NamespaceSymbol globalNamespace, BindingScope scope)
            : this(SymbolCache.From(globalNamespace), scope)
        {
        }

        public override ExpressionContext WithScope(BindingScope scope) =>
            new ExpressionContext(this.Symbols, scope, this.Rebind, this.TargetType, this.InflowType);

        public ExpressionContext WithRebind(bool rebind) =>
            new ExpressionContext(this.Symbols, this.Scope, this.Rebind, this.TargetType, this.InflowType);

        public ExpressionContext WithInflowType(TypeSymbol inflowType) =>
            new ExpressionContext(this.Symbols, this.Scope, this.Rebind, this.TargetType, inflowType);

        public ExpressionContext WithTargetType(TypeSymbol? targetType) =>
            new ExpressionContext(this.Symbols, this.Scope, this.Rebind, targetType, this.InflowType);
    };
    #endregion

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
}
