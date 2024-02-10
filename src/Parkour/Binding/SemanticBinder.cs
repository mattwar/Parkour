using System.Diagnostics.CodeAnalysis;

namespace Parkour.Binding;
using Semantics;
using Symbols;

/// <summary>
/// Converts unbound declarations and expressions into bound declarations and expressions,
/// by creating new instances of each with declared or referenced symbols assigned
/// and including any diagnostics.
/// </summary>
public class SemanticBinder
{
    /// <summary>
    /// Creates a new instance of <see cref="SemanticBinder"/> that 
    /// converts unbound declarations and expressions into bound declarations and expressions,
    /// by creating new instances of each with declared or referenced symbols assigned
    /// and including any diagnostics.
    /// </summary>
    public SemanticBinder()
    {
    }

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
        public GlobalNamespaceSymbol GlobalNamespace => this.Symbols.GlobalNamespace;

        /// <summary>
        /// The current binding scope.
        /// </summary>
        public abstract BindingScope Scope { get; }

        public abstract BindingContext WithScope(BindingScope scope);
    }

    /// <summary>
    /// Contains useful state for binding declarations.
    /// </summary>
    protected abstract class DeclarationContext : BindingContext
    {
        public abstract bool TryGetSymbol(Declaration declaration, [NotNullWhen(true)] out Symbol? symbol);
        public abstract ExpressionContext ExpressionContext { get; }

        public override DeclarationContext WithScope(BindingScope scope)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Contains useful state for creating symbols for declarations.
    /// </summary>
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

        internal ImmutableDictionary<Symbol, ImmutableList<Declaration>> GetSymbolToUnboundDeclarationMap()
        {
            return _map
                .GroupBy(kvp => kvp.Value)
                .ToImmutableDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToImmutableList());
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

    /// <summary>
    /// Contains useful state for binding expressions.
    /// </summary>
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
        /// The current target type available for an expression.
        /// </summary>
        public TypeSymbol? TargetType { get; }

        private readonly Dictionary<LabelExpression, LabelSymbol> _labelToSymbolMap;

        private ExpressionContext(
            SymbolCache symbols,
            BindingScope scope,
            bool rebind,
            TypeSymbol? targetType,
            Dictionary<LabelExpression, LabelSymbol>? labelToSymbolMap)
        {
            this.Symbols = symbols;
            this.Scope = scope;
            this.Rebind = rebind;
            this.TargetType = targetType;
            _labelToSymbolMap = labelToSymbolMap ?? new Dictionary<LabelExpression, LabelSymbol>();

        }

        public ExpressionContext(SymbolCache symbols, BindingScope scope)
            : this(symbols, scope, false, null, null)
        {
        }

        public ExpressionContext(GlobalNamespaceSymbol globalNamespace, BindingScope scope)
            : this(SymbolCache.From(globalNamespace), scope)
        {
        }

        /// <summary>
        /// Create a new instance with <see cref="Scope"/> assigned.
        /// </summary>
        public override ExpressionContext WithScope(BindingScope scope) =>
            new ExpressionContext(this.Symbols, scope, this.Rebind, this.TargetType, _labelToSymbolMap);

        /// <summary>
        /// Creates a new instance with <see cref="Rebind"/> assigned.
        /// </summary>
        public ExpressionContext WithRebind(bool rebind) =>
            new ExpressionContext(this.Symbols, this.Scope, this.Rebind, this.TargetType, _labelToSymbolMap);

        /// <summary>
        /// Createsa  new instance with <see cref="TargetType"/> assigned.
        /// </summary>
        public ExpressionContext WithTargetType(TypeSymbol? targetType) =>
            new ExpressionContext(this.Symbols, this.Scope, this.Rebind, targetType, _labelToSymbolMap);

        /// <summary>
        /// Sets the <see cref="LabelSymbol"/> associated with its declaring <see cref="LabelExpression"/>
        /// </summary>
        public void SetLabelSymbol(LabelExpression label, LabelSymbol symbol)
        {
            _labelToSymbolMap[label] = symbol;
        }

        /// <summary>
        /// Gets the <see cref="LabelSymbol"/> associated to the declaring <see cref="LabelExpression"/>
        /// </summary>
        public LabelSymbol? GetLabelSymbol(LabelExpression label)
        {
            _labelToSymbolMap.TryGetValue(label, out var target);
            return target;
        }
    };
    #endregion

    #region Declarations
    /// <summary>
    /// Binds a set of declarations given external symbols.
    /// </summary>
    /// <param name="declarations">The declarations to bind.</param>
    /// <param name="externalSymbols">The global namespace containing all external symbols.</param>
    public DeclarationBinding BindDeclarations(
        IEnumerable<Declaration> declarations,
        GlobalNamespaceSymbol externalSymbols)
    {
        GlobalNamespaceSymbol? declarationSymbols = null;
        SymbolContext? context = null;

        // create combined symbols' global namespace including all declared and external symbols
        var combinedSymbols = CombinedSymbols.CreateCombinedGlobalNamespace(
            globals =>
            {
                // make global namespace for all declared symbols
                declarationSymbols = new GlobalNamespaceSymbol(
                me =>
                {
                    // all top level declarations that are not global namespace declarations
                    var topLevelDeclarations = declarations.SelectMany(d =>
                        d is NamespaceDeclaration nd && nd.IsGlobalNamespace
                            ? (IEnumerable<Declaration>)nd.Declarations
                            : new[] { d })
                        .ToImmutableList();

                    return CreateAndCombineDeclarationSymbols(context!, me, topLevelDeclarations);
                });

                // return both declared and external global namespaces to be combined.
                return [externalSymbols, declarationSymbols];
            });

        // symbol context for use in creating declaration symbols.
        var cache = SymbolCache.From(combinedSymbols);
        var scope = GetEmptyBindingScope().AddMembers(combinedSymbols);
        context = new SymbolContext(cache, scope);

        // force evaluation of global namespace symbol members
        _ = combinedSymbols.Members;
        _ = declarationSymbols!.Members;

        // force creation of all declared symbols
        // this side effect of building a map of declaration->symbol for use in binding
        declarationSymbols.WalkDeclarations(s => { });

        return new DeferredBinding(
            this,
            context.DeclarationContext,
            externalSymbols,
            declarationSymbols,
            declarations.ToImmutableList(),
            context.GetSymbolToUnboundDeclarationMap());
    }

    /// <summary>
    /// Creates symbols for the declarations,
    /// combining same named namespace declaration symbols.
    /// </summary>
    private ImmutableList<Symbol> CreateAndCombineDeclarationSymbols(
        SymbolContext context,
        NamespaceSymbol @namespace,
        IEnumerable<Declaration> declarations)
    {
        var newMembers = new List<Symbol>();

        var namespaceMemberGroups = declarations
            .OfType<NamespaceDeclaration>()
            .GroupBy(d => d.Name)
            .ToList();

        var newMemberNamespaces = namespaceMemberGroups
            .Select(g => new NamespaceSymbol(
                g.Key,
                @namespace,
                me =>
                {
                    context.Map(g, me);
                    var newContext = context.WithScope(context.Scope.AddMembers(me).AddSymbol(me));
                    return CreateAndCombineDeclarationSymbols(newContext, me, g.SelectMany(n => n.Declarations));
                }))
            .ToList();

        newMembers.AddRange(newMemberNamespaces);

        var otherMembers = declarations
            .Where(d => !(d is NamespaceDeclaration))
            .ToList();

        var otherMemberSymbols =
            otherMembers
            .Select(d => CreateAndMapDeclarationSymbol(context, @namespace, d))
            .OfType<Symbol>() // filter out nulls
            .ToList();

        newMembers.AddRange(otherMemberSymbols);

        return newMembers.ToImmutableList();
    }

    protected Symbol? CreateAndMapDeclarationSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        Declaration declaration)
    {
        var symbol = CreateDeclarationSymbol(context, declaringSymbol, declaration);
        if (symbol != null)
        {
            context.Map(declaration, symbol);
        }
        return symbol;
    }

    /// <summary>
    /// Creates the corresponding symbol for the declaration.
    /// </summary>
    protected virtual Symbol? CreateDeclarationSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        Declaration declaration)
    {
        switch (declaration)
        {
            case TypeParameterDeclaration tp:
                return new TypeParameterSymbol(tp.Name);

            case ClassDeclaration cd:
                var classContext = context;
                var classSymbol = new TypeSymbol(
                    cd.Name,
                    declaringSymbol,
                    cd.Access,
                    cd.Modifiers,
                    me => cd.TypeParameters.Select(tp => (TypeParameterSymbol)CreateAndMapDeclarationSymbol(context, me, tp)!).ToImmutableList()!,
                    () => ImmutableList<TypeSymbol>.Empty,
                    () => cd.BaseTypes.Select(bt => GetType(classContext, bt) ?? SpecialSymbols.Unknown).ToImmutableList()!,
                    me => cd.Declarations.Select(d => CreateAndMapDeclarationSymbol(classContext, me, d)).Where(s => s != null).ToImmutableList()!,
                    constructedFrom: null);

                classContext = classContext.WithScope(
                    classContext.Scope
                        .AddMembers(classSymbol)
                        .AddSymbol(classSymbol)
                        .AddSymbols(classSymbol.TypeParameters));

                return classSymbol;

            case ConstructorDeclaration cd:
                return new ConstructorSymbol(
                    (TypeSymbol)declaringSymbol!,
                    cd.Access,
                    cd.Modifiers,
                    me => cd.Parameters.Select(p => (ParameterSymbol)CreateAndMapDeclarationSymbol(context, me, p)!).ToImmutableList()!
                    );

            case MethodDeclaration md:
                var mmods = md.Modifiers
                    | (declaringSymbol is NamespaceSymbol ? SymbolModifier.Static : SymbolModifier.None);
                return new MethodSymbol(
                    md.Name,
                    declaringSymbol,
                    md.Access,
                    mmods,
                    me => ImmutableList<TypeParameterSymbol>.Empty,
                    () => ImmutableList<TypeSymbol>.Empty,
                    me => md.Parameters.Select(p => (ParameterSymbol)CreateAndMapDeclarationSymbol(context, me, p)!).ToImmutableList()!,
                    () => GetType(context, md.ReturnType) ?? SpecialSymbols.Unknown,
                    constructedFrom: null
                    );

            case ParameterDeclaration pd:
                return new ParameterSymbol(
                    pd.Name,
                    declaringSymbol,
                    () => pd.ParameterType != null 
                        ? GetType(context, pd.ParameterType) ?? SpecialSymbols.Unknown 
                        : SpecialSymbols.Any
                    );

            case FieldDeclaration fd:
                return new FieldSymbol(
                    fd.Name,
                    declaringSymbol as TypeSymbol,
                    fd.Access,
                    fd.Modifiers,
                    () => GetType(context, fd.FieldType) ?? SpecialSymbols.Unknown
                    );

            case PropertyDeclaration pd:
                return new PropertySymbol(
                    pd.Name,
                    declaringSymbol as TypeSymbol,
                    pd.Access,
                    pd.Modifiers,
                    () => GetType(context, pd.PropertyType) ?? SpecialSymbols.Unknown,
                    pd.BackingField != null
                        ? me => (FieldSymbol)CreateAndMapDeclarationSymbol(context, me, pd.BackingField)!
                        : null,
                    me => (MethodSymbol)CreateAndMapDeclarationSymbol(context, me, pd.GetMethod)!,
                    pd.SetMethod != null
                        ? me => (MethodSymbol)CreateAndMapDeclarationSymbol(context, me, pd.SetMethod)!
                        : null
                );

            case UsingDeclaration:
                // this declaration does not have a symbol that is part of a namespace.
                return null;

            default:
                throw new InvalidOperationException($"Unhandled declaration type '{declaration.GetType().Name}' in {nameof(SemanticBinder)}.{nameof(CreateDeclarationSymbol)}");
        }
    }

    /// <summary>
    /// Binds a declaration to its associated symbol
    /// </summary>
    protected virtual Declaration BindDeclaration(
        DeclarationContext context,
        Declaration declaration)
    {
        switch (declaration)
        {
            case ClassDeclaration cld:
                return BindClass(context, cld);
            case ConstructorDeclaration cd:
                return BindConstructor(context, cd);
            case FieldDeclaration fd:
                return BindField(context, fd);
            case MethodDeclaration md:
                return BindMethod(context, md);
            case NamespaceDeclaration nd:
                return BindNamespace(context, nd);
            case ParameterDeclaration prd:
                return BindParameter(context, prd);
            case PropertyDeclaration pd:
                return BindProperty(context, pd);
            case TypeParameterDeclaration tp:
                return BindTypeParameter(context, tp);
            case UsingDeclaration ud:
                return BindUsing(context, ud);
            default:
                throw new InvalidCastException($"Unhandled declaration '{declaration.GetType().Name}' in {nameof(SemanticBinder)}.{nameof(BindDeclaration)}");
        }
    }

    /// <summary>
    /// Binds a list of declarations
    /// </summary>
    protected virtual ImmutableList<TDeclaration> BindDeclarations<TDeclaration>(
        DeclarationContext context,
        ImmutableList<TDeclaration> list)
        where TDeclaration : Declaration
    {
        return list.Rewrite(d => (TDeclaration)BindDeclaration(context, d));
    }

    #region Class Declaration
    /// <summary>
    /// Binds <see cref="ClassDeclaration"/>
    /// </summary>
    protected virtual ClassDeclaration BindClass(
        DeclarationContext context,
        ClassDeclaration cd)
    {
        var classSymbol = context.TryGetSymbol(cd, out var symbol) ? symbol as TypeSymbol : null;

        var typeParameters = BindDeclarations(context, cd.TypeParameters);

        // put class symbol in scope for baseTypes and members
        var classContext = classSymbol != null
            ? context.WithScope(context.Scope.AddMembers(classSymbol).AddSymbol(classSymbol))
            : context;

        var baseTypes = BindExpressionList(classContext.ExpressionContext, cd.BaseTypes);

        // add all class members to scope
        var bodyContext = classContext;
        if (classSymbol != null)
        {
            bodyContext = bodyContext.WithScope(bodyContext.Scope.AddMembers(classSymbol).AddSymbol(classSymbol));
        }

        var declarations = BindDeclarations(bodyContext, cd.Declarations);

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
    #endregion

    protected virtual ConstructorDeclaration BindConstructor(
        DeclarationContext context,
        ConstructorDeclaration cd)
    {
        var constructorSymbol = context.TryGetSymbol(cd, out var symbol) ? symbol as ConstructorSymbol : null;
        var parameters = BindDeclarations(context, cd.Parameters);
        var returnLabel = new LabelSymbol(LabelSymbol.ReturnLabelName, SpecialSymbols.Void);
        var bodyContext = context.ExpressionContext.WithScope(context.Scope.AddSymbol(returnLabel));

        // add parameters to scope for body
        if (constructorSymbol != null
            && constructorSymbol.Parameters.Count > 0)
        {
            bodyContext = bodyContext.WithScope(bodyContext.Scope.AddSymbols(constructorSymbol.Parameters));
        }

        var body = BindExpression(bodyContext, cd.Body);

        return new ConstructorDeclaration(
            cd.Access,
            cd.Modifiers,
            parameters,
            body,
            cd.Location,
            constructorSymbol,
            returnLabel,
            null
            );
    }

    #region Field Declaration
    /// <summary>
    /// Binds <see cref="FieldDeclaration"/>
    /// </summary>
    protected virtual FieldDeclaration BindField(
        DeclarationContext context,
        FieldDeclaration fd)
    {
        var fieldSymbol = context.TryGetSymbol(fd, out var symbol) ? symbol as FieldSymbol : null;
        var fieldType = BindExpression(context.ExpressionContext, fd.FieldType);
        var initializer = fd.Initializer != null
            ? BindExpression(context.ExpressionContext, fd.Initializer)
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
    #endregion

    #region Method Declaration
    /// <summary>
    /// Binds <see cref="MethodDeclaration"/>
    /// </summary>
    protected virtual MethodDeclaration BindMethod(
        DeclarationContext context,
        MethodDeclaration md)
    {
        var methodSymbol = context.TryGetSymbol(md, out var symbol) ? symbol as MethodSymbol : null;

        var typeParameters = BindDeclarations(context, md.TypeParameters);
        var parameters = BindDeclarations(context, md.Parameters);
        var returnType = BindExpression(context.ExpressionContext, md.ReturnType);
        var returnLabel = new LabelSymbol(LabelSymbol.ReturnLabelName, methodSymbol?.ReturnType ?? SpecialSymbols.Void);

        var bodyContext = context.ExpressionContext.WithScope(context.Scope.AddSymbol(returnLabel));

        // add parameters to scope for body
        if (methodSymbol != null
            && methodSymbol.Parameters.Count > 0)
        {
            bodyContext = bodyContext.WithScope(bodyContext.Scope.AddSymbols(methodSymbol.Parameters));
        }

        var body = BindExpression(bodyContext, md.Body);

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
            returnLabel,
            md.Diagnostics
            );
    }
    #endregion

    #region Namespace Declaration
    /// <summary>
    /// Binds <see cref="NamespaceDeclaration"/>
    /// </summary>
    protected virtual NamespaceDeclaration BindNamespace(
        DeclarationContext context,
        NamespaceDeclaration nd)
    {
        var nsSymbol = context.TryGetSymbol(nd, out var symbol) ? symbol as NamespaceSymbol : null;

        var bodyContext = context;
        if (nsSymbol != null)
        {
            bodyContext = bodyContext.WithScope(bodyContext.Scope.AddMembers(nsSymbol).AddSymbol(nsSymbol));
        }

        var (declarations, finalContext) = nd.Declarations.Rewrite(bodyContext, (d, _context) =>
        {
            var nd = BindDeclaration(_context, d);

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
    #endregion

    #region Parameter Declaration
    /// <summary>
    /// Binds <see cref="ParameterDeclaration"/>
    /// </summary>
    protected virtual ParameterDeclaration BindParameter(
        DeclarationContext context,
        ParameterDeclaration pd)
    {
        var parameterSymbol = context.TryGetSymbol(pd, out var symbol) ? symbol as ParameterSymbol : null;

        var parameterType = pd.ParameterType != null
            ? BindExpression(context.ExpressionContext, pd.ParameterType)
            : null;

        return new ParameterDeclaration(
            pd.Name,
            parameterType,
            pd.Location,
            parameterSymbol,
            pd.Diagnostics
            );
    }
    #endregion

    #region Property Declaration
    /// <summary>
    /// Binds <see cref="PropertyDeclaration"/>
    /// </summary>
    protected virtual PropertyDeclaration BindProperty(
        DeclarationContext context,
        PropertyDeclaration pd)
    {
        var propertySymbol = context.TryGetSymbol(pd, out var symbol) ? symbol as PropertySymbol : null;

        var propertyType = BindExpression(context.ExpressionContext, pd.PropertyType);

        var backingField = pd.BackingField != null
            ? (FieldDeclaration)BindDeclaration(context, pd.BackingField)
            : null;

        var methodContext = context;

        if (propertySymbol?.BackingField != null)
        {
            methodContext = methodContext.WithScope(
                methodContext.Scope.AddSymbol(propertySymbol.BackingField));
        }

        var getMethod = (MethodDeclaration)BindDeclaration(methodContext, pd.GetMethod);

        var setMethod = pd.SetMethod != null
            ? (MethodDeclaration)BindDeclaration(methodContext, pd.SetMethod)
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
    #endregion
    
    #region TypeParameter Declaration
    /// <summary>
    /// Binds <see cref="TypeParameterDeclaration"/>
    /// </summary>
    protected virtual TypeParameterDeclaration BindTypeParameter(
        DeclarationContext context,
        TypeParameterDeclaration tp)
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
    #endregion
    
    #region Using Declaration
    /// <summary>
    /// Binds <see cref="UsingDeclaration"/>
    /// </summary>
    protected virtual UsingDeclaration BindUsing(
        DeclarationContext context,
        UsingDeclaration ud)
    {
        var expression = BindExpression(context.ExpressionContext, ud.Expression);

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

    #endregion

    #region Expressions
    /// <summary>
    /// Binds all unbound expressions
    /// </summary>
    public Expression BindExpression(
        Expression expression, 
        GlobalNamespaceSymbol globalNamespace)
    {
        var scope = GetEmptyBindingScope().AddSymbol(globalNamespace);
        return BindExpression(expression, globalNamespace, scope);
    }

    /// <summary>
    /// Binds all unbound expressions
    /// </summary>
    public Expression BindExpression(
        Expression expression, 
        GlobalNamespaceSymbol globalNamespace, 
        BindingScope scope)
    {
        var context = new ExpressionContext(globalNamespace, scope);
        return BindExpression(context, expression);
    }

    /// <summary>
    /// Binds all unbound expressions
    /// </summary>
    protected virtual Expression BindExpression(ExpressionContext context, Expression expression)
    {
        if (!(context.Rebind || expression.IsUnbound))
            return expression;

        switch (expression)
        {
            case ArrayExpression array:
                return BindArray(context, array);

            case ArityExpression arity:
                return BindArity(context, arity);

            case AssignExpression assign:
                return BindAssign(context, assign);

            case BlockExpression block:
                return BindBlock(context, block);

            case BranchExpression branch:
                return BindBranch(context, branch);

            case CallExpression call:
                return BindCall(context, call);

            case ConditionExpression condition:
                return BindCondition(context, condition);

            case ConstantExpression constant:
                return BindConstant(context, constant);

            case ConvertExpression convert:
                return BindConvert(context, convert);

            case DefaultExpression dex:
                return BindDefault(context, dex);

            case LabelExpression label:
                return BindLabel(context, label);

            case LambdaExpression lambda:
                return BindLambda(context, lambda);

            case LoopExpression loop:
                return BindLoop(context, loop);

            case MemberExpression member:
                return BindMember(context, member);

            case NameReferenceExpression nameRef:
                return BindNameReference(context, nameRef);

            case NewExpression @new:
                return BindNew(context, @new);

            case NewArrayInitExpression newArrayInit:
                return BindNewArrayInit(context, newArrayInit);

            case NewArraySizeExpression newArraySize:
                return BindNewArraySize(context, newArraySize);

            case OperatorExpression opex:
                return BindOperator(context, opex);

            case SymbolReferenceExpression symbolRef:
                return BindSymbolReference(context, symbolRef);

            case TypeArgumentsExpression typeArgs:
                return BindTypeArguments(context, typeArgs);

            case VariableExpression variable:
                return BindVariable(context, variable);

            case VoidExpression vex:
                return BindVoid(context, vex);

            default:
                throw new InvalidOperationException($"Unhandled semantic '{expression.GetType().Name}' in {nameof(SemanticBinder)}.BindExpression");
        }
    }

    /// <summary>
    /// Binds a list of expressions.
    /// </summary>
    public ImmutableList<TExpression> BindExpressionList<TExpression>(
        ImmutableList<TExpression> expressions,
        GlobalNamespaceSymbol globalNamespace)
        where TExpression : Expression
    {
        var scope = GetEmptyBindingScope().AddSymbol(globalNamespace);
        return BindExpressionList(expressions, globalNamespace, scope);
    }

    /// <summary>
    /// Binds a list of expressions.
    /// </summary>
    public ImmutableList<TExpression> BindExpressionList<TExpression>(
        ImmutableList<TExpression> expressions,
        GlobalNamespaceSymbol globalNamespace,
        BindingScope scope)
        where TExpression : Expression
    {
        var context = new ExpressionContext(globalNamespace, scope);
        return BindExpressionList(context, expressions);
    }

    /// <summary>
    /// Binds a list of <see cref="Expression"/>
    /// </summary>
    protected virtual ImmutableList<TExpression> BindExpressionList<TExpression>(
        ExpressionContext context, ImmutableList<TExpression> expressions)
        where TExpression : Expression
    {
        if (expressions.Count == 0)
            return expressions;
        return expressions.Rewrite(e => (TExpression)BindExpression(context, e));
    }

    /// <summary>
    /// Gets the bound type of a type expression
    /// </summary>
    protected virtual TypeSymbol? GetType(ExpressionContext context, Expression typeExpression)
    {
        if (typeExpression.IsUnbound)
            typeExpression = BindExpression(context, typeExpression);
        return typeExpression.ReferencedSymbol as TypeSymbol;
    }

    /// <summary>
    /// Gets the bound type of a type expression
    /// </summary>
    protected TypeSymbol? GetType(DeclarationContext context, Expression typeExpression) =>
        GetType(context.ExpressionContext, typeExpression);

    /// <summary>
    /// Gets the bound type of a type expression
    /// </summary>
    protected TypeSymbol? GetType(SymbolContext context, Expression typeExpression) =>
        GetType(context.DeclarationContext, typeExpression);

    /// <summary>
    /// Gets an empty <see cref="BindingScope"/>.
    /// </summary>
    public virtual BindingScope GetEmptyBindingScope() =>
        SimpleBindingScope.Empty;

    #region Array Expression
    /// <summary>
    /// Binds <see cref="ArrayExpression"/>,
    /// converting referenced symbols into arrays of those symbols.
    /// </summary>
    protected virtual Expression BindArray(ExpressionContext context, ArrayExpression array)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(context, array.Expression);
            var elementType = expression.ReferencedSymbol as TypeSymbol;
            var referencedSymbol = elementType != null ? context.Symbols.GetArray(elementType) : null;
            var resultType = GetReferenceResultType(context, referencedSymbol);

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
    #endregion

    #region Arity Expression
    /// <summary>
    /// Binds <see cref="ArityExpression"/>,
    /// filtering referenced symbols to only those matching the specified arity.
    /// </summary>
    protected virtual Expression BindArity(ExpressionContext context, ArityExpression arity)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(context, arity.Expression);

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

            var resultType = GetReferenceResultType(context, referencedSymbol);

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
    #endregion

    #region Assign Expression
    /// <summary>
    /// Binds <see cref="AssignExpression"/>
    /// </summary>
    protected virtual Expression BindAssign(ExpressionContext context, AssignExpression assign)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var target = BindExpression(context.WithTargetType(null), assign.Target);
            var assignType = GetReferenceResultType(context, target.ReferencedSymbol) ?? context.Symbols.Object;
            var source = BindExpression(context.WithTargetType(assignType), assign.Source);
            source = ConvertTo(context, source, assignType);

            if (IsValidAssignmentTarget(assign.ReferencedSymbol))
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

    /// <summary>
    /// Determines if a symbol is a valid assignment target symbol
    /// </summary>
    protected virtual bool IsValidAssignmentTarget(Symbol? symbol) =>
        symbol switch
        {
            VariableSymbol => true,
            FieldSymbol => true,
            PropertySymbol => true,
            _ => false
        };
    #endregion

    #region Block Expression
    /// <summary>
    /// Binds a <see cref="BlockExpression"/>
    /// </summary>
    protected virtual Expression BindBlock(ExpressionContext context, BlockExpression block)
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
                        ? GetType(labelContext, label.ReceivingType)
                        : context.Symbols.Void;
                    var labelSymbol = label.LabelSymbol ?? new LabelSymbol(label.Name, type);
                    labelSymbols.Add(labelSymbol);
                    context.SetLabelSymbol(label, labelSymbol);
                }
            }

            context = context.WithScope(context.Scope.AddSymbols(labelSymbols));

            // bind expressions 
            (var boundExpressions, _) = block.Expressions.Rewrite(context, (expression, _context) =>
            {
                var boundExpression = BindExpression(_context, expression);

                if (boundExpression is VariableExpression decl
                    && decl.VariableSymbol != null)
                {
                    // if the declaration changes then the variable is now different 
                    // so the rest of the block needs to be rebound in case it references the old variable
                    _context = _context.WithRebind(context.Rebind || boundExpression != expression);
                    _context = _context.WithScope(context.Scope.AddSymbol(decl.VariableSymbol));
                }

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
    /// Gets all the branch expression types for a label.
    /// </summary>
    protected virtual void GetBranchExpressionTypes(Expression blockBody, LabelSymbol label, List<TypeSymbol> types)
    {
        types.AddRange(
            blockBody.SelectWhere(
                s => s is Expression e && !HasIsolatedBody(e),
                s => s is BranchExpression b && b.LabelSymbol == label,
                s => ((BranchExpression)s).Expression != null ? ((BranchExpression)s).Expression!.ResultType : SpecialSymbols.Void));
    }

    /// <summary>
    /// True if the expression has a body of its own whose branches are separate from any outer body,
    /// such as the body of a lambda expression.
    /// </summary>
    protected virtual bool HasIsolatedBody(Expression expression) =>
        expression is LambdaExpression;
    #endregion

    #region Branch Expression
    /// <summary>
    /// Binds a <see cref="BranchExpression"/> 
    /// </summary>
    protected virtual Expression BindBranch(ExpressionContext context, BranchExpression branch)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var labelSymbol = GetLabel(context, branch.LabelName);

            var expression = branch.Expression != null
                ? BindExpression(context.WithTargetType(labelSymbol?.Type), branch.Expression)
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
                expression = ConvertTo(context, expression, labelSymbol.Type);
                expression = BindExpression(context.WithRebind(true), expression);
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

    /// <summary>
    /// Gets the label matching the name.
    /// </summary>
    protected virtual LabelSymbol? GetLabel(ExpressionContext context, string name)
    {
        return context.Scope.FindMatchingSymbol<LabelSymbol>(name, null);
    }
    #endregion

    #region Call Expression
    /// <summary>
    /// Binds <see cref="CallExpression"/>
    /// </summary>
    protected virtual Expression BindCall(ExpressionContext context, CallExpression call)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithTargetType(null);

            var expression = BindExpression(context, call.Expression);
            var arguments = BindExpressionList(context, call.Arguments);

            if (expression is LambdaExpression lambda)
            {
                if (lambda.FunctionSymbol != null)
                {
                    candidates.Add(lambda.FunctionSymbol);
                }
            }
            else
            {
                var referencedSymbol = expression.ReferencedSymbol;
                if (referencedSymbol != null)
                {
                    GetCalledSymbolCandidates(context, referencedSymbol, arguments, candidates);
                }
            }

            var location = expression.Location;
            Symbol? calledSymbol = null;

            if (candidates.Count == 0)
            {
                diagnostics.Add(BindingDiagnostics.NoCallableSymbol().WithLocation(location));
            }
            else
            {
                calledSymbol = GetBestCalledSymbol(context, arguments, candidates);

                if (calledSymbol == null)
                {
                    diagnostics.Add(BindingDiagnostics.CallIsAmbiguous().WithLocation(location));
                }
                else
                {
                    var parameters = GetSymbolParameters(calledSymbol);
                    if (parameters.Count != arguments.Count)
                    {
                        diagnostics.Add(BindingDiagnostics.IncorrectNumberOfArguments().WithLocation(location));
                    }
                    else
                    {
                        arguments = ConvertArguments(context, parameters, arguments);
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

    /// <summary>
    /// Gets the instance of the call from the called expression.
    /// </summary>
    protected virtual Expression? GetCallInstance(Expression expression)
    {
        switch (expression)
        {
            case MemberExpression member:
                return member.Instance;
            case AdjustedReferenceExpression filter:
                return GetCallInstance(filter.Expression);
            default:
                return expression;
        }
    }

    /// <summary>
    /// True if the symbol is callable by a <see cref="CallExpression"/>
    /// </summary>
    protected virtual bool IsCallableSymbol(Symbol symbol) =>
        symbol is FunctionSymbol or MethodSymbol or ConstructorSymbol;

    /// <summary>
    /// Gets a called symbol's return type.
    /// </summary>
    protected virtual TypeSymbol? GetCalledSymbolReturnType(Symbol symbol) =>
        symbol switch
        {
            FunctionSymbol f => f.ReturnType,
            MethodSymbol m => m.ReturnType,
            ConstructorSymbol c => c.ConstructedType,
            _ => null
        };

    /// <summary>
    /// Gets the list of candidate symbols for a call with the supplied arguments
    /// </summary>
    protected virtual void GetCalledSymbolCandidates(
        ExpressionContext context,
        Symbol symbol,
        ImmutableList<Expression> arguments,
        List<Symbol> candidates)
    {
        if (symbol is GroupSymbol group)
        {
            candidates.AddRange(group.Symbols.Where(s => IsCallableSymbol(s) && MatchesParameters(context, s, arguments)));
        }
        else if (IsCallableSymbol(symbol))
        {
            candidates.Add(symbol);
        }
    }

    /// <summary>
    /// Gets the best called symbol from a set of symbols relative to the supplied arguments.
    /// </summary>
    protected virtual Symbol? GetBestCalledSymbol(ExpressionContext context, ImmutableList<Expression> arguments, List<Symbol> candidates)
    {
        // todo: be better
        return candidates.FirstOrDefault(c => MatchesParameters(context, c, arguments));
    }

    /// <summary>
    /// Get the parameter symbols for the symbol.
    /// </summary>
    protected virtual ImmutableList<ParameterSymbol> GetSymbolParameters(Symbol symbol) =>
        symbol switch
        {
            FunctionSymbol function => function.Parameters,
            MethodSymbol method => method.Parameters,
            ConstructorSymbol constructor => constructor.Parameters,
            _ => ImmutableList<ParameterSymbol>.Empty
        };

    /// <summary>
    /// Returns true if the callable symbol has parameters that are compatible with the arguments
    /// </summary>
    protected virtual bool MatchesParameters(
        ExpressionContext context,
        Symbol callableSymbol,
        ImmutableList<Expression> arguments) =>
        MatchesParameters(context, GetSymbolParameters(callableSymbol), arguments);

    /// <summary>
    /// Returns true if the set of arguments matches the parameters.
    /// </summary>
    protected virtual bool MatchesParameters(
        ExpressionContext context,
        ImmutableList<ParameterSymbol> parameters,
        ImmutableList<Expression> arguments)
    {
        if (parameters.Count != arguments.Count)
            return false;

        for (int i = 0; i < parameters.Count; i++)
        {
            if (GetConversion(context, arguments[i].ResultType, parameters[i].ParameterType) == ConversionKind.None)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Converts a set of arguments to their corresponding parameter types.
    /// </summary>
    protected virtual ImmutableList<Expression> ConvertArguments(
        ExpressionContext context,
        ImmutableList<ParameterSymbol> parameters,
        ImmutableList<Expression> arguments)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var argument = arguments[i];
            var convertedArg = ConvertTo(context, argument, parameter.ParameterType);
            arguments = arguments.SetItem(i, convertedArg);
        }

        return arguments;
    }
    #endregion

    #region Condition Expression
    /// <summary>
    /// Binds <see cref="ConditionExpression"/>
    /// </summary>
    protected virtual Expression BindCondition(ExpressionContext context, ConditionExpression condition)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var test = BindExpression(context.WithTargetType(context.Symbols.Boolean), condition.Test);
            test = ConvertTo(context, test, context.Symbols.Boolean);

            var whenTrue = BindExpression(context, condition.WhenTrue);
            var whenFalse = BindExpression(context, condition.WhenFalse);

            var resultType = GetBestCommonType(context, [whenTrue.ResultType, whenFalse.ResultType, context.TargetType], voidIsBetter: false);
            if (resultType == null)
            {
                diagnostics.Add(BindingDiagnostics.NoCommonTypeFound().WithLocation(condition.Location));
                resultType = context.Symbols.Object;
            }

            whenTrue = ConvertTo(context, whenTrue, resultType);
            whenFalse = ConvertTo(context, whenFalse, resultType);

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
    #endregion

    #region Constant Expression
    /// <summary>
    /// Binds <see cref="ConstantExpectedAttribute"/>
    /// </summary>
    protected virtual Expression BindConstant(ExpressionContext context, ConstantExpression constant)
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
    #endregion

    #region Conversion Expression
    /// <summary>
    /// Binds <see cref="ConvertExpression"/>
    /// </summary>
    protected virtual Expression BindConvert(ExpressionContext context, ConvertExpression convert)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            var convertedType = convert.ConvertedType != null
                ? BindExpression(context.WithTargetType(null), convert.ConvertedType)
                : null;

            var type = convertedType != null
                ? convertedType.ReferencedSymbol as TypeSymbol
                : convert.ResultType;

            var expression = BindExpression(context.WithTargetType(type), convert.Expression);

            if (convert.ConvertedType == null
                && convert.ResultType != null
                && IsAssignableWithoutConversion(context, expression.ResultType, convert.ResultType))
            {
                // remove unnecessary non-explicit conversion
                return expression;
            }

            _ = GetConversion(context, expression.ResultType, type, out var conversionSymbol, expression.Location, diagnostics);

            if (convert.Expression == expression
                && convert.ConvertedType == convertedType
                && convert.ConversionSymbol == conversionSymbol
                && convert.ResultType == type
                && diagnostics.Count == 0)
            {
                return convert;
            }

            return new ConvertExpression(
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

    /// <summary>
    /// Injects a <see cref="ConvertExpression"/> if the expression's type if a conversion is necessary.
    /// </summary>
    protected virtual Expression ConvertTo(ExpressionContext context, Expression expression, TypeSymbol type)
    {
        // ignore void
        if (type == SpecialSymbols.Void
            || expression.ResultType == SpecialSymbols.Void)
            return expression;

        // remove unnecessary conversions added by this method on prior bindings
        while (expression is ConvertExpression ce
            && ce.ConvertedType == null // added by binding
            && IsAssignableWithoutConversion(context, ce.Expression.ResultType, type))
        {
            expression = ce.Expression;
        }

        if (IsAssignableWithoutConversion(context, expression.ResultType, type))
            return expression;

        // wrap expression with widening conversion and bind it.
        var convert = new ConvertExpression(
            expression,
            convertedType: null,
            expression.Location,
            conversionSymbol: null,
            resultType: type,
            diagnostics: null);

        return BindConvert(context, convert);
    }

    /// <summary>
    /// Returns true if conversion is possible between the source and target type.
    /// </summary>
    protected virtual ConversionKind GetConversion(
        ExpressionContext context,
        TypeSymbol sourceType,
        TypeSymbol targetType)
    {
        return GetConversion(context, sourceType, targetType, out _, null, null);
    }

    /// <summary>
    /// Determines if a conversion if possible between the two types 
    /// and returns the symbol to use to preform the conversion if one is required.
    /// </summary>
    protected virtual ConversionKind GetConversion(
        ExpressionContext context,
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
                diagnostics?.Add(BindingDiagnostics.CannotConvert(sourceType, SpecialSymbols.Unknown).WithLocation(location));
                conversionSymbol = null;
                return ConversionKind.None;
            }
            else if (GetIntrinsicConversion(context, sourceType, targetType) is ConversionKind intrinsicConversion
                && intrinsicConversion != ConversionKind.None)
            {
                conversionSymbol = null;
                return intrinsicConversion;
            }
            else
            {
                GetConversionOperatorCandidates(context, sourceType, targetType, candidates);
                conversionSymbol = GetBestConversionOperator(context, sourceType, targetType, candidates);

                if (conversionSymbol == null)
                {
                    diagnostics?.Add(BindingDiagnostics.CannotConvert(sourceType, targetType ?? SpecialSymbols.Unknown).WithLocation(location));
                    return ConversionKind.None;
                }
                else
                {
                    return conversionSymbol.Name == "op_Implicit"
                        ? ConversionKind.CustomImplicit
                        : ConversionKind.CustomExplicit;
                }
            }
        }
        finally
        {
            _symbolListPool.ReturnToPool(candidates);
        }
    }

    /// <summary>
    /// Determines if the conversion can be done through intrinsic means (not a custom conversion).
    /// </summary>
    protected virtual ConversionKind GetIntrinsicConversion(
        ExpressionContext context,
        TypeSymbol sourceType, 
        TypeSymbol targetType)
    {
        if (sourceType == targetType)
            return ConversionKind.SameType;

        if (sourceType == SpecialSymbols.DoesNotReturn)
            return ConversionKind.DoesNotReturn;

        if (targetType == SpecialSymbols.Any)
            return ConversionKind.Any;

        if (targetType == SpecialSymbols.Unknown)
            return ConversionKind.Unknown;

        if (CanDownCast(context, sourceType, targetType))
            return ConversionKind.BaseType;

        if (CanWiden(context, sourceType, targetType))
            return ConversionKind.Widening;

        return ConversionKind.None;
    }

    /// <summary>
    /// Returns true if the source type can be down cast to the target type.
    /// </summary>
    protected virtual bool CanDownCast(ExpressionContext context, TypeSymbol source, TypeSymbol target)
    {
        if (source == target)
            return true;

        foreach (var sbt in source.BaseTypes)
        {
            if (CanDownCast(context, sbt, target))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if it is possible to upcast the source type to the target type.
    /// </summary>
    protected virtual bool CanUpCast(ExpressionContext context, TypeSymbol source, TypeSymbol target)
    {
        if (target == source)
            return true;

        foreach (var tbt in target.BaseTypes)
        {
            if (CanUpCast(context, source, tbt))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the source type can be widened to the target type; like int to long.
    /// </summary>
    protected virtual bool CanWiden(ExpressionContext context, TypeSymbol source, TypeSymbol target)
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
                || CanWiden(context, source, context.Symbols.Int64);
        }
        else if (target == context.Symbols.Single)
        {
            return CanWiden(context, source, context.Symbols.Int64);
        }
        else if (target == context.Symbols.Decimal)
        {
            return CanWiden(context, source, context.Symbols.Double);
        }
        return false;
    }

    /// <summary>
    /// Returns true if the source type is assignable to the target type without conversion.
    /// </summary>
    protected virtual bool IsAssignableWithoutConversion(ExpressionContext context, TypeSymbol sourceType, TypeSymbol targetType)
    {
        if (sourceType == targetType)
            return true;

        if (sourceType == SpecialSymbols.DoesNotReturn)
            return true;

        if (targetType == SpecialSymbols.Any)
            return true;

        if (CanDownCast(context, sourceType, targetType))
            return true;

        return false;
    }

    /// <summary>
    /// Gets all candidates for custom conversion
    /// </summary>
    protected virtual void GetConversionOperatorCandidates(
        ExpressionContext context,
        TypeSymbol source,
        TypeSymbol target,
        List<Symbol> operators,
        bool includeExplicit = false)
    {
        GetMatchingTypeMembers(source, "op_Implicit", s => IsMatchingConversionOperator(context, s, target, source), operators);
        GetMatchingTypeMembers(target, "op_Implicit", s => IsMatchingConversionOperator(context, s, target, source), operators);

        if (includeExplicit)
        {
            GetMatchingTypeMembers(source, "op_Explicit", s => IsMatchingConversionOperator(context, s, target, source), operators);
            GetMatchingTypeMembers(target, "op_Explicit", s => IsMatchingConversionOperator(context, s, target, source), operators);
        }

        if (target == context.Symbols.Boolean)
        {
            GetMatchingTypeMembers(source, "op_True", s => IsMatchingConversionOperator(context, s, target, source), operators);
        }
    }

    /// <summary>
    /// Returns true if the custom conversion symbol can perform a conversion between the source type and the target type.
    /// </summary>
    protected virtual bool IsMatchingConversionOperator(
        ExpressionContext context,
        Symbol conversionSymbol,
        TypeSymbol source,
        TypeSymbol target)
    {
        return conversionSymbol switch
        {
            FunctionSymbol function =>
                function.ReturnType == target
                && function.Parameters.Count == 1
                && GetConversion(context, source, function.Parameters[0].ParameterType) != ConversionKind.None,
            MethodSymbol method =>
                method.IsStatic
                && method.ReturnType == target
                && method.Parameters.Count == 1
                && GetConversion(context, source, method.Parameters[0].ParameterType) != ConversionKind.None,
            _ => false
        };
    }

    /// <summary>
    /// Determines the best custom conversion symbol from a set of candidate conversion symbols.
    /// </summary>
    protected virtual Symbol? GetBestConversionOperator(
        ExpressionContext context,
        TypeSymbol sourceType,
        TypeSymbol targetType,
        IReadOnlyList<Symbol> candidates)
    {
        // todo: do better
        return candidates.FirstOrDefault();
    }
    #endregion

    #region Default Expression
    /// <summary>
    /// Binds <see cref="DefaultExpression"/>
    /// </summary>
    protected virtual Expression BindDefault(ExpressionContext context, DefaultExpression dex)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            Expression? typeExpr = null;
            TypeSymbol? resultType = null;

            if (dex.TypeExpression != null)
            {
                typeExpr = dex.TypeExpression != null ? BindExpression(context.WithTargetType(null), dex.TypeExpression) : null;
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
    #endregion

    #region Label Expression
    /// <summary>
    /// Binds <see cref="LabelExpression"/>
    /// </summary>
    protected virtual Expression BindLabel(ExpressionContext context, LabelExpression label)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var receivingType = label.ReceivingType != null 
                ? BindExpression(context, label.ReceivingType)
                : null;

            var resultType = receivingType != null 
                ? receivingType.ReferencedSymbol as TypeSymbol 
                : SpecialSymbols.Void;

            var targetSymbol = context.GetLabelSymbol(label) 
                ?? new LabelSymbol(label.Name, resultType); // not found, must not have been inside a block

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
    #endregion

    #region Lambda Expression
    /// <summary>
    /// Binds <see cref="LambdaExpression"/>
    /// </summary>
    protected virtual Expression BindLambda(ExpressionContext context, LambdaExpression lambda)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var types = _typeListPool.AllocateFromPool();
        try
        {
            FunctionSymbol? lambdaSymbol = lambda.FunctionSymbol;
            LabelSymbol? returnLabel = lambda.ReturnLabel
                ?? new LabelSymbol(LabelSymbol.ReturnLabelName, SpecialSymbols.Any);
            ImmutableList<ParameterDeclaration> parameters = lambda.Parameters;
            Expression body = lambda.Body;
            TypeSymbol? returnType = null;

            BindLambdaSymbol(context);

            if (returnLabel.Type != returnType)
            {
                context = context.WithRebind(true);
                returnLabel = new LabelSymbol(LabelSymbol.ReturnLabelName, returnType);
                BindLambdaSymbol(context);
            }

            if (parameters == lambda.Parameters
                && body == lambda.Body
                && lambda.FunctionSymbol != null
                && lambda.FunctionSymbol.ReturnType == body.ResultType
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
                returnLabel,
                diagnostics.ToImmutableList());

            void BindLambdaSymbol(ExpressionContext context)
            {
                // bind and evalute new function symbol at the same time
                lambdaSymbol = new FunctionSymbol(
                    lambda.Name,
                    null,
                    me =>
                    {
                        var pms = CreateParameterSymbols(me, context);
                        BindBodyAndReturnType(pms, context);
                        return pms;
                    },
                    () => returnType!);
                // force eval of deferred parameters and return type
                // for side-effect assignment to locals  (Erik Meijer said it was okay)
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
                    var type = p.ParameterType != null ? BindExpression(context, p.ParameterType) : null;
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
                        .AddSymbol(returnLabel));
                body = BindExpression(bodyContext, lambda.Body);
                returnType = GetLambdaReturnType(context, body, returnLabel, diagnostics);
            }
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _typeListPool.ReturnToPool(types);
        }
    }

    /// <summary>
    /// Determines the return type of a <see cref="LambdaExpression"/>
    /// </summary>
    protected virtual TypeSymbol GetLambdaReturnType(
        ExpressionContext context,
        Expression body,
        LabelSymbol returnTarget,
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
    #endregion

    #region Loop Expression
    /// <summary>
    /// Bind <see cref="LoopExpression"/>
    /// </summary>
    protected virtual Expression BindLoop(ExpressionContext context, LoopExpression loop)
    {
        var diagostics = _diagnosticListPool.AllocateFromPool();
        var types = _typeListPool.AllocateFromPool();
        try
        {
            var breakTarget = loop.BreakTarget ?? new LabelSymbol(LabelSymbol.BreakLabelName, SpecialSymbols.Any);
            var continueTarget = loop.ContinueTarget ?? new LabelSymbol(LabelSymbol.ContinueLabelName, SpecialSymbols.Void);

            var bodyContext = context.WithScope(
                context.Scope.AddSymbols(new[] { breakTarget, continueTarget }));

            var body = BindExpression(bodyContext, loop.Body);

            // result type is the common type of all the break branches.
            GetBranchExpressionTypes(body, breakTarget, types);
            var resultType = GetBestCommonType(context, types, voidIsBetter: false) ?? SpecialSymbols.Void;

            if (breakTarget.Type != resultType)
            {
                breakTarget = new LabelSymbol(breakTarget.Name, resultType);
                bodyContext = context
                    .WithRebind(true)
                    .WithScope(context.Scope.AddSymbols(new[] { breakTarget, continueTarget }));
                body = BindExpression(bodyContext, loop.Body);
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
    #endregion

    #region Operator Expression
    /// <summary>
    /// Binds <see cref="OperatorExpression"/>
    /// </summary>
    protected virtual Expression BindOperator(ExpressionContext context, OperatorExpression opex)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            var arguments = BindExpressionList(context, opex.Arguments);

            GetCandidateOperators(context, opex.Kind, arguments, candidates);

            Symbol? operatorSymbol = null;

            if (candidates.Count == 0)
            {
                diagnostics.Add(BindingDiagnostics.NoOperatorDefined().WithLocation(opex.Location));
            }
            else
            {
                operatorSymbol = GetBestOperatorCandidate(context, arguments, candidates);
                if (operatorSymbol == null)
                {
                    diagnostics.Add(BindingDiagnostics.OperatorIsAmbiguous().WithLocation(opex.Location));
                }
                else
                {
                    var parameters = GetSymbolParameters(operatorSymbol);
                    if (parameters.Count != arguments.Count)
                    {
                        diagnostics.Add(BindingDiagnostics.IncorrectNumberOfOperands().WithLocation(opex.Location));
                    }
                    else
                    {
                        arguments = ConvertArguments(context, parameters, arguments);
                    }
                }
            }

            var resultType = operatorSymbol != null
                ? GetCalledSymbolReturnType(operatorSymbol)
                : null;

            if (arguments == opex.Arguments
                && operatorSymbol == opex.OperatorSymbol
                && resultType == opex.ResultType
                && diagnostics.Count == 0)
                return opex;

            return new OperatorExpression(
                opex.Kind,
                arguments,
                opex.Location,
                operatorSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _symbolListPool.ReturnToPool(candidates);
        }
    }

    protected virtual void GetCandidateOperators(ExpressionContext context, string operatorKind, ImmutableList<Expression> arguments, List<Symbol> candidates)
    {
        // 
        var ops = Operators.From(context.Symbols);
        candidates.AddRange(ops.GetIntrinsicOperators(operatorKind));

        var opName = GetOperatorName(operatorKind);
        foreach (var arg in arguments)
        {
            GetMatchingTypeMembers(arg.ResultType, opName, s => s is MemberSymbol ms && ms.IsStatic, candidates);
        }
    }

    protected virtual Symbol? GetBestOperatorCandidate(ExpressionContext context, ImmutableList<Expression> arguments, List<Symbol> candidates)
    {
        // todo: be better
        return candidates.FirstOrDefault(c => MatchesParameters(context, c, arguments));
    }

    private static string? GetOperatorName(string opKind)
    {
        return opKind switch
        {
            OperatorKind.Add => "op_Addition",
            OperatorKind.Subtract => "op_Subtraction",
            OperatorKind.Multiply => "op_Multiply",
            OperatorKind.Divide => "op_Division",
            OperatorKind.Remainder => "op_Modulus",
            OperatorKind.Negate => "op_Negation",
            OperatorKind.UnaryPlus => "op_UnaryPlus",
            OperatorKind.ShiftLeft => "op_ShiftLeft",
            OperatorKind.ShiftRight => "op_ShiftRight",
            OperatorKind.BitwiseAnd => "op_BitwiseAnd",
            OperatorKind.BitwiseOr => "op_BitwiseOr",
            OperatorKind.BitwiseXor => "op_ExclusiveOr",
            OperatorKind.BitwiseNot => "op_OnesCompliment",
            OperatorKind.LogicalAnd => "op_BitwiseAnd",
            OperatorKind.LogicalOr => "op_BitwiseOr",
            OperatorKind.LogicalNot => "op_LogicalNot",
            OperatorKind.LogicalXor => "op_ExclusiveOr",
            OperatorKind.Equal => "op_Equality",
            OperatorKind.NotEqual => "op_Inequality",
            OperatorKind.LessThan => "op_LessThan",
            OperatorKind.LessThanOrEqual => "op_LessThanOrEqual",
            OperatorKind.GreaterThan => "op_GreaterThan",
            OperatorKind.GreaterThanOrEqual => "op_GreaterThanOrEqual",
            OperatorKind.True => "op_True",
            OperatorKind.False => "op_False",
            OperatorKind.Increment => "op_Increment",
            OperatorKind.Decrement => "op_Decrement",
            _ => null
        };
    }

#endregion

    #region Member Expression
    /// <summary>
    /// Binds <see cref="MemberExpression"/>
    /// </summary>
    protected virtual Expression BindMember(ExpressionContext context, MemberExpression member)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(context.WithTargetType(null), member.Instance);

            if (expression.ResultType is GroupSymbol)
                diagnostics.Add(BindingDiagnostics.UnknownName(member.Name).WithLocation(member.Location));

            var referencedSymbol = GetReferencedMember(context, expression, member.Name, diagnostics);

            var resultType = GetReferenceResultType(context, referencedSymbol);

            if (member.Instance == expression
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
        ExpressionContext context,
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

            return context.Symbols.GetGroup(members);
        }
        finally
        {
            _symbolListPool.ReturnToPool(members);
        }
    }
    #endregion

    #region NameReference Expression
    /// <summary>
    /// Binds <see cref="NameReferenceExpression"/>
    /// </summary>
    protected virtual Expression BindNameReference(ExpressionContext context, NameReferenceExpression nameRef)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var referencedSymbol = GetNameReference(context, nameRef.Name);

            if (referencedSymbol != null && referencedSymbol == nameRef.ReferencedSymbol)
                return nameRef;

            if (referencedSymbol == null)
                diagnostics.Add(BindingDiagnostics.UnknownName(nameRef.Name).WithLocation(nameRef.Location));

            var resultType = GetReferenceResultType(context, referencedSymbol) ?? context.Symbols.Object;

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
    protected virtual Symbol? GetNameReference(ExpressionContext context, string name)
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
    #endregion

    #region New Expression
    /// <summary>
    /// Binds <see cref="NewExpression"/>
    /// </summary>
    protected virtual Expression BindNew(ExpressionContext context, NewExpression nex)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var argContext = context.WithTargetType(null);

            var typeExpression = nex.TypeExpression != null ? BindExpression(argContext, nex.TypeExpression) : null;
            var arguments = BindExpressionList(argContext, nex.Arguments);
            var referencedType = (typeExpression?.ReferencedSymbol ?? context.TargetType) as TypeSymbol;

            if (referencedType != null)
                GetConstructorCandidates(context, referencedType, arguments, candidates);

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
                    arguments = ConvertArguments(context, constructorSymbol.Parameters, arguments);
                }
            }

            var resultType = constructorSymbol?.ConstructedType;

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
        ExpressionContext context,
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
                .Where(c => !c.IsStatic && MatchesParameters(context, c, arguments))
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
    #endregion

    #region NewArraySize Expression
    /// <summary>
    /// Binds <see cref="NewArraySizeExpression"/>
    /// </summary>
    protected virtual Expression BindNewArraySize(ExpressionContext context, NewArraySizeExpression newArraySize)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var targetType = context.TargetType;
            context = context.WithTargetType(null);
            var elementType = newArraySize.ElementType != null ? BindExpression(context, newArraySize.ElementType) : null;
            var size = BindExpression(context, newArraySize.Size);

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
    #endregion

    #region NewArrayInit Expression
    /// <summary>
    /// Binds <see cref="NewArrayInitExpression"/>
    /// </summary>
    protected virtual Expression BindNewArrayInit(ExpressionContext context, NewArrayInitExpression newArrayInit)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var targetType = context.TargetType;
            context = context.WithTargetType(null);
            var elementType = newArrayInit.ElementType != null ? BindExpression(context, newArrayInit.ElementType) : null;
            var expressions = BindExpressionList(context, newArrayInit.Expressions);

            var targetElementType = targetType is ArraySymbol asym ? asym.ElementType : null;
            var elementTypeSymbol = elementType?.ReferencedSymbol as TypeSymbol
                ?? targetElementType
                ?? GetBestCommonResultType(context, expressions);
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
    #endregion

    #region SymbolReference Expression
    /// <summary>
    /// Binds <see cref="SymbolReferenceExpression"/>
    /// </summary>
    protected virtual Expression BindSymbolReference(ExpressionContext context, SymbolReferenceExpression symbolRef)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var referencedSymbol = GetSymbolReference(context, symbolRef.FullName);

            if (referencedSymbol != null && referencedSymbol == symbolRef.ReferencedSymbol)
                return symbolRef;

            if (referencedSymbol == null)
                diagnostics.Add(BindingDiagnostics.UnknownName(symbolRef.FullName).WithLocation(symbolRef.Location));

            var resultType = GetReferenceResultType(context, referencedSymbol) ?? context.Symbols.Object;

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
    protected virtual Symbol? GetSymbolReference(ExpressionContext context, string fullName)
    {
        return context.Symbols.GetSymbol<Symbol>(fullName);
    }

    /// <summary>
    /// Determines the result type of a referenced <see cref="Symbol"/>.
    /// </summary>
    protected virtual TypeSymbol? GetReferenceResultType(ExpressionContext context, Symbol? referencedSymbol) =>
        referencedSymbol switch
        {
            VariableSymbol v => v.VariableType,
            ParameterSymbol p => p.ParameterType,
            FieldSymbol f => f.FieldType,
            PropertySymbol p => p.PropertyType,
            FunctionSymbol f => f,
            GroupSymbol g => g,
            MethodSymbol => SpecialSymbols.Void,
            TypeSymbol => context.Symbols.Type,
            NamespaceSymbol => context.Symbols.Namespace,
            _ => null
        };
    #endregion

    #region TypeArguments Expression
    /// <summary>
    /// Binds <see cref="TypeArgumentsExpression"/>,
    /// agumenting filtering referencdd symbols by the corresponding arity
    /// and applying the specified type arguments.
    /// </summary>
    protected virtual Expression BindTypeArguments(ExpressionContext context, TypeArgumentsExpression construct)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(context, construct.Expression);
            var typeArguments = BindExpressionList(context, construct.TypeArguments);

            Symbol? constructedSymbol = null;
            if (expression.ReferencedSymbol is Symbol symbol)
            {
                var typeArgs = typeArguments
                    .Select(ta => GetType(context, ta))
                    .OfType<TypeSymbol>()
                    .ToImmutableList();

                constructedSymbol = AppyTypeArguments(context, symbol, typeArgs, construct.Location, diagnostics);
            }

            var resultType = GetReferenceResultType(context, constructedSymbol);

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
    protected virtual Symbol? AppyTypeArguments(
        ExpressionContext context,
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
                .Select(s => AppyTypeArguments(context, s, typeArguments))
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
    #endregion

    #region Variable Expression
    /// <summary>
    /// Binds <see cref="VariableExpression"/>
    /// </summary>
    protected virtual Expression BindVariable(ExpressionContext context, VariableExpression declaration)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            Expression? variableType = null;
            TypeSymbol? vtype = null;
            Expression? initializer = null;

            if (declaration.VariableType != null)
            {
                variableType = BindExpression(context.WithTargetType(null), declaration.VariableType);
                vtype = variableType?.ReferencedSymbol as TypeSymbol ?? context.Symbols.Object;
                if (declaration.Initializer != null)
                {
                    initializer = BindExpression(context.WithTargetType(vtype), declaration.Initializer);
                    initializer = ConvertTo(context, initializer, vtype);
                }
            }
            else if (declaration.Initializer != null)
            {
                // target type can carry through declaration
                initializer = BindExpression(context, declaration.Initializer);
                vtype = initializer.ResultType;
            }
            else
            {
                diagnostics.Add(BindingDiagnostics.DeclarationMustHaveTypeOrInitializer().WithLocation(declaration.Location));
                vtype = context.Symbols.Object;
            }

            if (variableType == declaration.VariableType
                && initializer == declaration.Initializer
                && declaration.VariableSymbol != null
                && declaration.VariableSymbol.VariableType == vtype
                && declaration.ResultType == vtype)
            {
                return declaration;
            }

            var variable = declaration.VariableSymbol != null
                && declaration.VariableSymbol.VariableType == vtype
                ? declaration.VariableSymbol
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
    #endregion

    #region Void Expression
    /// <summary>
    /// Binds <see cref="VoidExpression"/>
    /// </summary>
    protected virtual VoidExpression BindVoid(ExpressionContext context, VoidExpression vex)
    {
        // nothing to actually bind
        return vex;
    }
    #endregion

#endregion

    #region Misc
    /// <summary>
    /// Gets the matching members of a type
    /// </summary>
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

    /// <summary>
    /// Gets the best common result type from a set of expressions.
    /// </summary>
    protected TypeSymbol? GetBestCommonResultType(ExpressionContext context, IReadOnlyList<Expression> expressions, bool voidIsBetter = false)
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

    /// <summary>
    /// Gets the best common type from a set of types, or null if no best common type can be determined.
    /// </summary>
    protected TypeSymbol? GetBestCommonType(ExpressionContext context, params TypeSymbol?[] types) =>
        GetBestCommonType(context, (IReadOnlyList<TypeSymbol?>)types);

    /// <summary>
    /// Gets the best common tyep from a set of types, or null if no best common type can be determined.
    /// </summary>
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
            if (GetConversion(context, type, best) == ConversionKind.None
                && GetConversion(context, best, type) != ConversionKind.None)
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
        public override GlobalNamespaceSymbol ExternalSymbols { get; }

        /// <summary>
        /// The namespace including only declared symbols
        /// </summary>
        public override GlobalNamespaceSymbol DeclarationSymbols { get; }

        /// <summary>
        /// The namespace including both declared and external symbol.
        /// </summary>
        public override GlobalNamespaceSymbol GlobalNamespace => _context.GlobalNamespace;

        public DeferredBinding(
            SemanticBinder binder,
            DeclarationContext context,
            GlobalNamespaceSymbol externalSymbols,
            GlobalNamespaceSymbol declarationSymbols,
            ImmutableList<Declaration> unboundDeclarations,
            ImmutableDictionary<Symbol, ImmutableList<Declaration>> symbolToUnboundDeclarationMap)
        {
            _binder = binder;
            _context = context;
            ExternalSymbols = externalSymbols;
            DeclarationSymbols = declarationSymbols;
            UnboundDeclarations = unboundDeclarations;
            _symbolToUnboundDeclarationMap = symbolToUnboundDeclarationMap;
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
                var tmp = _binder.BindDeclaration(_context, unboundDeclaration);
                boundDeclaration = ImmutableInterlocked.GetOrAdd(ref _unboundToBoundMap, unboundDeclaration, tmp);
            }

            return boundDeclaration;
        }

        public readonly ImmutableDictionary<Symbol, ImmutableList<Declaration>> _symbolToUnboundDeclarationMap;

        public ImmutableDictionary<Symbol, ImmutableList<Declaration>> _symbolToBoundDeclarationMap =
            ImmutableDictionary<Symbol, ImmutableList<Declaration>>.Empty;

        /// <summary>
        /// Get the bound declarations associated with a declared symbol.
        /// </summary>
        public override ImmutableList<Declaration> GetBoundSymbolDeclarations(Symbol symbol)
        {
            if (!_symbolToBoundDeclarationMap.TryGetValue(symbol, out var boundDecls))
            {
                if (_symbolToUnboundDeclarationMap.TryGetValue(symbol, out var unboundDecls))
                {
                    var tmp = unboundDecls.Select(u => GetBoundDeclaration(u)!).ToImmutableList();
                    boundDecls = ImmutableInterlocked.GetOrAdd(ref _symbolToBoundDeclarationMap, symbol, tmp);
                }
            }

            return boundDecls ?? ImmutableList<Declaration>.Empty;
        }

        private ImmutableList<Diagnostic>? _diagnostics;

        public override ImmutableList<Diagnostic> Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    var dxs = new List<Diagnostic>();
                    
                    foreach (var bd in this.BoundDeclarations)
                    {
                        bd.GetContainedDiagnostics(dxs);
                    }

                    Interlocked.CompareExchange(ref _diagnostics, dxs.ToImmutableList(), null);
                }

                return _diagnostics;
            }
        }
    }
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
