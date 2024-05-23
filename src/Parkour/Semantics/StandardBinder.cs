using System.Diagnostics.CodeAnalysis;

namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Converts unbound declarations and expressions into bound declarations and expressions
/// with symbols and diagnostics assigned.
/// </summary>
public class StandardBinder : SemanticBinder
{
    /// <summary>
    /// Creates a new instance of <see cref="StandardBinder"/> that 
    /// converts unbound declarations and expressions into bound declarations and expressions,
    /// by creating new instances of each with declared or referenced symbols assigned
    /// and including any diagnostics.
    /// </summary>
    public StandardBinder()
    {
    }

    /// <summary>
    /// Binds declarations and expressions.
    /// </summary>
    /// <param name="elements">The elements to be bound.</param>
    /// <param name="imports">The imported symbols.</param>
    public override SemanticBinding Bind(        
        ImmutableList<SemanticElement> elements,
        SymbolTable imports)
    {
        var result = CreateSymbols(elements.OfType<Declaration>(), imports);
        var boundElements = BindList(result.Context.BindingContext, elements);

        return new SemanticBinding(
            boundElements,
            imports,
            result.CombinedSymbols
            );
    }

    /// <summary>
    /// Binds all unbound expressions given an initial binding scope
    /// </summary>
    public virtual SemanticBinding BindExpression(
        Expression expression,
        SymbolTable imports,
        Scope scope)
    {
        var context = new BindingContext(
            CreateInitialSymbolContext(imports), null, scope, null);
        var boundExpression = BindExpression(context, expression);
        return new SemanticBinding(
            [boundExpression],
            imports,
            imports
            );
    }

    #region Symbol Creation

    /// <summary>
    /// Gets an empty <see cref="Scope"/>.
    /// </summary>
    protected virtual Scope GetDefaultBindingScope() =>
        Scope.Empty;

    /// <summary>
    /// Creates the initial set of operators to use for binding.
    /// </summary>
    protected virtual ImmutableList<OperatorSymbol> GetOperators(SymbolTable symbols) =>
        OperatorSymbols.From(symbols).Default;

    /// <summary>
    /// Creates a <see cref="SymbolContext"/> with a default <see cref="Scope"/>
    /// </summary>
    protected virtual SymbolContext CreateDefaultSymbolContext(SymbolTable symbols)
    {
        return new SymbolContext(symbols, 
            GetDefaultBindingScope().AddContainer(symbols.GlobalNamespace));
    }

    /// <summary>
    /// Creates the symbol context as it is initially used for binding.
    /// </summary>
    protected virtual SymbolContext CreateInitialSymbolContext(SymbolTable symbols)
    {
        return CreateDefaultSymbolContext(symbols);
    }

    /// <summary>
    /// Create a <see cref="SymbolTable"/> from a global namespace.
    /// </summary>
    protected virtual SymbolTable CreateSymbolTable(GlobalNamespaceSymbol globalNamespace)
    {
        return new StandardSymbolTable(globalNamespace);
    }

    private record struct CreateSymbolsResult(
        GlobalNamespaceSymbol DeclaredSymbols,
        SymbolTable CombinedSymbols,
        SymbolContext Context);

    /// <summary>
    /// Creates all declared symbols from declarations.
    /// </summary>
    private CreateSymbolsResult CreateSymbols(
        IEnumerable<Declaration> declarations,
        SymbolTable externalSymbols)
    {
        GlobalNamespaceSymbol? declaredSymbols = null;
        SymbolContext? creationContext = null;

        // create combined symbols' global namespace including all declared and external symbols
        var combinedSymbols = CombinedSymbols.Create(
            globals =>
            {
                // make global namespace for all declared symbols
                declaredSymbols = new GlobalNamespaceSymbol(
                me =>
                {
                    // all top level declarations that are not global namespace declarations
                    var topLevelDeclarations = declarations.SelectMany(d =>
                        d is NamespaceDeclaration nd && nd.IsGlobalNamespace
                            ? (IEnumerable<Declaration>)nd.Declarations
                            : new[] { d })
                        .ToImmutableList();

                    return CreateAndCombineSymbols(creationContext!, me, topLevelDeclarations);
                });

                // return both declared and external global namespaces to be combined.
                return [externalSymbols.GlobalNamespace, declaredSymbols];
            });

        // symbol context for use in creating declaration symbols.
        creationContext = CreateDefaultSymbolContext(CreateSymbolTable(combinedSymbols));

        // force evaluation of global namespace symbol members
        _ = combinedSymbols.Members;
        _ = declaredSymbols!.Members;

        // force creation of all declared symbols
        // this side effect of building a map of declaration->symbol for use in binding
        declaredSymbols.WalkDeclarations(s => { });

        return new CreateSymbolsResult(declaredSymbols, CreateSymbolTable(combinedSymbols), creationContext);
    }

