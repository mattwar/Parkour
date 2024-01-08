using System.Reflection;
using System.Runtime.CompilerServices;

namespace Parkour.Analysis;
using Symbols;

public class RuntimeSymbols
{
    private ConditionalWeakTable<object, Symbol> _runtimeToSymbolMap =
        new ConditionalWeakTable<object, Symbol>();

    private RuntimeSymbols()
    {
    }

    private static readonly ConditionalWeakTable<ImmutableList<Assembly>, SymbolTree> _map =
        new ConditionalWeakTable<ImmutableList<Assembly>, SymbolTree>();

    public static CommonSymbols GetOrCreateCommonSymbols(ImmutableList<Assembly>? assemblies = null)
    {
        return CommonSymbols.From(GetOrCreateGlobalNamespace(assemblies));
    }

    public static NamespaceSymbol GetOrCreateGlobalNamespace(ImmutableList<Assembly>? assemblies = null)
    {
        assemblies ??= _defaultAssemblies;

        if (!_map.TryGetValue(assemblies, out var symbolTree))
        {
            symbolTree = _map.GetValue(assemblies, _assemblies => 
                new SymbolTree(tree => 
                    CreateGlobalNamespace(tree, _assemblies)));
        }

        return symbolTree.GlobalNamespace;
    }

    private static NamespaceSymbol CreateGlobalNamespace(SymbolTree tree, ImmutableList<Assembly>? assemblies)
    {
        assemblies = assemblies ?? _defaultAssemblies;
        var types = assemblies.SelectMany(a => a.GetTypes()).ToList();
        var gns = new RuntimeSymbols().CreateNamespace("", "", types);
        return gns;
    }

    private static ImmutableList<Assembly> _defaultAssemblies =
        ImmutableList.Create(typeof(int).Assembly);

    /// <summary>
    /// Gets a namespace symbol containing the types and namespaces as members.
    /// </summary>
    private NamespaceSymbol CreateNamespace(
        string containingNamespace, 
        string name, 
        IReadOnlyList<Type> types)
    {
        if (name == "" && containingNamespace != "")
            throw new InvalidOperationException($"Name missing from nested namespace");

        return new NamespaceSymbol(name, () =>
        {
            var list = new List<Symbol>();

            var namespaceFullName = containingNamespace.Length > 0 && name.Length > 0
                ? containingNamespace + "." + name
                : name;
            var namespaceFullNameWithDot = namespaceFullName.Length > 0 ? namespaceFullName + "." : namespaceFullName;

            var typesInNamespace = types
                .Where(t => t.Namespace == namespaceFullName)
                .ToList();

            list.AddRange(typesInNamespace.Select(t => GetType(t)));

            var nestedTypes = types
                .Where(t => t.Namespace != null && t.Namespace.Contains(namespaceFullNameWithDot))
                .ToList();

            var nsGroups = nestedTypes
                .Where(t => t.Namespace != null && t.Namespace.Length > 0)
                .GroupBy(t => GetNextNamespaceName(t.Namespace!, namespaceFullName))
                .Where(g => g.Key.Length > 0) // remove non-namespaces
                .ToList();

            var nestedNamespaces = nsGroups.Select(ng => CreateNamespace(namespaceFullName, ng.Key, ng.ToList())).ToList();

            list.AddRange(nestedNamespaces);

            return list.ToImmutableList();
        });

        static string GetNextNamespaceName(string fullName, string containingNamespace)
        {
            var start = containingNamespace.Length == 0 ? 0 : containingNamespace.Length + 1;
            var nextDot = fullName.IndexOf('.', start);
            if (nextDot > 0)
                return fullName.Substring(start, nextDot - start);
            return fullName.Substring(start);
        }
    }

    public TypeSymbol GetType(Type type) =>
        (TypeSymbol)GetOrCreateSymbol(type);

    private Symbol GetOrCreateSymbol(object runtimeSymbol)
    {
        if (runtimeSymbol == (object)typeof(void))
            return CommonSymbols.Void;

        if (!_runtimeToSymbolMap.TryGetValue(runtimeSymbol, out var symbol))
        {
            TryCreateSymbol(runtimeSymbol, out symbol);
        }

        return symbol;
    }

    public static SymbolAccess GetAccess(MemberInfo info) =>
        info switch
        {
            System.Type type =>
                type.IsPublic ? SymbolAccess.Public
                    : type.IsNestedPublic ? SymbolAccess.Public
                    : type.IsNestedFamily ? SymbolAccess.Protected
                    : type.IsNestedFamANDAssem ? SymbolAccess.ProtectedAndInternal
                    : type.IsNestedFamORAssem ? SymbolAccess.ProtectedOrInternal
                    : type.IsPublic ? SymbolAccess.Public
                    : type.IsNotPublic ? SymbolAccess.Internal
                    : SymbolAccess.Private,
            FieldInfo field =>
                field.IsPublic ? SymbolAccess.Public
                    : field.IsAssembly ? SymbolAccess.Internal
                    : field.IsFamily ? SymbolAccess.Protected
                    : field.IsFamilyAndAssembly ? SymbolAccess.ProtectedAndInternal
                    : field.IsFamilyOrAssembly ? SymbolAccess.ProtectedOrInternal
                    : SymbolAccess.Private,
            PropertyInfo property =>
                GetAccess(property.GetGetMethod()!),
            MethodBase method =>
                method.IsPublic ? SymbolAccess.Public
                    : method.IsAssembly ? SymbolAccess.Internal
                    : method.IsFamily ? SymbolAccess.Protected
                    : method.IsFamilyAndAssembly ? SymbolAccess.ProtectedAndInternal
                    : method.IsFamilyOrAssembly ? SymbolAccess.ProtectedOrInternal
                    : SymbolAccess.Private,
            _ => SymbolAccess.Private
        };

