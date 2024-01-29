namespace Parkour.Binding;

using Semantics;
using Symbols;

public class DeclarationBinder
{
    private ExpressionBinder? _binder;
    private BindingScope _scope;

    public DeclarationBinder()
    {
        _scope = BindingScope.Default;
    }

    public DeclarationBinding Bind(
        IEnumerable<Declaration> declarations,
        NamespaceSymbol externalSymbols)
    {
        NamespaceSymbol? declarationSymbols = null;

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

                    Map(globalNamespaces, me);

                    var globalNamespaceMembers = declarations.SelectMany(d =>
                        d is NamespaceDeclaration nd && nd.Name == ""
                            ? (IEnumerable<Declaration>)nd.Declarations
                            : new[] { d })
                        .ToImmutableList();

                    return CombineMembers(me, globalNamespaceMembers, _scope);
                });

                return [externalSymbols, declarationSymbols];
            });

        //_globals = globals;
        _scope = BindingScope.Default.AddMembers(combinedSymbols);
        _binder = new ExpressionBinder(combinedSymbols);

        // force evaluation of declarationNamespace
        var globalMembers = combinedSymbols.Members;
        var declarationNamespaceMembers = declarationSymbols!.Members;

        // resolve all declared symbols which creates symbol<->declaration maps
        declarationSymbols.WalkDeclarations(s => { });

        return new DeferredBinding(
            this,
            _scope,
            externalSymbols,
            declarationSymbols,
            combinedSymbols,
            declarations.ToImmutableList());
    }

    #region Deferred Binding
    private class DeferredBinding : DeclarationBinding
    {
        public override ImmutableList<Declaration> UnboundDeclarations { get; }
        public override NamespaceSymbol ExternalSymbols { get; }
        public override NamespaceSymbol DeclarationSymbols { get; }
        public override NamespaceSymbol CombindedSymbols { get; }

        private readonly DeclarationBinder _binder;
        private readonly BindingScope _scope;

        public DeferredBinding(
            DeclarationBinder binder,
            BindingScope scope,
            NamespaceSymbol externalSymbols,
            NamespaceSymbol declarationSymbols,
            NamespaceSymbol combinedSymbols,
            ImmutableList<Declaration> unboundDeclarations)
        {
            _binder = binder;
            _scope = scope;
            ExternalSymbols = externalSymbols;
            DeclarationSymbols = declarationSymbols;
            CombindedSymbols = combinedSymbols;
            UnboundDeclarations = unboundDeclarations;
        }

        private ImmutableList<Declaration>? _boundDeclarations;

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
                var tmp = _binder.BindDeclaration(unboundDeclaration, _scope);
                boundDeclaration = ImmutableInterlocked.GetOrAdd(ref _unboundToBoundMap, unboundDeclaration, tmp);
            }

            return boundDeclaration;
        }
    }
    #endregion

    private Dictionary<Declaration, Symbol> _declToSymbolMap = 
        new Dictionary<Declaration, Symbol>();

    private Dictionary<Symbol, ImmutableList<Declaration>> _symbolToDeclsMap =
        new Dictionary<Symbol, ImmutableList<Declaration>>();

    private void Map(Declaration declaration, Symbol symbol)
    {
        _declToSymbolMap[declaration] = symbol;
        _symbolToDeclsMap[symbol] = [declaration];
    }

    private void Map(IEnumerable<Declaration> declarations, Symbol symbol)
    {
        foreach (var decl in declarations)
        {
            _declToSymbolMap[decl] = symbol;
        }

        _symbolToDeclsMap[symbol] = declarations.ToImmutableList();
    }

    /// <summary>
    /// returns the symbols for the declarations, 
    /// with same named namespace declarations combined into a single namespace symbol.
    /// </summary>
    private ImmutableList<Symbol> CombineMembers(
        NamespaceSymbol container,
        IEnumerable<Declaration> members,
        BindingScope scope)
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
                    Map(g, me);
                    var newScope = scope.AddMembers(me).AddSymbol(me);
                    return CombineMembers(me, g.SelectMany(n => n.Declarations), newScope);
                }))
            .ToList();

        newMembers.AddRange(newMemberNamespaces);

        var otherMembers = members
            .Where(d => !(d is NamespaceDeclaration))
            .ToList();

        var otherMemberSymbols =
            otherMembers
            .Select(d => CreateDeclarationSymbol(null, d, scope))
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
        BindingScope scope)
    {
        Symbol? symbol = null;

        switch (declaration)
        {
            case TypeParameterDeclaration tp:
                symbol = new TypeParameterSymbol(
                    tp.Name,
                    runtimeType: null);
                break;

            case ClassDeclaration cd:
                symbol = new TypeSymbol(
                    cd.Name,
                    declaringSymbol,
                    cd.Access,
                    cd.Modifiers,
                    me => cd.TypeParameters.Select(tp => (TypeParameterSymbol)CreateDeclarationSymbol(me, tp, scope)!).ToImmutableList()!,
                    () => ImmutableList<TypeSymbol>.Empty,
                    () => cd.BaseTypes.Select(bt => GetType(bt)).ToImmutableList()!,
                    me => cd.Declarations.Select(d => CreateDeclarationSymbol(me, d, scope)).Where(s => s != null).ToImmutableList()!,
                    constructedFrom: null,
                    runtimeType: null);
                break;

            case MethodDeclaration md:
                symbol = new MethodSymbol(
                    md.Name,
                    declaringSymbol,
                    md.Access,
                    md.Modifiers,
                    me => ImmutableList<TypeParameterSymbol>.Empty,
                    () => ImmutableList<TypeSymbol>.Empty,
                    me => md.Parameters.Select(p => (ParameterSymbol)CreateDeclarationSymbol(me, p, scope)!).ToImmutableList()!,
                    () => GetType(md.ReturnType),
                    constructedFrom: null,
                    runtimeInfo: null
                    );
                break;

            case ParameterDeclaration pd:
                symbol = new ParameterSymbol(
                    pd.Name,
                    declaringSymbol,
                    () => pd.ParameterType != null ? GetType(pd.ParameterType) : SpecialSymbols.Any,
                    runtimeParameter: null
                    );
                break;

            case FieldDeclaration fd:
                symbol = new FieldSymbol(
                    fd.Name,
                    declaringSymbol as TypeSymbol,
                    fd.Access,
                    fd.Modifiers,
                    () => GetType(fd.FieldType),
                    runtimeInfo: null
                    );
                break;

            case PropertyDeclaration pd:
                symbol = new PropertySymbol(
                    pd.Name,
                    declaringSymbol as TypeSymbol,
                    pd.Access,
                    pd.Modifiers,
                    () => GetType(pd.PropertyType),
                    pd.BackingField != null 
                        ? me => (FieldSymbol)CreateDeclarationSymbol(me, pd.BackingField, scope)!
                        : null,
                    me => (MethodSymbol)CreateDeclarationSymbol(me, pd.GetMethod, scope)!,
                    pd.SetMethod != null 
                        ? me => (MethodSymbol)CreateDeclarationSymbol(me, pd.SetMethod, scope)!
                        : null,
                    runtimeInfo: null
                );
                break;
        }

        if (symbol != null)
        {
            Map(declaration, symbol);
        }

        return symbol;
    }

    private TypeSymbol GetType(Expression typeExpression) =>
        _binder!.GetType(typeExpression) ?? SpecialSymbols.Unknown;

    protected virtual Expression BindExpression(Expression expr, BindingScope scope)
    {
        return _binder!.Bind(expr, scope);
    }

    protected virtual ImmutableList<TExpression> BindExpressions<TExpression>(ImmutableList<TExpression> list, BindingScope scope)
        where TExpression : Expression
    {
        return _binder!.BindList(list, scope);
    }

    /// <summary>
    /// Binds declarations and related expressions
    /// </summary>
    protected virtual Declaration BindDeclaration(Declaration declaration, BindingScope scope)
    {
        if (_declToSymbolMap.TryGetValue(declaration, out var symbol))
        {
            switch (declaration)
            {
                case FieldDeclaration fd:
                    return BindField(fd, (FieldSymbol)symbol, scope);
                case PropertyDeclaration pd:
                    return BindProperty(pd, (PropertySymbol)symbol, scope);
                case ParameterDeclaration prd:
                    return BindParameter(prd, (ParameterSymbol)symbol, scope);
                case MethodDeclaration md:
                    return BindMethod(md, (MethodSymbol)symbol, scope);
                case ClassDeclaration cld:
                    return BindClass(cld, (TypeSymbol)symbol, scope);
                case NamespaceDeclaration nd:
                    return BindNamespace(nd, (NamespaceSymbol)symbol, scope);
                case TypeParameterDeclaration tp:
                    return BindTypeParameter(tp, (TypeParameterSymbol)symbol, scope);
                default:
                    throw new InvalidCastException($"Unhandled declaration '{declaration.GetType().Name}' in {nameof(DeclarationBinder)}.{nameof(BindDeclaration)}");
            }
        }
        else
        {
            switch (declaration)
            {
                case UsingDeclaration ud:
                    return BindUsing(ud, scope);
                default:
                    throw new InvalidCastException($"Unhandled declaration '{declaration.GetType().Name}' in {nameof(DeclarationBinder)}.{nameof(BindDeclaration)}");
            }
        }
    }

    protected virtual ImmutableList<TDeclaration> BindDeclarations<TDeclaration>(ImmutableList<TDeclaration> list, BindingScope scope)
        where TDeclaration : Declaration
    {
        return list.Rewrite(d => (TDeclaration)BindDeclaration(d, scope));
    }

    protected virtual FieldDeclaration BindField(FieldDeclaration fd, FieldSymbol symbol, BindingScope scope)
    {
        var fieldType = BindExpression(fd.FieldType, scope);
        var initializer = fd.Initializer != null
            ? BindExpression(fd.Initializer, scope)
            : null;
        return new FieldDeclaration(
            fd.Name,
            fd.Access,
            fd.Modifiers,
            fieldType,
            initializer,
            fd.Location,
            symbol,
            fd.Diagnostics
            );
    }

    protected virtual PropertyDeclaration BindProperty(PropertyDeclaration pd, PropertySymbol symbol, BindingScope scope)
    {
        var propertyType = BindExpression(pd.PropertyType, scope);

        var backingField = pd.BackingField != null 
            ? (FieldDeclaration)BindDeclaration(pd.BackingField, scope) 
            : null;

        // put property symbol in scope when binding accessor methods
        var methodScope = scope.AddSymbol(symbol);

        if (symbol.BackingField != null)
            methodScope = methodScope.AddSymbol(symbol.BackingField);

        var getMethod = (MethodDeclaration)BindDeclaration(pd.GetMethod, methodScope);

        var setMethod = pd.SetMethod != null 
            ? (MethodDeclaration)BindDeclaration(pd.SetMethod, methodScope) 
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
            symbol,
            pd.Diagnostics
            );
    }

    protected virtual ParameterDeclaration BindParameter(ParameterDeclaration pd, ParameterSymbol symbol, BindingScope scope)
    {
        var parameterType = pd.ParameterType != null
            ? BindExpression(pd.ParameterType, scope)
            : null;

        return new ParameterDeclaration(
            pd.Name,
            parameterType,
            pd.Location,
            symbol,
            pd.Diagnostics
            );
    }

    protected virtual MethodDeclaration BindMethod(
        MethodDeclaration md, 
        MethodSymbol symbol, 
        BindingScope scope)
    {
        var typeParameters = BindDeclarations(md.TypeParameters, scope);
        var parameters = BindDeclarations(md.Parameters, scope);
        var returnType = BindExpression(md.ReturnType, scope);

        var bodyScope = symbol.Parameters.Count > 0
            ? scope.AddSymbols(symbol.Parameters)
            : scope;

        var body = BindExpression(md.Body, bodyScope);

        return new MethodDeclaration(
            md.Name,
            md.Access,
            md.Modifiers,
            typeParameters,
            parameters,
            body,
            returnType,
            md.Location,
            symbol,
            md.Diagnostics
            );
    }

    protected virtual ClassDeclaration BindClass(ClassDeclaration cd, TypeSymbol symbol, BindingScope scope)
    {
        var typeParameters = BindDeclarations(cd.TypeParameters, scope);
        var baseTypes = BindExpressions(cd.BaseTypes, scope);

        // add all class members to scope
        var bodyScope = scope
            .AddMembers(symbol)
            .AddSymbol(symbol);

        var declarations = BindDeclarations(cd.Declarations, bodyScope);

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
            symbol,
            cd.Diagnostics
            );
    }

    protected virtual TypeParameterDeclaration BindTypeParameter(TypeParameterDeclaration tp, TypeParameterSymbol symbol, BindingScope scope)
    {
        if (tp.TypeParameterSymbol == symbol)
            return tp;

        return new TypeParameterDeclaration(
            tp.Name,
            tp.Location,
            symbol,
            tp.Diagnostics);
    }

    protected virtual NamespaceDeclaration BindNamespace(NamespaceDeclaration nd, NamespaceSymbol symbol, BindingScope scope)
    {
        var bodyScope = scope
            .AddMembers(symbol)
            .AddSymbol(symbol);

        var (declarations, finalScope) = nd.Declarations.Rewrite(bodyScope, (d, _scope) => 
        {            
            var nd = BindDeclaration(d, _scope);

            // handle using declarations
            if (nd is UsingDeclaration ud
                && ud.Expression.ReferencedSymbol != null)
            {
                if (ud.AliasedSymbol != null)
                {
                    _scope = _scope.AddSymbol(ud.AliasedSymbol);
                }
                else if (ud.Expression.ReferencedSymbol is NamespaceSymbol ns)
                {
                    _scope = _scope.AddSymbol(ns).AddMembers(ns);
                }
            }

            return (nd, _scope);
        });

        return new NamespaceDeclaration(
            nd.Name,
            declarations,
            nd.Location,
            symbol,
            nd.Diagnostics
            );
    }

    protected virtual UsingDeclaration BindUsing(UsingDeclaration ud, BindingScope scope)
    {
        var expression = BindExpression(ud.Expression, scope);

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
}
