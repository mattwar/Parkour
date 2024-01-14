namespace Parkour.Binding;

using Semantics;
using Symbols;

public class DeclarationBinder
{
    private ExpressionBinder? _binder;
    private BindingScope _scope;

    public DeclarationBinder()
    {
    }

    public DeclarationBinding Bind(
        IEnumerable<Declaration> declarations,
        NamespaceSymbol importsNamespace)
    {
        NamespaceSymbol? declarationNamespace = null;

        // create combined global namespace and construct declaration symbols
        var combinedGlobalNamespace = CombinedSymbols.CreateCombinedGlobalNamespace(
            globals =>
            {
                declarationNamespace = new NamespaceSymbol("", null,
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

                return [importsNamespace, declarationNamespace];
            });

        //_globals = globals;
        _scope = new BindingScope().AddSymbolMembers(combinedGlobalNamespace);
        _binder = new ExpressionBinder(combinedGlobalNamespace);

        // force evaluation of declarationNamespace
        var globalMembers = combinedGlobalNamespace.Members;
        var declarationNamespaceMembers = declarationNamespace!.Members;

        // resolve all declared symbols which creates symbol<->declaration maps
        declarationNamespace.Walk(s => { });

        return new DeferredBinding(
            this,
            _scope,
            combinedGlobalNamespace,
            declarations.ToImmutableList());
    }

    #region Deferred Binding
    private class DeferredBinding : DeclarationBinding
    {
        public override ImmutableList<Declaration> UnboundDeclarations { get; }
        public override NamespaceSymbol CombindedSymbols { get; }

        private readonly DeclarationBinder _binder;
        private readonly BindingScope _scope;

        public DeferredBinding(
            DeclarationBinder binder,
            BindingScope scope,
            NamespaceSymbol combinedSymbols,
            ImmutableList<Declaration> unboundDeclarations)
        {
            _binder = binder;
            _scope = scope;
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
        _symbolToDeclsMap[symbol] = ImmutableList.Create(declaration);
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
                    var newScope = scope.AddSymbolMembers((Symbol)me).AddSymbol(me);
                    return CombineMembers(me, g.SelectMany(n => n.Declarations), newScope);
                }))
            .ToList();

        newMembers.AddRange(newMemberNamespaces);

        var otherMembers = members
            .Where(d => !(d is NamespaceDeclaration))
            .ToList();

        var otherMemberSymbols =
            otherMembers
            .Select(d => CreateSymbol(null, d, scope))
            .OfType<Symbol>()
            .ToList();

        newMembers.AddRange(otherMemberSymbols);

        return newMembers.ToImmutableList();
    }

    private Symbol? CreateSymbol(
        MemberSymbol? container,
        Declaration declaration,
        BindingScope scope)
    {
        Symbol? symbol = null;

        switch (declaration)
        {
            case ClassDeclaration cd:
                symbol = new TypeSymbol(
                    cd.Name,
                    container,
                    cd.Access,
                    cd.Modifiers,
                    fnTypeParameters: null,
                    () => cd.BaseTypes.Select(bt => GetType(bt)).ToImmutableList()!,
                    me => cd.Declarations.Select(d => CreateSymbol(me, d, scope)).Where(s => s != null).ToImmutableList()!,
                    genericDefinition: null,
                    runtimeType: null);
                break;

            case MethodDeclaration md:
                symbol = new MethodSymbol(
                    md.Name,
                    container,
                    md.Access,
                    md.Modifiers,
                    fnTypeParameters: null,
                    me =>
                    {
                        return md.Parameters
                            .Select(p => (ParameterSymbol)CreateSymbol(me, p, scope)!)
                            .ToImmutableList()!;
                    },
                    () => GetType(md.ReturnType),
                    fnGenericDefinition: null,
                    runtimeMethod: null
                    );
                break;

            case ParameterDeclaration pd:
                symbol = new ParameterSymbol(
                    pd.Name,
                    container,
                    () => pd.ParameterType != null ? GetType(pd.ParameterType) : SpecialSymbols.Any,
                    runtimeParameter: null
                    );
                break;

            case FieldDeclaration fd:
                symbol = new FieldSymbol(
                    fd.Name,
                    container as TypeSymbol,
                    fd.Access,
                    fd.Modifiers,
                    () => GetType(fd.FieldType),
                    runtimeField: null
                    );
                break;

            case PropertyDeclaration pd:
                symbol = new PropertySymbol(
                    pd.Name,
                    container as TypeSymbol,
                    pd.Access,
                    pd.Modifiers,
                    () => GetType(pd.PropertyType),
                    pd.BackingField != null 
                        ? me => (FieldSymbol)CreateSymbol(me, pd.BackingField, scope)!
                        : null,
                    me => (MethodSymbol)CreateSymbol(me, pd.GetMethod, scope)!,
                    pd.SetMethod != null 
                        ? me => (MethodSymbol)CreateSymbol(me, pd.SetMethod, scope)!
                        : null,
                    runtimeProperty: null
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
        _binder!.BindType(typeExpression, null) ?? SpecialSymbols.Unknown;

    private Expression BindExpression(Expression expr, BindingScope scope)
    {
        return _binder!.BindInScope(expr, scope);
    }

    private ImmutableList<TExpression> BindExpressions<TExpression>(ImmutableList<TExpression> list, BindingScope scope)
        where TExpression : Expression
    {
        return _binder!.BindListInScope(list, scope);
    }

    /// <summary>
    /// Binds declarations and related expressions
    /// </summary>
    internal virtual Declaration BindDeclaration(Declaration declaration, BindingScope scope)
    {
        var symbol = _declToSymbolMap[declaration];

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
            default:
                break;
        }

        return declaration;
    }

    protected virtual ImmutableList<TDeclaration> BindDeclarations<TDeclaration>(ImmutableList<TDeclaration> list, BindingScope scope)
        where TDeclaration : Declaration
    {
        List<TDeclaration>? newList = null;

        for (int i = 0; i < list.Count; i++)
        {
            var decl = list[i];

            var bound = (TDeclaration)BindDeclaration(decl, scope);
            if (bound != decl || newList != null)
            {
                if (newList == null)
                    newList = [.. list.Take(i)];
                newList.Add(bound);
            }
        }

        return newList != null
            ? newList.ToImmutableList()
            : list;
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

    protected virtual MethodDeclaration BindMethod(MethodDeclaration md, MethodSymbol symbol, BindingScope scope)
    {
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
            parameters,
            body,
            returnType,
            md.Location,
            symbol,
            md.Diagnostics
            );
    }

    private ClassDeclaration BindClass(ClassDeclaration cd, TypeSymbol symbol, BindingScope scope)
    {
        var baseTypes = BindExpressions(cd.BaseTypes, scope);

        // add all class members to scope
        var bodyScope = scope
            .AddSymbolMembers(symbol)
            .AddSymbol(symbol);

        var declarations = BindDeclarations(cd.Declarations, bodyScope);

        return new ClassDeclaration(
            cd.Name,
            cd.Access,
            cd.Modifiers,
            baseTypes,
            declarations,
            cd.Location,
            symbol,
            cd.Diagnostics
            );
    }

    private NamespaceDeclaration BindNamespace(NamespaceDeclaration nd, NamespaceSymbol symbol, BindingScope scope)
    {
        var bodyScope = scope
            .AddSymbolMembers(symbol)
            .AddSymbol(symbol);

        var declarations = BindDeclarations(nd.Declarations, bodyScope);

        return new NamespaceDeclaration(
            nd.Name,
            declarations,
            nd.Location,
            symbol,
            nd.Diagnostics
            );
    }
}