    /// <summary>
    /// Creates symbols for the declarations,
    /// combining same named namespace declaration symbols.
    /// </summary>
    private ImmutableList<Symbol> CreateAndCombineSymbols(
        SymbolContext context,
        NamespaceSymbol @namespace,
        IEnumerable<Declaration> declarations)
    {
        if (@namespace is not GlobalNamespaceSymbol)
        {
            context = context.WithScope(context.Scope.AddSymbolAndMembers(@namespace));
        }

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
                    var newContext = context.WithScope(context.Scope.AddSymbolAndMembers(me));
                    return CreateAndCombineSymbols(newContext, me, g.SelectMany(n => n.Declarations));
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
            context.AssociateSymbolWithDeclaration(declaration, symbol);
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
            case ClassDeclaration cd:
                return CreateClassSymbol(context, declaringSymbol, cd);

            case ConstructorDeclaration cd:
                return CreateConstructorSymbol(context, declaringSymbol, cd);

            case DelegateDeclaration dd:
                return CreateDelegateSymbol(context, declaringSymbol, dd);

            case FieldDeclaration fd:
                return CreateFieldSymbol(context, declaringSymbol, fd);

            case IndexerDeclaration id:
                return CreateIndexerSymbol(context, declaringSymbol, id);

            case InterfaceDeclaration ifd:
                return CreateInterfaceSymbol(context, declaringSymbol, ifd);

            case MethodDeclaration md:
                return CreateMethodSymbol(context, declaringSymbol, md);

            case ParameterDeclaration pd:
                return CreateParameterSymbol(context, declaringSymbol, pd);

            case PropertyDeclaration pd:
                return CreatePropertySymbol(context, declaringSymbol, pd);

            case StructDeclaration sd:
                return CreateStructSymbol(context, declaringSymbol, sd);

            case TypeParameterDeclaration tp:
                return CreateTypeParameterSymbol(context, declaringSymbol, tp);

            case UsingDeclaration:
                // this declaration does not have a symbol that is reachable from the global namespace
                return null;

            default:
                throw new InvalidOperationException($"Unhandled declaration type '{declaration.GetType().Name}' in {nameof(StandardBinder)}.{nameof(CreateDeclarationSymbol)}");
        }
    }

    protected virtual Symbol CreateClassSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        ClassDeclaration declaration)
    {
        SymbolContext typeContext = null!;

        var typeSymbol = new ClassSymbol(
            declaration.Name,
            declaringSymbol,
            declaration.Access,
            declaration.Modifiers,
            fnTypeParameters: me => 
                declaration.TypeParameters
                    .Select(tp => (TypeParameterSymbol)CreateAndMapDeclarationSymbol(context, me, tp)!)
                    .ToImmutableList()!,
            fnTypeArguments: null,
            fnBaseTypes: () => 
                declaration.BaseTypes
                    .Select(bt => GetReferencedType(typeContext.BindingContext, bt))
                    .ToImmutableList()!,
            fnMembers: me => 
                declaration.Declarations
                    .Select(d => CreateAndMapDeclarationSymbol(typeContext, me, d))
                    .Where(s => s != null)
                    .ToImmutableList()!,
            constructedFrom: null);

        typeContext = context.WithScope(
            context.Scope
                .AddSymbolAndMembers(typeSymbol)
                .AddSymbols(typeSymbol.TypeParameters)
                );

        return typeSymbol;
    }

    protected virtual Symbol CreateConstructorSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        ConstructorDeclaration declaration)
    {
        return new ConstructorSymbol(
            (TypeSymbol)declaringSymbol!,
            declaration.Access,
            declaration.Modifiers,
            fnParameters: me => 
                declaration.Parameters
                    .Select(p => (ParameterSymbol)CreateAndMapDeclarationSymbol(context, me, p)!)
                    .ToImmutableList()!
            );
    }

    protected virtual Symbol CreateDelegateSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        DelegateDeclaration declaration)
    {
        return new DelegateSymbol(
            declaration.Name,
            declaringSymbol,
            fnParameters: me => 
                declaration.Parameters
                    .Select(p => (ParameterSymbol)CreateAndMapDeclarationSymbol(context, me, p)!)
                    .ToImmutableList()!,
            fnReturnType: () => 
                GetReferencedType(context.BindingContext, declaration.ReturnType)
            );
    }

    protected virtual Symbol CreateFieldSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        FieldDeclaration declaration)
    {
        var constValue = declaration.Modifiers.Contains(SymbolModifier.Constant)
            && declaration.Initializer is ConstantExpression constExpr
            ? constExpr.Value
            : null;

        return new FieldSymbol(
            declaration.Name,
            declaringSymbol as TypeSymbol,
            declaration.Access,
            declaration.Modifiers,
            fnType: () => 
            {
                return declaration.FieldType != null ? GetReferencedType(context.BindingContext, declaration.FieldType)
                    : declaration.Initializer != null ? GetResultType(context.BindingContext, declaration.Initializer)
                    : context.Symbols.Object;
            },
            constValue
            );
    }

    protected virtual Symbol CreateIndexerSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        IndexerDeclaration declaration)
    {
        return new IndexerSymbol(
            declaration.Name,
            declaringSymbol as TypeSymbol,
            declaration.Access,
            declaration.Modifiers,
            fnElementType: () => 
            {
                return declaration.ElementType != null ? GetReferencedType(context.BindingContext, declaration.ElementType)
                    : declaration.GetMethod.ReturnType != null ? GetReferencedType(context.BindingContext, declaration.GetMethod.ReturnType)
                    : GetResultType(context.BindingContext, declaration.GetMethod.Body);
            },
            fnGetMethod: me => 
                (MethodSymbol)CreateAndMapDeclarationSymbol(context, declaringSymbol, declaration.GetMethod)!,
            fnSetMethod: 
                declaration.SetMethod != null
                    ? me => (MethodSymbol)CreateAndMapDeclarationSymbol(context, declaringSymbol, declaration.SetMethod)!
                    : null
            );
    }

    protected virtual Symbol CreateInterfaceSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        InterfaceDeclaration declaration)
    {
        SymbolContext typeContext = null!;

        var typeSymbol = new InterfaceSymbol(
            declaration.Name,
            declaringSymbol,
            declaration.Access,
            declaration.Modifiers,
            fnTypeParameters: me => 
                declaration.TypeParameters
                    .Select(tp => (TypeParameterSymbol)CreateAndMapDeclarationSymbol(context, me, tp)!)
                    .ToImmutableList()!,
            fnTypeArguments: null,
            fnBaseTypes: () => 
                declaration.BaseTypes
                    .Select(bt => GetReferencedType(typeContext.BindingContext, bt))
                    .ToImmutableList()!,
            fnMembers: me => 
                declaration.Declarations
                    .Select(d => CreateAndMapDeclarationSymbol(typeContext, me, d))
                    .Where(s => s != null)
                    .ToImmutableList()!,
            constructedFrom: null
            );

        // determine context after symbol is declared
        typeContext = context.WithScope(
            context.Scope
                .AddSymbolAndMembers(typeSymbol)
                .AddSymbols(typeSymbol.TypeParameters));

        return typeSymbol;
    }

    protected virtual Symbol CreateMethodSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        MethodDeclaration declaration)
    {
        var mmods = declaration.Modifiers
            | (declaringSymbol is NamespaceSymbol 
                ? SymbolModifier.Static 
                : SymbolModifier.None);

        return new MethodSymbol(
            declaration.Name,
            declaringSymbol,
            declaration.Access,
            mmods,
            fnTypeParameters: 
                declaration.TypeParameters.Count > 0
                    ? me => 
                        declaration.TypeParameters
                            .Select(p => (TypeParameterSymbol)CreateAndMapDeclarationSymbol(context, me, p)!)
                            .ToImmutableList()
                    : null,
            fnTypeArguments: null,
            fnParameters: me => 
                declaration.Parameters
                    .Select(p => (ParameterSymbol)CreateAndMapDeclarationSymbol(context, me, p)!)
                    .ToImmutableList()!,
            fnReturnType: () =>
            {
                return declaration.ReturnType != null 
                    ? GetReferencedType(context.BindingContext, declaration.ReturnType)
                    : GetResultType(context.BindingContext, declaration.Body);
            },
            constructedFrom: null
            );
    }

    protected virtual Symbol CreateParameterSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        ParameterDeclaration declaration)
    {
        return new ParameterSymbol(
            declaration.Name,
            declaringSymbol,
            fnType: () => 
                declaration.ParameterType != null
                    ? GetReferencedType(context.BindingContext, declaration.ParameterType)
                    : context.Symbols.Object
            );
    }

    protected virtual Symbol CreatePropertySymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        PropertyDeclaration declaration)
    {
        return new PropertySymbol(
            declaration.Name,
            declaringSymbol as TypeSymbol,
            declaration.Access,
            declaration.Modifiers,
            fnType: () =>
            {
                return declaration.PropertyType != null ? GetReferencedType(context.BindingContext, declaration.PropertyType)
                    : declaration.GetMethod.ReturnType != null ? GetReferencedType(context.BindingContext, declaration.GetMethod.ReturnType)
                    : GetResultType(context.BindingContext, declaration.GetMethod.Body);
            },
            fnBackingField: 
                declaration.BackingField != null
                    ? me => (FieldSymbol)CreateAndMapDeclarationSymbol(context, declaringSymbol, declaration.BackingField)!
                    : null,
            fnGetMethod: me => 
                (MethodSymbol)CreateAndMapDeclarationSymbol(context, declaringSymbol, declaration.GetMethod)!,
            fnSetMethod: 
                declaration.SetMethod != null
                    ? me => (MethodSymbol)CreateAndMapDeclarationSymbol(context, declaringSymbol, declaration.SetMethod)!
                    : null
        );
    }

    protected virtual Symbol CreateStructSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        StructDeclaration declaration)
    {
        SymbolContext typeContext = null!;

        var typeSymbol = new StructSymbol(
            declaration.Name,
            declaringSymbol,
            declaration.Access,
            declaration.Modifiers,
            fnTypeParameters: me => 
                declaration.TypeParameters
                    .Select(tp => (TypeParameterSymbol)CreateAndMapDeclarationSymbol(context, me, tp)!).
                    ToImmutableList()!,
            fnTypeArguments: null,
            fnBaseTypes: () => 
                declaration.BaseTypes
                    .Select(bt => GetReferencedType(typeContext.BindingContext, bt))
                    .ToImmutableList()!,
            fnMembers: me => 
                declaration.Declarations
                    .Select(d => CreateAndMapDeclarationSymbol(typeContext, me, d))
                    .Where(s => s != null)
                    .ToImmutableList()!,
            constructedFrom: null);

        typeContext = context.WithScope(
            context.Scope
                .AddSymbolAndMembers(typeSymbol)
                .AddSymbols(typeSymbol.TypeParameters));

        return typeSymbol;
    }

    protected virtual Symbol CreateTypeParameterSymbol(
        SymbolContext context,
        Symbol? declaringSymbol,
        TypeParameterDeclaration declaration)
    {
        return new TypeParameterSymbol(declaration.Name);
    }

    #endregion

    /// <summary>
    /// Binds a list of elements
    /// </summary>
    protected virtual ImmutableList<TElement> BindList<TElement>(
        BindingContext context,
        ImmutableList<TElement> list)
        where TElement : SemanticElement
    {
        return list.Rewrite(e =>
            e switch
            {
                Declaration d => (TElement)(object)BindDeclaration(context, d),
                Expression x => (TElement)(object)BindExpression(context, x),
                _ => e
            }
        );
    }

    #region Declaration Binding

    /// <summary>
    /// Binds a declaration to its associated symbol
    /// </summary>
    protected virtual Declaration BindDeclaration(
        BindingContext context,
        Declaration declaration)
    {
        Declaration bound;

        context.TryGetSymbolForUnboundDeclaration(declaration, out var symbol);

        switch (declaration)
        {
            case ClassDeclaration cld:
                bound = BindClass(context, cld, symbol as ClassSymbol);
                break;
            case ConstructorDeclaration cd:
                bound = BindConstructor(context, cd, symbol as ConstructorSymbol);
                break;
            case DelegateDeclaration dd:
                bound = BindDelegate(context, dd, symbol as DelegateSymbol);
                break;
            case FieldDeclaration fd:
                bound = BindField(context, fd, symbol as FieldSymbol);
                break;
            case IndexerDeclaration id:
                bound = BindIndexer(context, id, symbol as IndexerSymbol);
                break;
            case InterfaceDeclaration ifd:
                bound = BindInterface(context, ifd, symbol as InterfaceSymbol);
                break;
            case MethodDeclaration md:
                bound = BindMethod(context, md, symbol as MethodSymbol);
                break;
            case NamespaceDeclaration nd:
                bound = BindNamespace(context, nd, symbol as NamespaceSymbol);
                break;
            case ParameterDeclaration prd:
                bound = BindParameter(context, prd, symbol as ParameterSymbol);
                break;
            case PropertyDeclaration pd:
                bound = BindProperty(context, pd, symbol as PropertySymbol);
                break;
            case StructDeclaration std:
                bound = BindStruct(context, std, symbol as StructSymbol);
                break;
            case TypeParameterDeclaration tp:
                bound = BindTypeParameter(context, tp, symbol as TypeParameterSymbol);
                break;
            case UsingDeclaration ud:
                bound = BindUsing(context, ud);
                break;
            default:
                throw new InvalidCastException($"Unhandled declaration '{declaration.GetType().Name}' in {nameof(StandardBinder)}.{nameof(BindDeclaration)}");
        }

        return bound;
    }

    #region Class Declaration
    /// <summary>
    /// Binds <see cref="ClassDeclaration"/>
    /// </summary>
    protected virtual ClassDeclaration BindClass(
        BindingContext context,
        ClassDeclaration decl,
        ClassSymbol? symbol)
    {
        var typeParameters = BindList(context, decl.TypeParameters);

        // put symbol in scope for baseTypes and members
        var typeContext = symbol != null
            ? context
                .WithScope(context.Scope.AddSymbolAndMembers(symbol))
                .WithDeclaringType(symbol)
            : context;

        var baseTypes = BindList(typeContext, decl.BaseTypes);
        var declarations = BindList(typeContext, decl.Declarations);

        if (typeParameters == decl.TypeParameters
            && baseTypes == decl.BaseTypes
            && declarations == decl.Declarations
            && symbol == decl.Symbol)
            return decl;

        return new ClassDeclaration(
            decl.Name,
            decl.Access,
            decl.Modifiers,
            typeParameters,
            baseTypes,
            declarations,
            decl.Location,
            symbol,
            decl.Diagnostics
            );
    }
    #endregion

    #region Constructor Declaration

    protected virtual ConstructorDeclaration BindConstructor(
        BindingContext context,
        ConstructorDeclaration cd,
        ConstructorSymbol? constructorSymbol)
    {
        var parameters = BindList(context, cd.Parameters);
        var returnLabel = new LabelSymbol(LabelSymbol.ReturnLabelName, context.Symbols.Void);
        var bodyContext = context.WithScope(context.Scope.AddSymbol(returnLabel));

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

    #endregion

    #region Delegate Declaration

    protected virtual DelegateDeclaration BindDelegate(
        BindingContext context,
        DelegateDeclaration decl,
        DelegateSymbol? delegateSymbol)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var typeParameters = BindList(context, decl.TypeParameters);
            var parameters = BindList(context, decl.Parameters);
            var returnType = BindTypeExpression(context, decl.ReturnType, diagnostics);
            var baseTypes = BindTypeExpressionList(context, decl.BaseTypes, diagnostics);
            var declarations = BindList(context, decl.Declarations);

            if (typeParameters == decl.TypeParameters
                && parameters == decl.Parameters
                && returnType == decl.ReturnType
                && baseTypes == decl.BaseTypes
                && declarations == decl.Declarations)
                return decl;

            return new DelegateDeclaration(
                decl.Name,
                decl.Access,
                decl.Modifiers,
                typeParameters,
                baseTypes,
                declarations,
                parameters,
                returnType,
                decl.Location,
                delegateSymbol,
                diagnostics.ToImmutableList()
                );
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }

    #endregion

    #region Field Declaration
    /// <summary>
    /// Binds <see cref="FieldDeclaration"/>
    /// </summary>
    protected virtual FieldDeclaration BindField(
        BindingContext context,
        FieldDeclaration fd,
        FieldSymbol? fieldSymbol)
    {
        var fieldType = fd.FieldType != null 
            ? BindExpression(context, fd.FieldType) 
            : null;

        var initializer = fd.Initializer != null
            ? BindExpression(context.WithTargetType(fieldType?.ReferencedSymbol as TypeSymbol), fd.Initializer)
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

    #region Indexer Declaration
    /// <summary>
    /// Binds <see cref="IndexerDeclaration"/>
    /// </summary>
    protected virtual IndexerDeclaration BindIndexer(
        BindingContext context,
        IndexerDeclaration id,
        IndexerSymbol? indexerSymbol)
    {
        var elementType = id.ElementType != null 
            ? BindExpression(context, id.ElementType) 
            : null;

        var methodContext = context;

        var getMethod = (MethodDeclaration)BindDeclaration(methodContext, id.GetMethod);

        var setMethod = id.SetMethod != null
            ? (MethodDeclaration)BindDeclaration(methodContext, id.SetMethod)
            : null;

        return new IndexerDeclaration(
            id.Access,
            id.Modifiers,
            elementType,
            getMethod,
            setMethod,
            id.Location,
            indexerSymbol,
            id.Diagnostics
            );
    }
    #endregion

    #region Interface Declaration
    /// <summary>
    /// Binds <see cref="InterfaceDeclaration"/>
    /// </summary>
    protected virtual InterfaceDeclaration BindInterface(
        BindingContext context,
        InterfaceDeclaration decl,
        InterfaceSymbol? symbol)
    {
        var typeParameters = BindList(context, decl.TypeParameters);

        // put symbol in scope for baseTypes and members
        var typeContext = symbol != null
            ? context
                .WithScope(context.Scope.AddSymbolAndMembers(symbol))
                .WithDeclaringType(symbol)
            : context;

        var baseTypes = BindList(typeContext, decl.BaseTypes);
        var declarations = BindList(typeContext, decl.Declarations);

        if (typeParameters == decl.TypeParameters
            && baseTypes == decl.BaseTypes
            && declarations == decl.Declarations
            && symbol == decl.Symbol)
            return decl;

        return new InterfaceDeclaration(
            decl.Name,
            decl.Access,
            decl.Modifiers,
            typeParameters,
            baseTypes,
            declarations,
            decl.Location,
            symbol,
            decl.Diagnostics
            );
    }
    #endregion

    #region Method Declaration
    /// <summary>
    /// Binds <see cref="MethodDeclaration"/>
    /// </summary>
    protected virtual MethodDeclaration BindMethod(
        BindingContext context,
        MethodDeclaration md,
        MethodSymbol? methodSymbol)
    {
        var typeParameters = BindList(context, md.TypeParameters);
        var parameters = BindList(context, md.Parameters);
        var returnType = md.ReturnType != null
            ? BindExpression(context, md.ReturnType)
            : null;
        var returnLabel = new LabelSymbol(LabelSymbol.ReturnLabelName, methodSymbol?.ReturnType ?? context.Symbols.Void);

        var bodyContext = context
            .WithScope(context.Scope.AddSymbol(returnLabel))
            .WithTargetType(returnType?.ReferencedSymbol as TypeSymbol);

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
            returnType,
            body,
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
        BindingContext context,
        NamespaceDeclaration nd,
        NamespaceSymbol? nsSymbol)
    {
        var bodyContext = context;
        if (nsSymbol != null)
        {
            bodyContext = bodyContext.WithScope(bodyContext.Scope.AddSymbolAndMembers(nsSymbol));
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
                    _context = _context.WithScope(_context.Scope.AddSymbolAndMembers(ns));
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
        BindingContext context,
        ParameterDeclaration pd,
        ParameterSymbol? parameterSymbol)
    {
        var parameterType = pd.ParameterType != null
            ? BindExpression(context, pd.ParameterType)
            : null;

        return new ParameterDeclaration(
            pd.Name,
            pd.Modifiers,
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
        BindingContext context,
        PropertyDeclaration pd,
        PropertySymbol? propertySymbol)
    {
        var propertyType = pd.PropertyType != null
            ? BindExpression(context, pd.PropertyType)
            : null;

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

    #region Struct Declaration
    /// <summary>
    /// Binds <see cref="StructDeclaration"/>
    /// </summary>
    protected virtual StructDeclaration BindStruct(
        BindingContext context,
        StructDeclaration decl,
        StructSymbol? symbol)
    {
        var typeParameters = BindList(context, decl.TypeParameters);

        // put symbol in scope for baseTypes and members
        var typeContext = symbol != null
            ? context
                .WithScope(context.Scope.AddSymbolAndMembers(symbol))
                .WithDeclaringType(symbol)
            : context;

        var baseTypes = BindList(typeContext, decl.BaseTypes);
        var declarations = BindList(typeContext, decl.Declarations);

        if (typeParameters == decl.TypeParameters
            && baseTypes == decl.BaseTypes
            && declarations == decl.Declarations
            && symbol == decl.Symbol)
            return decl;

        return new StructDeclaration(
            decl.Name,
            decl.Access,
            decl.Modifiers,
            typeParameters,
            baseTypes,
            declarations,
            decl.Location,
            symbol,
            decl.Diagnostics
            );
    }
    #endregion

    #region TypeParameter Declaration
    /// <summary>
    /// Binds <see cref="TypeParameterDeclaration"/>
    /// </summary>
    protected virtual TypeParameterDeclaration BindTypeParameter(
        BindingContext context,
        TypeParameterDeclaration typeParameter,
        TypeParameterSymbol? typeParameterSymbol)
    {
        if (typeParameter.Symbol == typeParameterSymbol)
            return typeParameter;

        return new TypeParameterDeclaration(
            typeParameter.Name,
            typeParameter.Location,
            typeParameterSymbol,
            typeParameter.Diagnostics);
    }
    #endregion
    
    #region Using Declaration
    /// <summary>
    /// Binds <see cref="UsingDeclaration"/>
    /// </summary>
    protected virtual UsingDeclaration BindUsing(
        BindingContext context,
        UsingDeclaration ud)
    {
        var expression = BindExpression(context, ud.Expression);

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

    #region Expression Binding
    private ImmutableDictionary<string, ImmutableList<OperatorSymbol>>? _kindToOperatorsMap;

    protected virtual ImmutableList<OperatorSymbol> GetOperators(BindingContext context, string operatorKind)
    {
        if (_kindToOperatorsMap == null)
        {
            _kindToOperatorsMap = OperatorSymbols.From(context.Symbols).Default
                .GroupBy(op => op.Kind)
                .ToImmutableDictionary(g => g.Key, g => g.ToImmutableList());
        }

        if (_kindToOperatorsMap.TryGetValue(operatorKind, out var operators))
            return operators;

        return ImmutableList<OperatorSymbol>.Empty;
    }


    /// <summary>
    /// Binds all unbound expressions
    /// </summary>
    protected virtual Expression BindExpression(BindingContext context, Expression expression)
    {
        switch (expression)
        {
            case ArrayExpression array:
                return BindArray(context, array);

            case ArityExpression arity:
                return BindArity(context, arity);

            case AssignExpression assign:
                return BindAssign(context, assign);

            case AsTypeExpression asType:
                return BindAsType(context, asType);

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

            case ElementExpression element:
                return BindElement(context, element);

            case LabelExpression label:
                return BindLabel(context, label);

            case LambdaExpression lambda:
                return BindLambda(context, lambda);

            case LoopExpression loop:
                return BindLoop(context, loop);

            case MemberExpression member:
                return BindMember(context, member);

            case NameExpression nameRef:
                return BindNameReference(context, nameRef);

            case NewArrayExpression newArrayInit:
                return BindNewArray(context, newArrayInit);

            case NewExpression @new:
                return BindNew(context, @new);

            case OperatorExpression opex:
                return BindOperator(context, opex);

            case SymbolExpression symbolRef:
                return BindSymbolReference(context, symbolRef);

            case ThisExpression me:
                return BindThis(context, me);

            case IsTypeExpression istype:
                return BindIsType(context, istype);

            case TypeOfExpression toe:
                return BindTypeOf(context, toe);

            case ConstructExpression construct:
                return BindConstruct(context, construct);

            case VariableExpression variable:
                return BindVariable(context, variable);

            default:
                throw new InvalidOperationException($"Unhandled semantic '{expression.GetType().Name}' in {nameof(StandardBinder)}.BindExpression");
        }
    }

    /// <summary>
    /// Binds the type expression and reports an error if the expression does not bind to a type.
    /// </summary>
    protected virtual Expression BindTypeExpression(BindingContext context, Expression type, List<Diagnostic> diagnostics)
    {
        var expr = BindExpression(context.WithTargetType(null), type);

        var isType = 
            expr.ReferencedSymbol is TypeSymbol
            || expr.ReferencedSymbol is GroupSymbol gs && gs.Members.Any(m => m is TypeSymbol);

        if (!isType)
        {
            diagnostics.Add(SemanticDiagnostics.ExpressionIsNotType().WithLocation(type.Location));
        }

        return expr;
    }

    /// <summary>
    /// Binds a list of type expressions.
    /// </summary>
    protected virtual ImmutableList<TExpression> BindTypeExpressionList<TExpression>(
        BindingContext context,
        ImmutableList<TExpression> expressions,
        List<Diagnostic> diagnostics)
        where TExpression : Expression
    {
        if (expressions.Count == 0)
            return expressions;
        return expressions.Rewrite(e => (TExpression)BindTypeExpression(context, e, diagnostics));
    }

    private readonly Dictionary<Expression, TypeSymbol> _cachedResultType =
        new Dictionary<Expression, TypeSymbol>();

    private readonly Dictionary<Expression, Symbol?> _cachedReferencedSymbol =
        new Dictionary<Expression, Symbol?>();

    /// <summary>
    /// Gets the reference symbol of an expression.
    /// </summary>
    protected virtual Symbol? GetReferencedSymbol(BindingContext context, Expression expression)
    {
        var bound = BindExpression(context, expression);
        return bound.ReferencedSymbol;
    }

    /// <summary>
    /// Gets the referenced type of an expression
    /// </summary>
    protected virtual TypeSymbol GetReferencedType(BindingContext context, Expression typeExpression) =>
        GetReferencedSymbol(context, typeExpression) as TypeSymbol ?? SpecialSymbols.Unknown;

    /// <summary>
    /// Gets the result type of an expression
    /// </summary>
    protected virtual TypeSymbol GetResultType(BindingContext context, Expression expression)
    {
        var bound = BindExpression(context, expression);
        return bound.ResultType;
    }

    #region Array Expression
    /// <summary>
    /// Binds <see cref="ArrayExpression"/>,
    /// converting referenced symbols into arrays of those symbols.
    /// </summary>
    protected virtual Expression BindArray(BindingContext context, ArrayExpression array)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var elementTypeExpr = BindTypeExpression(context, array.TypeOrMember, diagnostics);
            var elementType = elementTypeExpr.ReferencedSymbol as TypeSymbol;
            var arrayType = elementType != null ? context.Symbols.GetArray(elementType) : null;
            var resultType = context.Symbols.Type; // a type is a type

            if (elementType == null)
            {
                diagnostics.Add(SemanticDiagnostics.ReferencedSymbolNotType().WithLocation(array.Location));
            }

            if (elementTypeExpr == array.TypeOrMember
                && arrayType == array.ReferencedSymbol
                && resultType == array.ResultType
                && diagnostics.Count == 0)
                return array;

            return new ArrayExpression(
                elementTypeExpr,
                array.Location,
                arrayType,
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
    protected virtual Expression BindArity(BindingContext context, ArityExpression arity)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(context, arity.TypeOrMember);

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
                diagnostics.Add(SemanticDiagnostics.NoReferencedSymbolsHaveMatchingArity().WithLocation(arity.Location));
            }

            var resultType = GetReferenceResultType(context, referencedSymbol);

            if (expression == arity.TypeOrMember
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
    protected virtual Expression BindAssign(BindingContext context, AssignExpression assign)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var target = BindExpression(context.WithTargetType(null), assign.Target);
            var assignType = target.ResultType ?? context.Symbols.Object;
            var source = BindExpression(context.WithTargetType(assignType), assign.Source);
            source = ConvertTo(context, source, assignType);

            if (IsValidAssignmentTarget(assign))
            {
                diagnostics.Add(SemanticDiagnostics.NotAValidAssignmentTarget().WithLocation(assign.Target.Location));
            }

            if (target == assign.Target
                && source == assign.Source
                && assign.ResultType == target.ResultType
                && assign.Diagnostics.Count == 0
                && diagnostics.Count == 0)
                return assign;

            return new AssignExpression(
                target,
                source,
                assign.Location,
                assignType,
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
    protected virtual bool IsValidAssignmentTarget(Expression expression) =>
        expression.ReferencedSymbol switch
        {
            VariableSymbol v => true,

            FieldSymbol f => !f.IsReadOnly,

            PropertySymbol p => p.SetMethod != null,

            _ => expression is ElementExpression e
                && (e.Expression.ResultType.IsArray
                    || (e.Expression.ReferencedSymbol is IndexerSymbol i && i.SetMethod != null))
        };

    #endregion

    #region AsType Expression
    /// <summary>
    /// Binds <see cref="AsTypeExpression"/>
    /// </summary>
    protected virtual Expression BindAsType(BindingContext context, AsTypeExpression asType)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithTargetType(null);
            var expression = BindExpression(context, asType.Expression);
            var type = BindTypeExpression(context, asType.Type, diagnostics);
            var typeSymbol = type.ReferencedSymbol as TypeSymbol;

            if (expression == asType.Expression
                && type == asType.Type
                && typeSymbol == asType.TypeSymbol
                && typeSymbol == asType.ResultType
                && diagnostics.Count == 0
                && asType.Diagnostics.Count == 0)
                return asType;

            return new AsTypeExpression(
                expression,
                type,
                asType.Location,
                typeSymbol,
                typeSymbol,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }
    #endregion

    #region Block Expression
    /// <summary>
    /// Binds a <see cref="BlockExpression"/>
    /// </summary>
    protected virtual Expression BindBlock(BindingContext context, BlockExpression block)
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
                        ? GetReferencedType(labelContext, label.ReceivingType)
                        : context.Symbols.Void;
                    var labelSymbol = label.LabelSymbol ?? new LabelSymbol(label.Name, type!);
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
                    _context = _context.WithScope(context.Scope.AddSymbol(decl.VariableSymbol));
                }

                return (boundExpression, _context);
            });

            var resultType = boundExpressions.Count > 0
                ? boundExpressions[^1].ResultType
                : context.Symbols.Void;

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
    protected virtual void GetBranchExpressionTypes(BindingContext context, Expression blockBody, LabelSymbol label, List<TypeSymbol> types)
    {
        types.AddRange(
            blockBody.SelectWhere(
                s => s is Expression e && !HasIsolatedBody(e),
                s => s is BranchExpression b && b.LabelSymbol == label,
                s => ((BranchExpression)s).Expression != null ? ((BranchExpression)s).Expression!.ResultType : context.Symbols.Void));
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
    protected virtual Expression BindBranch(BindingContext context, BranchExpression branch)
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
                : context.Symbols.Void;

            if (labelSymbol == null)
            {
                diagnostics.Add(SemanticDiagnostics.NoMatchingTarget(branch.LabelName).WithLocation(branch.Location));
            }
            else if (expression != null && expressionType != labelSymbol.Type)
            {
                expression = ConvertTo(context, expression, labelSymbol.Type);
                expression = BindExpression(context, expression);
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
    protected virtual LabelSymbol? GetLabel(BindingContext context, string name)
    {
        return this.GetFirstMatchingSymbolInScope<LabelSymbol>(context.Scope, name, null);
    }
    #endregion

    #region Call Expression
    /// <summary>
    /// Binds <see cref="CallExpression"/>
    /// </summary>
    protected virtual Expression BindCall(BindingContext context, CallExpression call)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithTargetType(null);

            var expression = BindExpression(context, call.Expression);
            var arguments = BindList(context, call.Arguments);

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
                diagnostics.Add(SemanticDiagnostics.NoCallableSymbol().WithLocation(location));
            }
            else
            {
                calledSymbol = GetBestCalledSymbol(context, arguments, candidates);

                if (calledSymbol == null)
                {
                    diagnostics.Add(SemanticDiagnostics.CallIsAmbiguous().WithLocation(location));
                }
                else
                {
                    var parameters = GetSymbolParameters(calledSymbol);
                    if (parameters.Count != arguments.Count)
                    {
                        diagnostics.Add(SemanticDiagnostics.IncorrectNumberOfArguments().WithLocation(location));
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
                return GetCallInstance(filter.TypeOrMember);
            default:
                return expression;
        }
    }

    /// <summary>
    /// True if the symbol is callable by a <see cref="CallExpression"/>
    /// </summary>
    protected virtual bool IsCallableSymbol(Symbol symbol) =>
        symbol is DelegateSymbol or MethodSymbol or ConstructorSymbol;

    /// <summary>
    /// Gets a called symbol's return type.
    /// </summary>
    protected virtual TypeSymbol? GetCalledSymbolReturnType(Symbol symbol) =>
        symbol switch
        {
            DelegateSymbol f => f.ReturnType,
            MethodSymbol m => m.ReturnType,
            ConstructorSymbol c => c.ConstructedType,
            _ => null
        };

    /// <summary>
    /// Gets the list of candidate symbols for a call with the supplied arguments
    /// </summary>
    protected virtual void GetCalledSymbolCandidates(
        BindingContext context,
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
    protected virtual Symbol? GetBestCalledSymbol(BindingContext context, ImmutableList<Expression> arguments, List<Symbol> candidates)
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
            DelegateSymbol function => function.Parameters,
            MethodSymbol method => method.Parameters,
            ConstructorSymbol constructor => constructor.Parameters,
            _ => ImmutableList<ParameterSymbol>.Empty
        };

    /// <summary>
    /// Returns true if the callable symbol has parameters that are compatible with the arguments
    /// </summary>
    protected virtual bool MatchesParameters(
        BindingContext context,
        Symbol callableSymbol,
        ImmutableList<Expression> arguments) =>
        MatchesParameters(context, GetSymbolParameters(callableSymbol), arguments);

    /// <summary>
    /// Returns true if the set of arguments matches the parameters.
    /// </summary>
    protected virtual bool MatchesParameters(
        BindingContext context,
        ImmutableList<ParameterSymbol> parameters,
        ImmutableList<Expression> arguments)
    {
        if (parameters.Count != arguments.Count)
            return false;

        for (int i = 0; i < parameters.Count; i++)
        {
            var conversion = GetConversion(context, arguments[i].ResultType, parameters[i].Type);
            if (conversion == ConversionKind.None)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Converts a set of arguments to their corresponding parameter types.
    /// </summary>
    protected virtual ImmutableList<Expression> ConvertArguments(
        BindingContext context,
        ImmutableList<ParameterSymbol> parameters,
        ImmutableList<Expression> arguments)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var argument = arguments[i];
            var convertedArg = ConvertTo(context, argument, parameter.Type);
            arguments = arguments.SetItem(i, convertedArg);
        }

        return arguments;
    }
    #endregion

    #region Condition Expression
    /// <summary>
    /// Binds <see cref="ConditionExpression"/>
    /// </summary>
    protected virtual Expression BindCondition(BindingContext context, ConditionExpression condition)
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
                diagnostics.Add(SemanticDiagnostics.NoCommonTypeFound().WithLocation(condition.Location));
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
    protected virtual Expression BindConstant(BindingContext context, ConstantExpression constant)
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

    #region Construct Expression
    /// <summary>
    /// Binds <see cref="ConstructExpression"/>,
    /// Constructs type expression by applying type arguments.
    /// </summary>
    protected virtual Expression BindConstruct(BindingContext context, ConstructExpression construct)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(context, construct.TypeOrMember);
            var typeArguments = BindTypeExpressionList(context, construct.TypeArguments, diagnostics);

            Symbol? constructedSymbol = null;
            if (expression.ReferencedSymbol is Symbol symbol)
            {
                var typeArgs = typeArguments
                    .Select(ta => GetReferencedType(context, ta))
                    .OfType<TypeSymbol>()
                    .ToImmutableList();

                constructedSymbol = GetConstructedSymbol(context, symbol, typeArgs, construct.Location, diagnostics);
            }

            var resultType = GetReferenceResultType(context, constructedSymbol);

            if (expression == construct.TypeOrMember
                && typeArguments == construct.TypeArguments
                && constructedSymbol == construct.ConstructedSymbol
                && resultType == construct.ResultType
                && diagnostics.Count == 0)
                return construct;

            return new ConstructExpression(
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
    protected virtual Symbol? GetConstructedSymbol(
        BindingContext context,
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
                    diagnostics.Add(SemanticDiagnostics.NoTypeOrMethodWithMatchingArityToConstruct().WithLocation(location));
                return null;
            }

            var constructedSymbols =
                group.Symbols
                .Select(s => GetConstructedSymbol(context, s, typeArguments))
                .OfType<Symbol>()
                .ToImmutableList();

            return context.Symbols.GetGroup(constructedSymbols);
        }
        else if (symbol is TypeSymbol type)
        {
            if (type.Arity != typeArguments.Count)
            {
                if (diagnostics != null)
                    diagnostics.Add(SemanticDiagnostics.TypeDoesNotHaveMatchingArity().WithLocation(location));
                return null;
            }

            return context.Symbols.GetConstructed(type, typeArguments);
        }
        else if (symbol is MethodSymbol method)
        {
            if (method.Arity != typeArguments.Count)
            {
                if (diagnostics != null)
                    diagnostics.Add(SemanticDiagnostics.MethodDoesNotHaveMatchingArity().WithLocation(location));
                return null;
            }
            return context.Symbols.GetConstructed(method, typeArguments);
        }
        else
        {
            if (diagnostics != null)
                diagnostics.Add(SemanticDiagnostics.NoTypeOrMethodWithMatchingArityToConstruct().WithLocation(location));
        }

        return null;
    }
    #endregion

    #region Conversion Expression
    /// <summary>
    /// Binds <see cref="ConvertExpression"/>
    /// </summary>
    protected virtual Expression BindConvert(BindingContext context, ConvertExpression convert)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            var convertedType = convert.ConvertedType != null
                ? BindTypeExpression(context, convert.ConvertedType, diagnostics)
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
    protected virtual Expression ConvertTo(BindingContext context, Expression expression, TypeSymbol type)
    {
        // ignore void
        if (type == context.Symbols.Void
            || expression.ResultType == context.Symbols.Void)
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
        BindingContext context,
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
        BindingContext context,
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
                diagnostics?.Add(SemanticDiagnostics.CannotConvert(sourceType, SpecialSymbols.Unknown).WithLocation(location));
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
                    diagnostics?.Add(SemanticDiagnostics.CannotConvert(sourceType, targetType ?? SpecialSymbols.Unknown).WithLocation(location));
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
        BindingContext context,
        TypeSymbol sourceType, 
        TypeSymbol targetType)
    {
        if (sourceType == targetType)
            return ConversionKind.SameType;

        if (sourceType == SpecialSymbols.DoesNotReturn)
            return ConversionKind.DoesNotReturn;

        if (targetType == context.Symbols.Object
            && sourceType.IsValueType)
            return ConversionKind.Boxing;

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
    protected virtual bool CanDownCast(BindingContext context, TypeSymbol source, TypeSymbol target)
    {
        if (TypeEqualityComparer.Instance.Equals(source, target))
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
    protected virtual bool CanUpCast(BindingContext context, TypeSymbol source, TypeSymbol target)
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
    protected virtual bool CanWiden(BindingContext context, TypeSymbol source, TypeSymbol target)
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
    protected virtual bool IsAssignableWithoutConversion(BindingContext context, TypeSymbol sourceType, TypeSymbol targetType)
    {
        if (sourceType == targetType)
            return true;

        if (sourceType == SpecialSymbols.DoesNotReturn)
            return true;

        if (targetType == context.Symbols.Object)
            return true;

        if (CanDownCast(context, sourceType, targetType))
            return true;

        return false;
    }

    /// <summary>
    /// Gets all candidates for custom conversion
    /// </summary>
    protected virtual void GetConversionOperatorCandidates(
        BindingContext context,
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
        BindingContext context,
        Symbol conversionSymbol,
        TypeSymbol source,
        TypeSymbol target)
    {
        return conversionSymbol switch
        {
            DelegateSymbol function =>
                function.ReturnType == target
                && function.Parameters.Count == 1
                && GetConversion(context, source, function.Parameters[0].Type) != ConversionKind.None,
            MethodSymbol method =>
                method.IsStatic
                && method.ReturnType == target
                && method.Parameters.Count == 1
                && GetConversion(context, source, method.Parameters[0].Type) != ConversionKind.None,
            _ => false
        };
    }

    /// <summary>
    /// Determines the best custom conversion symbol from a set of candidate conversion symbols.
    /// </summary>
    protected virtual Symbol? GetBestConversionOperator(
        BindingContext context,
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
    protected virtual Expression BindDefault(BindingContext context, DefaultExpression dex)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            Expression? typeExpr = null;
            TypeSymbol? resultType = null;

            if (dex.Type != null)
            {
                typeExpr = dex.Type != null ? BindTypeExpression(context, dex.Type, diagnostics) : null;
                resultType = typeExpr != null ? typeExpr.ReferencedSymbol as TypeSymbol : null;
            }
            else if (context.TargetType != null)
            {
                resultType = context.TargetType;
            }
            else
            {
                diagnostics.Add(SemanticDiagnostics.DefaultTypeCannotBeInferred().WithLocation(dex.Location));
                resultType = context.Symbols.Object;
            }

            if (typeExpr == dex.Type
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

    #region Element Expression
    protected virtual Expression BindElement(BindingContext context, ElementExpression element)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            var expr = BindExpression(context, element.Expression);
            var arguments = BindList(context, element.Arguments);

            TypeSymbol indexedType = expr.ResultType;
            if (expr.ReferencedSymbol is TypeSymbol referencedType)
            {
                indexedType = referencedType;
            }

            if (indexedType is ArraySymbol array)
            {
                return new ElementExpression(expr, arguments, element.Location, null, array.ElementType, diagnostics.ToImmutableList());
            }
            else
            {
                GetIndexerCandidates(
                    context,
                    indexedType,
                    isStatic: expr.ReferencedSymbol is TypeSymbol,
                    element.Arguments,
                    candidates);

                IndexerSymbol? indexer = null;

                if (candidates.Count == 0)
                {
                    diagnostics.Add(SemanticDiagnostics.NoMatchingIndexer().WithLocation(element.Location));
                }
                else
                {
                    indexer = candidates.Count == 1
                        ? (IndexerSymbol)candidates[0]
                        : GetBestIndexerCandidate(context, element.Arguments, candidates);

                    if (indexer == null)
                    {
                        diagnostics.Add(SemanticDiagnostics.IndexerIsAmbiguous().WithLocation(element.Location));
                    }
                    else
                    {
                        var parameters = indexer.GetMethod!.Parameters;
                        if (parameters.Count != arguments.Count)
                        {
                            diagnostics.Add(SemanticDiagnostics.IncorrectNumberOfArguments().WithLocation(element.Location));
                        }
                        else
                        {
                            arguments = ConvertArguments(context, parameters, arguments);
                        }
                    }
                }

                var resultType = indexer != null 
                    ? indexer.ElementType 
                    : SpecialSymbols.Unknown;

                // no change?
                if (element.Expression == expr
                    && element.Arguments == arguments
                    && element.IndexerSymbol == indexer
                    && element.ResultType == resultType
                    && element.Diagnostics.Count == 0
                    && diagnostics.Count == 0)
                    return element;

                return new ElementExpression(
                    expr,
                    arguments,
                    element.Location,
                    indexer,
                    resultType,
                    diagnostics.ToImmutableList()
                    );
            }
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
            _symbolListPool.ReturnToPool(candidates);
        }
    }

    /// <summary>
    /// Gets the list of candidate symbols for a call with the supplied arguments
    /// </summary>
    protected virtual void GetIndexerCandidates(
        BindingContext context,
        TypeSymbol targetType,
        bool isStatic,
        ImmutableList<Expression> arguments,
        List<Symbol> candidates)
    {
        GetMatchingTypeMembers(
            targetType,
            null,
            s => s is IndexerSymbol x
                && x.IsStatic == isStatic
                && x.GetMethod != null
                && x.GetMethod.Parameters.Count == arguments.Count,
            candidates);
    }

    protected virtual IndexerSymbol? GetBestIndexerCandidate(BindingContext context, ImmutableList<Expression> arguments, List<Symbol> candidates)
    {
        // TODO: betterness
        return candidates.OfType<IndexerSymbol>().FirstOrDefault(x => MatchesParameters(context, x.GetMethod!.Parameters, arguments));
    }

    #endregion

    #region IsType Expression
    /// <summary>
    /// Binds <see cref="IsTypeExpression"/>
    /// </summary>
    protected virtual Expression BindIsType(BindingContext context, IsTypeExpression istype)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            context = context.WithTargetType(null);
            var expression = BindExpression(context, istype.Expression);
            var type = BindTypeExpression(context, istype.Type, diagnostics);
            var typeSymbol = type.ReferencedSymbol as TypeSymbol;
            var resultType = context.Symbols.Boolean;

            if (expression == istype.Expression
                && type == istype.Type
                && typeSymbol == istype.TypeSymbol
                && resultType == istype.ResultType
                && diagnostics.Count == 0
                && istype.Diagnostics.Count == 0)
                return istype;

            return new IsTypeExpression(
                expression,
                type,
                istype.Location,
                typeSymbol,
                resultType,
                diagnostics.ToImmutableList());
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
    protected virtual Expression BindLabel(BindingContext context, LabelExpression label)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var receivingType = label.ReceivingType != null 
                ? BindTypeExpression(context, label.ReceivingType, diagnostics)
                : null;

            var resultType = receivingType != null 
                ? receivingType.ReferencedSymbol as TypeSymbol 
                : context.Symbols.Void;

            var targetSymbol = context.GetLabelSymbol(label) 
                ?? new LabelSymbol(label.Name, resultType!); // not found, must not have been inside a block

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
    protected virtual Expression BindLambda(BindingContext context, LambdaExpression lambda)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var types = _typeListPool.AllocateFromPool();
        try
        {
            DelegateSymbol? functionSymbol = lambda.FunctionSymbol;
            LabelSymbol? returnLabel = lambda.ReturnLabel
                ?? new LabelSymbol(LabelSymbol.ReturnLabelName, context.Symbols.Object);
            ImmutableList<ParameterDeclaration> parameters = lambda.Parameters;
            Expression body = lambda.Body;
            Expression? returnType = lambda.ReturnType;
            TypeSymbol? returnTypeSymbol = null;

            BindLambdaSymbol(context);

            if (returnLabel.Type != returnTypeSymbol)
            {
                returnLabel = new LabelSymbol(LabelSymbol.ReturnLabelName, returnTypeSymbol!);
                BindLambdaSymbol(context);
            }

            var resultType = GetReferenceResultType(context, functionSymbol);

            if (parameters == lambda.Parameters
                && body == lambda.Body
                && returnType == lambda.ReturnType
                && functionSymbol != null
                && functionSymbol == lambda.FunctionSymbol
                && resultType == lambda.ResultType
                && diagnostics.Count == 0)
                return lambda;

            return new LambdaExpression(
                lambda.Name,
                parameters ?? ImmutableList<ParameterDeclaration>.Empty,
                returnType,
                body,
                lambda.Location,
                functionSymbol,
                returnLabel,
                resultType,
                diagnostics.ToImmutableList());

            void BindLambdaSymbol(BindingContext context)
            {
                // bind and evalute new function symbol at the same time
                functionSymbol = new DelegateSymbol(
                    lambda.Name,
                    null,
                    SymbolAccess.Public,
                    SymbolModifier.None,
                    me =>
                    {
                        var pms = CreateParameterSymbols(me, context);
                        BindBodyAndReturnType(pms, context);
                        return pms;
                    },
                    () => returnTypeSymbol!);
                // force eval of deferred parameters and return type
                // for side-effect assignment to locals  (Erik Meijer said it was okay)
                var _ = functionSymbol.Parameters;
                returnTypeSymbol = functionSymbol.ReturnType;
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
                    var type = p.ParameterType != null ? BindTypeExpression(context, p.ParameterType, diagnostics) : null;
                    var ptype = type?.ReferencedSymbol as TypeSymbol ?? context.Symbols.Object;
                    var psymbol = new ParameterSymbol(p.Name, declaringSymbol, ptype);
                    var pdecl = new ParameterDeclaration(p.Name, p.Modifiers, type, p.Location, psymbol, null);
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
                        .AddSymbol(returnLabel));
                returnType = returnType != null ? BindExpression(bodyContext, returnType) : null;
                returnTypeSymbol = returnType != null ? returnType?.ReferencedSymbol as TypeSymbol : returnTypeSymbol;
                if (returnTypeSymbol != null)
                    bodyContext = bodyContext.WithTargetType(returnTypeSymbol);
                body = BindExpression(bodyContext, body);
                returnTypeSymbol ??= GetLambdaReturnType(context, body, returnLabel, diagnostics);
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
        BindingContext context,
        Expression body,
        LabelSymbol returnTarget,
        List<Diagnostic> diagnostics)
    {
        var types = _typeListPool.AllocateFromPool();
        try
        {
            GetBranchExpressionTypes(context, body, returnTarget, types);
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
    protected virtual Expression BindLoop(BindingContext context, LoopExpression loop)
    {
        var diagostics = _diagnosticListPool.AllocateFromPool();
        var types = _typeListPool.AllocateFromPool();
        try
        {
            var breakTarget = loop.BreakTarget ?? new LabelSymbol(LabelSymbol.BreakLabelName, context.Symbols.Object);
            var continueTarget = loop.ContinueTarget ?? new LabelSymbol(LabelSymbol.ContinueLabelName, context.Symbols.Void);

            var bodyContext = context.WithScope(
                context.Scope.AddSymbols(new[] { breakTarget, continueTarget }));

            var body = BindExpression(bodyContext, loop.Body);

            // result type is the common type of all the break branches.
            GetBranchExpressionTypes(context, body, breakTarget, types);
            var resultType = GetBestCommonType(context, types, voidIsBetter: false) ?? context.Symbols.Void;

            if (breakTarget.Type != resultType)
            {
                breakTarget = new LabelSymbol(breakTarget.Name, resultType);
                bodyContext = context
                    .WithScope(context.Scope.AddSymbols([breakTarget, continueTarget]));
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
    protected virtual Expression BindOperator(BindingContext context, OperatorExpression opex)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        var candidates = _symbolListPool.AllocateFromPool();
        try
        {
            var arguments = BindList(context, opex.Arguments);

            GetCandidateOperators(context, opex.Kind, arguments, candidates);

            Symbol? operatorSymbol = null;

            if (candidates.Count == 0)
            {
                diagnostics.Add(SemanticDiagnostics.NoOperatorDefined().WithLocation(opex.Location));
            }
            else
            {
                operatorSymbol = GetBestOperatorCandidate(context, arguments, candidates);
                if (operatorSymbol == null)
                {
                    diagnostics.Add(SemanticDiagnostics.OperatorIsAmbiguous().WithLocation(opex.Location));
                }
                else
                {
                    var parameters = GetSymbolParameters(operatorSymbol);
                    if (parameters.Count != arguments.Count)
                    {
                        diagnostics.Add(SemanticDiagnostics.IncorrectNumberOfOperands().WithLocation(opex.Location));
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

    protected virtual void GetCandidateOperators(BindingContext context, string operatorKind, ImmutableList<Expression> arguments, List<Symbol> candidates)
    {
        candidates.AddRange(context.Symbols.GetOperators(operatorKind));

        var opName = GetOperatorName(operatorKind);
        foreach (var arg in arguments)
        {
            GetMatchingTypeMembers(arg.ResultType, opName, s => s is MemberSymbol ms && ms.IsStatic, candidates);
        }
    }

    protected virtual Symbol? GetBestOperatorCandidate(BindingContext context, ImmutableList<Expression> arguments, List<Symbol> candidates)
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
    protected virtual Expression BindMember(BindingContext context, MemberExpression member)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var expression = BindExpression(context.WithTargetType(null), member.Instance);

            if (expression.ResultType is GroupSymbol)
                diagnostics.Add(SemanticDiagnostics.UnknownName(member.Name).WithLocation(member.Location));

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
        BindingContext context,
        Expression expression,
        string name,
        List<Diagnostic>? diagnositcs)
    {
        var members = _symbolListPool.AllocateFromPool();
        try
        {
            if (expression.ReferencedSymbol is TypeSymbol type)
            {
                // expression was type expression, match static members of that type
                GetMatchingTypeMembers(
                    type,
                    name,
                    s => s is MemberSymbol m && m.IsStatic,
                    members);
            }
            else if (expression.ReferencedSymbol is ContainerSymbol container)
            {
                // expression was namespace (or other non-type container)
                container.GetMembers(
                    name,
                    s => s is MemberSymbol m,
                    members);
            }
            else
            {
                // expression was an instance, match non-static members
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
    /// Binds <see cref="NameExpression"/>
    /// </summary>
    protected virtual Expression BindNameReference(BindingContext context, NameExpression nameRef)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var referencedSymbol = GetNameReference(context, nameRef.Name);

            if (referencedSymbol != null && referencedSymbol == nameRef.ReferencedSymbol)
                return nameRef;

            if (referencedSymbol == null)
                diagnostics.Add(SemanticDiagnostics.UnknownName(nameRef.Name).WithLocation(nameRef.Location));

            var resultType = GetReferenceResultType(context, referencedSymbol) ?? context.Symbols.Object;

            return new NameExpression(
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
    protected virtual Symbol? GetNameReference(BindingContext context, string name)
    {
        var symbols = _symbolListPool.AllocateFromPool();
        try
        {
            this.GetMatchingSymbolsInScope(context.Scope, name, null, symbols);
            return context.Symbols.GetGroup(symbols);
        }
        finally
        {
            _symbolListPool.ReturnToPool(symbols);
        }
    }
    #endregion

    #region NewArray Expression
    /// <summary>
    /// Binds <see cref="NewArrayExpression"/>
    /// </summary>
    protected virtual Expression BindNewArray(BindingContext context, NewArrayExpression newArray)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var targetType = context.TargetType;
            context = context.WithTargetType(null);
            var elementType = newArray.ElementType != null ? BindTypeExpression(context, newArray.ElementType, diagnostics) : null;
            var sizes = BindList(context, newArray.Sizes);
            var values = BindList(context, newArray.Values);

            var targetElementType = targetType is ArraySymbol asym ? asym.ElementType : null;
            var elementTypeSymbol = elementType?.ReferencedSymbol as TypeSymbol
                ?? targetElementType
                ?? GetBestCommonResultType(context, values);

            var dimensions = sizes.Count == 0 ? 1 : sizes.Count;
            var resultType = elementTypeSymbol != null
                ? context.Symbols.GetArray(elementTypeSymbol, dimensions)
                : null;

            if (elementTypeSymbol == null)
            {
                diagnostics.Add(SemanticDiagnostics.CannotInferElementType().WithLocation(newArray.Location));
            }

            if (elementType == newArray.ElementType
                && sizes == newArray.Sizes
                && values == newArray.Values
                && elementTypeSymbol == newArray.ElementTypeSymbol
                && resultType == newArray.ResultType)
                return newArray;

            return new NewArrayExpression(
                elementType,
                sizes,
                values,
                newArray.Location,
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

    #region New Expression
    /// <summary>
    /// Binds <see cref="NewExpression"/>
    /// </summary>
    protected virtual Expression BindNew(BindingContext context, NewExpression nex)
    {
        var candidates = _symbolListPool.AllocateFromPool();
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var argContext = context.WithTargetType(null);

            var type = nex.Type != null ? BindTypeExpression(argContext, nex.Type, diagnostics) : null;
            var arguments = BindList(argContext, nex.Arguments);
            var referencedType = (type?.ReferencedSymbol ?? context.TargetType) as TypeSymbol;

            if (referencedType != null)
                GetConstructorCandidates(context, referencedType, arguments, candidates);

            var location = nex.Location;
            ConstructorSymbol? constructorSymbol = null;

            if (candidates.Count == 0)
            {
                diagnostics.Add(SemanticDiagnostics.NoConstructorFound().WithLocation(location));
            }
            else
            {
                constructorSymbol = GetBestConstructor(candidates);

                if (constructorSymbol == null)
                {
                    diagnostics.Add(SemanticDiagnostics.ConstructorsAreAmbiguous().WithLocation(location));
                }
                else if (constructorSymbol.Parameters.Count != arguments.Count)
                {
                    diagnostics.Add(SemanticDiagnostics.IncorrectNumberOfArguments().WithLocation(location));
                }
                else
                {
                    arguments = ConvertArguments(context, constructorSymbol.Parameters, arguments);
                }
            }

            var resultType = constructorSymbol?.ConstructedType;

            if (type == nex.Type
                && arguments == nex.Arguments
                && constructorSymbol == nex.ConstructorSymbol
                && resultType == nex.ResultType
                && diagnostics.Count == 0)
                return nex;

            return new NewExpression(
                type,
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
        BindingContext context,
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

    #region SymbolReference Expression
    /// <summary>
    /// Binds <see cref="SymbolExpression"/>
    /// </summary>
    protected virtual Expression BindSymbolReference(BindingContext context, SymbolExpression symbolRef)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var referencedSymbol = GetSymbolReference(context, symbolRef.FullName);

            if (referencedSymbol != null && referencedSymbol == symbolRef.ReferencedSymbol)
                return symbolRef;

            if (referencedSymbol == null)
                diagnostics.Add(SemanticDiagnostics.UnknownName(symbolRef.FullName).WithLocation(symbolRef.Location));

            var resultType = GetReferenceResultType(context, referencedSymbol) ?? context.Symbols.Object;

            return new SymbolExpression(
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
    protected virtual Symbol? GetSymbolReference(BindingContext context, string fullName)
    {
        return context.Symbols.GetSymbol<Symbol>(fullName);
    }

    /// <summary>
    /// Determines the result type of a referenced <see cref="Symbol"/>.
    /// </summary>
    protected virtual TypeSymbol? GetReferenceResultType(BindingContext context, Symbol? referencedSymbol) =>
        referencedSymbol switch
        {
            VariableSymbol v => v.Type,
            ParameterSymbol p => p.Type,
            FieldSymbol f => f.Type,
            PropertySymbol p => p.Type,
            IndexerSymbol i => i.ElementType,
            DelegateSymbol f => f,
            GroupSymbol g => g,
            MethodSymbol => context.Symbols.Void,
            TypeSymbol => context.Symbols.Type,
            NamespaceSymbol => SpecialSymbols.Namespace,
            _ => null
        };
    #endregion

    #region This Expression
    /// <summary>
    /// Binds <see cref="ThisExpression"/>
    /// </summary>
    protected virtual Expression BindThis(BindingContext context, ThisExpression me)
    {
        if (me.ResultType != null 
            && me.ResultType == context.DeclaringType)
            return me;

        if (context.DeclaringType != null)
        {
            return new ThisExpression(
                me.Location,
                context.DeclaringType,
                null);
        }
        else
        {
            return new ThisExpression(
                me.Location,
                null,
                [new Diagnostic("No current type in scope.")]);
        }
    }
    #endregion

    #region TypeOf Expression
    /// <summary>
    /// Binds <see cref="TypeOfExpression"/>
    /// </summary>
    protected virtual Expression BindTypeOf(BindingContext context, TypeOfExpression toe)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            var type = BindTypeExpression(context, toe.Type, diagnostics);
            var typeSymbol = type.ReferencedSymbol as TypeSymbol;
            var resultType = context.Symbols.Type;

            if (type == toe.Type
                && typeSymbol == toe.TypeSymbol
                && resultType == toe.ResultType
                && toe.Diagnostics.Count == 0
                && diagnostics.Count == 0)
                return toe;

            return new TypeOfExpression(
                type,
                toe.Location,
                typeSymbol,
                resultType,
                diagnostics.ToImmutableList());
        }
        finally
        {
            _diagnosticListPool.ReturnToPool(diagnostics);
        }
    }
    #endregion

    #region Variable Expression
    /// <summary>
    /// Binds <see cref="VariableExpression"/>
    /// </summary>
    protected virtual Expression BindVariable(BindingContext context, VariableExpression declaration)
    {
        var diagnostics = _diagnosticListPool.AllocateFromPool();
        try
        {
            Expression? variableType = null;
            TypeSymbol? vtype = null;
            Expression? initializer = null;

            if (declaration.VariableType != null)
            {
                variableType = BindTypeExpression(context.WithTargetType(null), declaration.VariableType, diagnostics);
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
                diagnostics.Add(SemanticDiagnostics.DeclarationMustHaveTypeOrInitializer().WithLocation(declaration.Location));
                vtype = context.Symbols.Object;
            }

            if (variableType == declaration.VariableType
                && initializer == declaration.Initializer
                && declaration.VariableSymbol != null
                && declaration.VariableSymbol.Type == vtype
                && declaration.ResultType == vtype)
            {
                return declaration;
            }

            var variable = declaration.VariableSymbol != null
                && declaration.VariableSymbol.Type == vtype
                ? declaration.VariableSymbol
                : new VariableSymbol(declaration.Name, vtype ?? context.Symbols.Object);

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

    #endregion

    #region Misc
    /// <summary>
    /// Gets the matching members of a type
    /// </summary>
    protected virtual void GetMatchingTypeMembers(
        TypeSymbol type, string? name, Func<Symbol, bool>? predicate, List<Symbol> members)
    {
        if (name != null)
        {
            type.GetHierarchyMembers(name, predicate, firstMatchesOnly: true, members);
        }
        else if (predicate != null)
        {
            type.GetHierarchyMembers(predicate, firstMatchesOnly: true, members);
        }
    }

    protected virtual TSymbol? GetFirstMatchingTypeMember<TSymbol>(
        TypeSymbol type, string? name, Func<TSymbol, bool>? predicate)
        where TSymbol: Symbol
    {
        return type.GetFirstHierarchyMember<TSymbol>(name, predicate);
    }

    /// <summary>
    /// Gets the matching symbols in the specified scope.
    /// </summary>
    protected virtual void GetMatchingSymbolsInScope(
        Scope scope, 
        string? name, 
        Func<Symbol, bool>? predicate, 
        List<Symbol> symbols)
    {
        var originalCount = symbols.Count;

        while (scope != null)
        {
            for (int i = scope.Symbols.Count - 1; i >= 0; i--)
            {
                var symbol = scope.Symbols[i];
                if ((name == null || symbol.Name == name)
                    && (predicate == null || predicate(symbol)))
                {
                    symbols.Add(symbol);
                }
            }

            for (int i = scope.Containers.Count - 1; i >= 0; i--)
            {
                var container = scope.Containers[i];
                if (container is TypeSymbol typeSymbol)
                {
                    this.GetMatchingTypeMembers(typeSymbol, name, predicate, symbols);
                }
                else if (name != null)
                {
                    container.GetMembers(name, predicate, symbols);
                }
                else if (predicate != null)
                {
                    container.GetMembers(predicate, symbols);
                }
            }

            if (symbols.Count > originalCount)
                return;

            scope = scope.OuterScope!;
        }
    }

    /// <summary>
    /// Gets the first matching symbol in the specified scope.
    /// </summary>
    protected virtual TSymbol? GetFirstMatchingSymbolInScope<TSymbol>(
        Scope scope, string? name, Func<TSymbol, bool>? predicate)
        where TSymbol : Symbol
    {
        while (scope != null)
        {
            for (int i = scope.Symbols.Count - 1; i >= 0; i--)
            {
                var symbol = scope.Symbols[i];
                if (symbol is TSymbol tsymbol
                    && (name == null || symbol.Name == name)
                    && (predicate == null || predicate(tsymbol)))
                {
                    return tsymbol;
                }
            }

            for (int i = scope.Containers.Count - 1; i >= 0; i--)
            {
                var container = scope.Containers[i];
                if (container is TypeSymbol typeSymbol)
                {
                    var first = this.GetFirstMatchingTypeMember(typeSymbol, name, predicate);
                    if (first != null)
                        return first;
                }
                else
                {
                    var first = container.GetFirstMember(name, predicate);
                    if (first != null)
                        return first;
                }
            }

            scope = scope.OuterScope!;
        }

        return null;
    }

    /// <summary>
    /// Gets the best common result type from a set of expressions.
    /// </summary>
    protected TypeSymbol? GetBestCommonResultType(BindingContext context, IReadOnlyList<Expression> expressions, bool voidIsBetter = false)
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
    protected TypeSymbol? GetBestCommonType(BindingContext context, params TypeSymbol?[] types) =>
        GetBestCommonType(context, (IReadOnlyList<TypeSymbol?>)types);

    /// <summary>
    /// Gets the best common tyep from a set of types, or null if no best common type can be determined.
    /// </summary>
    protected virtual TypeSymbol? GetBestCommonType(
        BindingContext context,
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
            if (TypeEqualityComparer.Instance.Equals(type, best))
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
            type == context.Symbols.Void
            || type == SpecialSymbols.DoesNotReturn;

        bool IgnoreType(TypeSymbol type) =>
            type == SpecialSymbols.Null
            || type == SpecialSymbols.Unknown;
    }
    #endregion

    #region Scope
    /// <summary>
    /// Represents the symbols in scope at particular locations.
    /// </summary>
    public class Scope
    {
        /// <summary>
        /// The immediate outer scope.
        /// </summary>
        public Scope? OuterScope { get; }

        /// <summary>
        /// The symbols visible in this scope.
        /// </summary>
        public ImmutableList<Symbol> Symbols { get; }

        /// <summary>
        /// The containers with members visible in this scope.
        /// </summary>
        public ImmutableList<ContainerSymbol> Containers { get; }

        /// <summary>
        /// Creates a new instance of a scope.
        /// </summary>
        private Scope(
            ImmutableList<Symbol> symbols, 
            ImmutableList<ContainerSymbol> containers,
            Scope? outerScope)
        {
            this.OuterScope = outerScope;
            this.Symbols = symbols;
            this.Containers = containers;
        }

        /// <summary>
        /// An empty scope.
        /// </summary>
        public static readonly Scope Empty =
            new Scope(
                ImmutableList<Symbol>.Empty, 
                ImmutableList<ContainerSymbol>.Empty, 
                null);

        /// <summary>
        /// Creates a new inner scope.
        /// </summary>
        public Scope CreateInnerScope() =>
            new Scope(ImmutableList<Symbol>.Empty, ImmutableList<ContainerSymbol>.Empty, this);

        /// <summary>
        /// Returns a copy of this scope with the new symbol added.
        /// </summary>
        public Scope AddSymbol(Symbol symbol) =>
            new Scope(this.Symbols.Add(symbol), this.Containers, this.OuterScope);

        /// <summary>
        /// Returns a copy of this scope with the new symbols added.
        /// </summary>
        public Scope AddSymbols(IEnumerable<Symbol> symbols) =>
            new Scope(this.Symbols.AddRange(symbols), this.Containers, this.OuterScope);

        /// <summary>
        /// Returns a copy of this scope with the container added,
        /// whose members will be in scope.
        /// </summary>
        public Scope AddContainer(ContainerSymbol container) =>
            new Scope(this.Symbols, this.Containers.Add(container), this.OuterScope);

        /// <summary>
        /// Returns a copy of this scope with the containers added,
        /// whose members will be in scope.
        /// </summary>
        public Scope AddContainers(ImmutableList<ContainerSymbol> containers) =>
            new Scope(this.Symbols, this.Containers.AddRange(containers), this.OuterScope);

        /// <summary>
        /// Returns a copy of this scope with the symbol and its members added to the scope.
        /// </summary>
        public Scope AddSymbolAndMembers(ContainerSymbol container) =>
            new Scope(this.Symbols.Add(container), this.Containers.Add(container), this.OuterScope);
    }
    #endregion

    #region binding contexts

    /// <summary>
    /// Contains useful state for creating symbols for declarations.
    /// </summary>
    protected class SymbolContext
    {
        public SymbolTable Symbols { get; }
        public Scope Scope { get; }

        private SymbolContext(
            SymbolTable symbols,
            Scope scope,
            Dictionary<Declaration, Symbol> declToSymbolMap)
        {
            this.Symbols = symbols;
            this.Scope = scope;
            _declToSymbolMap = declToSymbolMap;
        }

        public SymbolContext(
            SymbolTable symbols,
            Scope scope)
            : this(symbols, scope, new Dictionary<Declaration, Symbol>())
        {
        }

        public virtual SymbolContext WithScope(Scope scope)
        {
            return new SymbolContext(this.Symbols, scope, _declToSymbolMap);
        }

        private Dictionary<Declaration, Symbol> _declToSymbolMap;

        public void AssociateSymbolWithDeclaration(Declaration declaration, Symbol symbol)
        {
            _declToSymbolMap[declaration] = symbol;
        }

        public Symbol? GetSymbolForDeclaration(Declaration declaration)
        {
            _declToSymbolMap.TryGetValue(declaration, out var symbol);
            return symbol;
        }

        private BindingContext? _bindingContext;

        public virtual BindingContext BindingContext
        {
            get
            {
                if (_bindingContext == null)
                {
                    var tmp = new BindingContext(
                        this,
                        null,
                        this.Scope,
                        _declToSymbolMap);
                    Interlocked.CompareExchange(ref _bindingContext, tmp, null);
                }

                return _bindingContext;
            }
        }
    }

    /// <summary>
    /// Contains useful state for binding declarations and expressions.
    /// </summary>
    protected class BindingContext
    {
        /// <summary>
        /// The intial <see cref="SymbolContext"/>
        /// </summary>
        public SymbolContext SymbolContext { get; }

        /// <summary>
        /// All the symbols that can be refered to by the expression.
        /// </summary>
        public SymbolTable Symbols => SymbolContext.Symbols;

        /// <summary>
        /// The declaring type associated with the expression
        /// </summary>
        public TypeSymbol? DeclaringType { get; }

        /// <summary>
        /// The current binding scope.
        /// </summary>
        public Scope Scope { get; }

        /// <summary>
        /// The target type in scope
        /// </summary>
        public TypeSymbol? TargetType { get; }

        private readonly Dictionary<Declaration, Symbol> _declToSymbolMap;
        private readonly Dictionary<LabelExpression, LabelSymbol> _labelToSymbolMap;

        private BindingContext(
            SymbolContext context,
            TypeSymbol? declaringType,
            Scope scope,
            TypeSymbol? targetType,
            Dictionary<Declaration, Symbol>? declToSymbolMap,
            Dictionary<LabelExpression, LabelSymbol>? labelToSymbolMap)
        {
            this.SymbolContext = context;
            this.DeclaringType = declaringType;
            this.Scope = scope;
            this.TargetType = targetType;
            _declToSymbolMap = declToSymbolMap ?? new Dictionary<Declaration, Symbol>();
            _labelToSymbolMap = labelToSymbolMap ?? new Dictionary<LabelExpression, LabelSymbol>();
        }

        internal BindingContext(
            SymbolContext context,
            TypeSymbol? declaringType,
            Scope scope,
            Dictionary<Declaration, Symbol>? declToSymbolMap = null)
            : this(context, declaringType, scope, null, declToSymbolMap, null)
        {
        }

        public bool TryGetSymbolForUnboundDeclaration(Declaration declaration, out Symbol symbol)
        {
            _declToSymbolMap.TryGetValue(declaration, out symbol!);
            return symbol != null;
        }           

        /// <summary>
        /// Creates a new instance with <see cref="DeclaringType"/> assigned.
        /// </summary>
        public virtual BindingContext WithDeclaringType(TypeSymbol? declaringType) =>
            (declaringType == this.DeclaringType) ? this :
            new BindingContext(
                this.SymbolContext, 
                declaringType, 
                this.Scope, 
                this.TargetType,
                _declToSymbolMap,
                _labelToSymbolMap
                );

        /// <summary>
        /// Creates a new instance with <see cref="Scope"/> assigned.
        /// </summary>
        public BindingContext WithScope(Scope scope) =>
            scope == this.Scope ? this :
            new BindingContext(
                this.SymbolContext,
                this.DeclaringType,
                scope,
                this.TargetType,
                _declToSymbolMap,
                _labelToSymbolMap
                );

        /// <summary>
        /// Createsa  new instance with <see cref="TargetType"/> assigned.
        /// </summary>
        public BindingContext WithTargetType(TypeSymbol? targetType) =>
            targetType == this.TargetType ? this :
            new BindingContext(
                this.SymbolContext,
                this.DeclaringType,
                this.Scope,
                targetType,
                _declToSymbolMap,
                _labelToSymbolMap
                );

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
