namespace Parkour.Analysis;

using Expressions;
using Symbols;
using Syntax;

public class DeclarationCompilation : SyntaxCompilation
{
    public override ImmutableList<SyntaxTree> SyntaxTrees { get; }
    public override NamespaceSymbol GlobalNamespace { get; }
    public ImmutableList<Declaration> Declarations { get; }

    private DeclarationCompilation(
        ImmutableList<Declaration> declarations, 
        NamespaceSymbol globalNamespace)
    {
        this.Declarations = declarations;
        this.GlobalNamespace = globalNamespace;
        this.SyntaxTrees = declarations.Select(d => d.Syntax?.Tree).OfType<SyntaxTree>().Distinct().ToImmutableList();
    }

    public static DeclarationCompilation Create(
        ImmutableList<Declaration> declarations,
        ImmutableList<NamespaceSymbol> imports)
    {
        var combinedNamespace = CombinedSymbols.CreateCombinedGlobalNamespace(
            combined =>
        {
            var declarationNs = new SymbolBuilder(combined).Build(declarations);
            return imports.Append(declarationNs).ToImmutableList();
        });

        return new DeclarationCompilation(declarations, combinedNamespace);
    }

    public static DeclarationCompilation Create(
        ImmutableList<Declaration> declarations,
        params NamespaceSymbol[] imports)
    {
        return Create(declarations, imports.ToImmutableList());
    }

    public override ImmutableList<Diagnostic> GetDiagnostics()
    {
        return ImmutableList<Diagnostic>.Empty;
    }

    private class SymbolBuilder
    {
        private readonly NamespaceSymbol _globalNamespace;
        private ExpressionBinder? _binder;
        private BindingScope _scope;

        public SymbolBuilder(NamespaceSymbol globals)
        {
            _globalNamespace = globals;
            _scope = new BindingScope().AddNamespace(globals);
        }

        public NamespaceSymbol Build(ImmutableList<Declaration> declarations)
        {
            return new NamespaceSymbol("", null, ns =>
            {
                var globalNamespaceMembers = declarations.SelectMany(d =>
                    d is NamespaceDeclaration nd && nd.Name == ""
                        ? (IEnumerable<Declaration>)nd.Declarations
                        : new[] { d })
                    .ToImmutableList();

                return CombineMembers(ns, globalNamespaceMembers);
            });
        }

        /// <summary>
        /// returns the symbols for the declarations, 
        /// with same named namespace declarations combined into a single namespace symbol.
        /// </summary>
        private ImmutableList<Symbol> CombineMembers(
            NamespaceSymbol container,
            IEnumerable<Declaration> members)
        {
            var newMembers = new List<Symbol>();

            var namespaceMemberGroups = members
                .OfType<NamespaceDeclaration>()
                .GroupBy(d => d.Name)
                .ToList();

            var newMemberNamespaces = namespaceMemberGroups
                .Select(g => new NamespaceSymbol(g.Key, container, _ns => CombineMembers(_ns, g.SelectMany(n => n.Declarations))))
                .ToList();

            newMembers.AddRange(newMemberNamespaces);

            var otherMembers = members
                .Where(d => !(d is NamespaceDeclaration))
                .ToList();

            var otherMemberSymbols =
                otherMembers
                .Select(d => CreateSymbol(null, d))
                .OfType<Symbol>()
                .ToList();

            newMembers.AddRange(otherMemberSymbols);

            return newMembers.ToImmutableList();
        }

        private Symbol? CreateSymbol(MemberSymbol? container, Declaration declaration)
        {
            switch (declaration)
            {
                case ClassDeclaration cd:
                    return CreateClassSymbol(container, cd);

                case MethodDeclaration md:
                    return CreateMethodSymbol((TypeSymbol)container!, md);

                case ParameterDeclaration pd:
                    return CreateParameterSymbol(container!, pd);

                case FieldDeclaration fd:
                    return CreateFieldSymbol((TypeSymbol)container!, fd);

                case PropertyDeclaration pd:
                    return CreatePropertySymbol((TypeSymbol)container!, pd);

                default:
                    return null;
            }
        }

        private TypeSymbol CreateClassSymbol(MemberSymbol? container, ClassDeclaration cd)
        {
            return new TypeSymbol(
                cd.Name,
                container,
                cd.Access,
                cd.Modifiers,
                fnTypeParameters: null,
                () => cd.BaseTypes.Select(bt => GetType(bt)).ToImmutableList()!,
                _container => cd.Declarations.Select(d => CreateSymbol(_container, d)).Where(s => s != null).ToImmutableList()!,
                genericDefinition: null,
                runtimeType: null);
        }

        private MethodSymbol CreateMethodSymbol(MemberSymbol declaringSymbol, MethodDeclaration md)
        {
            return new MethodSymbol(
                md.Name,
                declaringSymbol,
                md.Access,
                md.Modifiers,
                fnTypeParameters: null,
                _container => md.Parameters.Select(p => CreateParameterSymbol(_container, p)).ToImmutableList()!,
                () => GetType(md.ReturnType),
                fnGenericDefinition: null,
                runtimeMethod: null
                );
        }

        private ParameterSymbol CreateParameterSymbol(Symbol declaringSymbol, ParameterDeclaration pd)
        {
            return new ParameterSymbol(
                pd.Name,
                declaringSymbol,
                () => pd.ParameterType != null ? GetType(pd.ParameterType) : CommonSymbols.Any,
                runtimeParameter: null
                );
        }

        private FieldSymbol CreateFieldSymbol(TypeSymbol declaringType, FieldDeclaration fd)
        {
            return new FieldSymbol(
                fd.Name,
                declaringType,
                fd.Access,
                fd.Modifiers,
                () => GetType(fd.FieldType),
                runtimeField: null
                );
        }

        private PropertySymbol CreatePropertySymbol(TypeSymbol declaringType, PropertyDeclaration pd)
        {
            return new PropertySymbol(
                pd.Name,
                declaringType,
                pd.Access,
                pd.Modifiers,
                () => GetType(pd.PropertyType),
                me => CreateMethodSymbol(me, pd.GetMethod),
                me => pd.SetMethod != null ? CreateMethodSymbol(me, pd.SetMethod) : null,
                runtimeProperty: null
            );
        }

        private ExpressionBinder GetBinder()
        {
            if (_binder == null)
            {
                var tmp = new ExpressionBinder(_globalNamespace, _scope);
                Interlocked.CompareExchange(ref _binder, tmp, null);
            }

            return _binder;
        }

        private TypeSymbol GetType(Expression typeExpression) =>
            GetBinder().BindType(typeExpression, null) ?? CommonSymbols.Unknown;
    }
}
