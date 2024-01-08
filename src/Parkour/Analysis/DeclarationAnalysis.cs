namespace Parkour.Analysis;

using Expressions;
using Symbols;

public class DeclarationAnalysis
{
    public ImmutableList<NamespaceDeclaration> Declarations { get; }
    public NamespaceSymbol GlobalNamespace { get; }

    private DeclarationAnalysis(
        ImmutableList<NamespaceDeclaration> declarations, 
        NamespaceSymbol globalNamespace)
    {
        this.Declarations = declarations;
        this.GlobalNamespace = globalNamespace;
    }

    public static DeclarationAnalysis Analyze(
        ImmutableList<NamespaceDeclaration> declarations,
        params NamespaceSymbol[] imports)
    {
        var combinedNamespace = CombinedSymbols.CreateCombinedGlobalNamespace(symbols =>
        {
            var declarationNs = new DeclarationBinder(symbols).Bind(declarations);
            return imports.ToImmutableList().Append(declarationNs);
        });

        return new DeclarationAnalysis(declarations, combinedNamespace);
    }

    private class DeclarationBinder
    {
        private readonly CommonSymbols _symbols;
        private readonly ExpressionBinder _binder;
        private BindingScope _scope;

        public DeclarationBinder(CommonSymbols symbols)
        {
            _symbols = symbols;
            _scope = new BindingScope().AddNamespace(symbols.GlobalNamespace);
            _binder = new ExpressionBinder(symbols, _scope);
        }

        public NamespaceSymbol Bind(ImmutableList<NamespaceDeclaration> declarations)
        {
            return CreateNamespace("", declarations);
        }

        /// <summary>
        /// Gets a namespace symbol containing the types and namespaces as members.
        /// </summary>
        private NamespaceSymbol CreateNamespace(
            string name,
            IReadOnlyList<NamespaceDeclaration> declarations)
        {
            var nestedNamespaceGroups = declarations.SelectMany(d => d.Declarations.OfType<NamespaceDeclaration>()).GroupBy(d => d.Name).ToList();
            var nestedDeclarations = declarations.SelectMany(d => d.Declarations.OfType<ClassDeclaration>()).ToList();

            return new NamespaceSymbol(name, () =>
            {
                var list = new List<Symbol>();
                foreach (var nd in nestedDeclarations)
                {
                    if (CreateSymbol(null, nd) is Symbol s)
                        list.Add(s);
                }

                foreach (var group in nestedNamespaceGroups)
                {
                    var ns = CreateNamespace(group.Key, group.ToList());
                    list.Add(ns);
                }

                return list.ToImmutableList();
            });
        }

        private Symbol? CreateSymbol(Symbol? container, Declaration declaration)
        {
            switch (declaration)
            {
                case ClassDeclaration cd:
                    return CreateClassSymbol(container, cd);

                case MethodDeclaration md:
                    return CreateMethodSymbol(container, md);

                case ParameterDeclaration pd:
                    return CreateParameterSymbol(container, pd);

                case FieldDeclaration fd:
                    return CreateFieldSymbol(container, fd);

                case PropertyDeclaration pd:
                    return CreatePropertySymbol(container, pd);

                default:
                    return null;
            }
        }

        private TypeSymbol CreateClassSymbol(Symbol? container, ClassDeclaration cd)
        {
            return new TypeSymbol(
                cd.Name,
                container,
                cd.Access,
                cd.Modifiers,
                () => cd.BaseTypes.Select(bt => GetType(bt)).ToImmutableList()!,
                _container => cd.Declarations.Select(d => CreateSymbol(_container, d)).Where(s => s != null).ToImmutableList()!);
        }

        private MethodSymbol CreateMethodSymbol(Symbol? container, MethodDeclaration md)
        {
            return new MethodSymbol(
                md.Name,
                container,
                md.Access,
                md.Modifiers,
                _container => md.Parameters.Select(p => CreateParameterSymbol(_container, p)).ToImmutableList()!,
                () => GetType(md.ReturnType)
                );
        }

        private ParameterSymbol CreateParameterSymbol(Symbol? container, ParameterDeclaration pd)
        {
            return new ParameterSymbol(
                pd.Name,
                () => pd.ParameterType != null ? GetType(pd.ParameterType) : CommonSymbols.Any
                );
        }

        private FieldSymbol CreateFieldSymbol(Symbol? container, FieldDeclaration fd)
        {
            return new FieldSymbol(
                fd.Name,
                container,
                fd.Access,
                fd.Modifiers,
                () => GetType(fd.FieldType)
                );
        }

        private PropertySymbol CreatePropertySymbol(Symbol? container, PropertyDeclaration pd)
        {
            return new PropertySymbol(
                pd.Name,
                container,
                pd.Access,
                pd.Modifiers,
                () => GetType(pd.PropertyType),
                _container => CreateMethodSymbol(_container, pd.GetMethod),
                _container => pd.SetMethod != null ? CreateMethodSymbol(_container, pd.SetMethod) : null
                );
        }

        private TypeSymbol GetType(Expression typeExpression) =>
            _binder.BindType(typeExpression, null) ?? CommonSymbols.Unknown;
    }
}

public class CombinedSymbols
{
    public static NamespaceSymbol CreateCombinedGlobalNamespace(
        Func<CommonSymbols, ImmutableList<NamespaceSymbol>> globalNamespaces)
    {
        throw new NotImplementedException();
    }
}