    public static SymbolModifier GetModifiers(MemberInfo info) =>
        info switch
        {
            System.Type type =>
                (type.IsAbstract ? SymbolModifier.Abstract : SymbolModifier.None)
                | (type.IsSealed ? SymbolModifier.Sealed : SymbolModifier.None),
            FieldInfo field =>
                (field.IsStatic ? SymbolModifier.Static : SymbolModifier.None),
            PropertyInfo property =>
                GetModifiers(property.GetGetMethod()!),
            MethodBase method =>
                (method.IsStatic ? SymbolModifier.Static : SymbolModifier.None)
                | (method.IsAbstract ? SymbolModifier.Abstract : SymbolModifier.None)
                | (method.IsVirtual ? SymbolModifier.Virtual : SymbolModifier.None)
                | (method.IsFinal ? SymbolModifier.Sealed : SymbolModifier.None)
                | (method.IsHideBySig ? SymbolModifier.HideBySig : SymbolModifier.None)
                | (method.IsSpecialName ? SymbolModifier.Special : SymbolModifier.None),
            _ => SymbolModifier.None
        };

    private ImmutableList<ParameterSymbol> GetParameters(MethodBase method) =>
        method.GetParameters().Select(p => GetParameter(p)).ToImmutableList();

    private ParameterSymbol GetParameter(ParameterInfo p) =>
        new ParameterSymbol(
            p.Name ?? "",
            () => GetType(p.ParameterType)
            );

    private bool TryCreateSymbol(object runtimeSymbol, out Symbol symbol)
    {
        symbol = null!;

        switch (runtimeSymbol)
        {
            case FieldInfo field:
                symbol = new FieldSymbol(
                    field.Name,
                    field.DeclaringType != null ? GetType(field.DeclaringType) : null,
                    GetAccess(field),
                    GetModifiers(field),
                    GetType(field.FieldType),
                    field);
                break;

            case PropertyInfo property:
                symbol = new PropertySymbol(
                    property.Name,
                    property.DeclaringType != null ? GetType(property.DeclaringType) : null,
                    GetAccess(property),
                    GetModifiers(property),
                    GetType(property.PropertyType),
                    property);
                break;

            case MethodInfo method:
                symbol = new MethodSymbol(
                    method.Name,
                    method.DeclaringType != null ? GetType(method.DeclaringType) : null,
                    GetAccess(method),
                    GetModifiers(method),
                    GetParameters(method),
                    GetType(method.ReturnType),
                    method);
                break;

            case ConstructorInfo constructor:
                symbol = new ConstructorSymbol(
                    constructor.DeclaringType != null ? GetType(constructor.DeclaringType) : null,
                    GetAccess(constructor),
                    GetModifiers(constructor),
                    GetParameters(constructor),
                    constructor.DeclaringType != null ? GetType(constructor.DeclaringType) : null,
                    constructor);
                break;

            case System.Type type:
                if (type.IsClass || type.IsValueType || type.IsInterface)
                {
                    symbol = new TypeSymbol(
                        type.Name,
                        type.DeclaringType != null ? GetType(type.DeclaringType) : null,
                        GetAccess(type),
                        GetModifiers(type),
                        () => GetBaseTypes(type.BaseType, type.GetInterfaces()),
                        _ => CreateMembers(type),
                        type);
                }
                else if (type.IsArray && type.GetElementType() is System.Type elementType)
                {
                    symbol = new ArraySymbol(GetType(elementType));
                }
                break;
        }

        if (symbol != null)
        {
            var tmp = symbol;
            symbol = _runtimeToSymbolMap.GetValue(runtimeSymbol, _ => tmp);
        }

        return symbol != null;
    }

    private ImmutableList<TypeSymbol> GetBaseTypes(Type? baseType, Type[] interfaces)
    {
        if (baseType != null)
        {
            if (interfaces == null || interfaces.Length == 0)
                return ImmutableList.Create(GetType(baseType));
            return GetTypes(new[] { baseType }.Concat(interfaces));
        }
        else if (interfaces.Length > 0)
        {
            return GetTypes(interfaces);
        }
        else
        {
            return ImmutableList<TypeSymbol>.Empty;
        }
    }

    private ImmutableList<TypeSymbol> GetTypes(IEnumerable<Type> types) =>
        types.Select(t => GetType(t)).ToImmutableList();

    private ImmutableList<Symbol> CreateMembers(System.Type runtimeType) =>
        runtimeType.GetMembers(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.DeclaredOnly)
            .Select(m => TryCreateSymbol(m, out var s) ? s : null)
            .Where(s => s != null)
            .ToImmutableList()!;
}
