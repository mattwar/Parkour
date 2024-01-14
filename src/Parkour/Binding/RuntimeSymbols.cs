using System.Reflection;
using System.Runtime.CompilerServices;

namespace Parkour.Binding;
using Symbols;

public class RuntimeSymbols
{
    private NamespaceSymbol _globalNamespace;

    private RuntimeSymbols(NamespaceSymbol globalNamespace)
    {
        _globalNamespace = globalNamespace;
    }

    private static NamespaceSymbol? _defaultNamespace;
    public static NamespaceSymbol DefaultGlobalNamespace =>
        _defaultNamespace ??= GetOrCreateGlobalNamespace();

    private static readonly ConditionalWeakTable<ImmutableList<Assembly>, NamespaceSymbol> _map =
        new ConditionalWeakTable<ImmutableList<Assembly>, NamespaceSymbol>();

    private static ImmutableList<Assembly> _defaultAssemblies =
        [typeof(int).Assembly];

    public static NamespaceSymbol GetOrCreateGlobalNamespace(ImmutableList<Assembly>? assemblies = null)
    {
        assemblies ??= _defaultAssemblies;

        if (!_map.TryGetValue(assemblies, out var ns))
        {
            ns = _map.GetValue(assemblies, CreateGlobalNamespace);
        }

        return ns;
    }

    public static SymbolCache GetOrCreateCommonSymbols(ImmutableList<Assembly>? assemblies = null)
    {
        return SymbolCache.From(GetOrCreateGlobalNamespace(assemblies));
    }

    private static NamespaceSymbol CreateGlobalNamespace(ImmutableList<Assembly>? assemblies)
    {
        assemblies = assemblies ?? _defaultAssemblies;
        var types = assemblies.SelectMany(a => a.GetTypes()).ToList();

        return new NamespaceSymbol("", null, _ns =>
        {
            return new RuntimeSymbols(_ns).GetNamespaceMembers(_ns, "", "", types);
        });
    }

    private ImmutableList<Symbol> GetNamespaceMembers(
        NamespaceSymbol? declaringNamespace, 
        string containingNamespace, 
        string namespaceName, 
        IEnumerable<Type> types)
    {
        var list = new List<Symbol>();

        var namespaceFullName = containingNamespace.Length > 0 && namespaceName.Length > 0
            ? containingNamespace + "." + namespaceName
            : namespaceName;

        var namespaceFullNameWithDot = namespaceFullName.Length > 0 ? namespaceFullName + "." : namespaceFullName;

        var typesInNamespace = types
            .OfType<Type>()
            .Where(t => t.Namespace == namespaceFullName)
            .ToList();

        var symbolsInNamespace = typesInNamespace
            .Select(t => CreateSymbol(t, declaringNamespace) is Symbol s ? s : null)
            .OfType<Symbol>()
            .ToList();

        list.AddRange(symbolsInNamespace);

        var nestedTypes = types
            .OfType<Type>()
            .Where(t => t.Namespace != null && t.Namespace.Contains(namespaceFullNameWithDot))
            .ToList();

        var nsGroups = nestedTypes
            .Where(t => t.Namespace != null && t.Namespace.Length > 0)
            .GroupBy(t => GetNextNamespaceName(t.Namespace!, namespaceFullName))
            .Where(g => g.Key.Length > 0) // remove non-namespaces
            .ToList();

        var nestedNamespaces = nsGroups.Select(ng =>
            new NamespaceSymbol(
                ng.Key,
                declaringNamespace,
                _ns => GetNamespaceMembers(_ns, namespaceFullName, ng.Key, ng)));

        list.AddRange(nestedNamespaces);

        return list.ToImmutableList();
    }

    private static string GetNextNamespaceName(string fullName, string containingNamespace)
    {
        var start = containingNamespace.Length == 0 ? 0 : containingNamespace.Length + 1;
        var nextDot = fullName.IndexOf('.', start);
        if (nextDot > 0)
            return fullName.Substring(start, nextDot - start);
        return fullName.Substring(start);
    }

    private readonly ConditionalWeakTable<object, Symbol> _symbolMap =
        new ConditionalWeakTable<object, Symbol>();

    private Symbol GetOrCreateSymbol(object runtimeSymbol, MemberSymbol? container)
    {
        return GetSymbol(runtimeSymbol)
            ?? CreateSymbol(runtimeSymbol, container)
            ?? SpecialSymbols.Unknown;
    }

    private Symbol? GetSymbol(object runtimeSymbol)
    {
        _symbolMap.TryGetValue(runtimeSymbol, out var symbol);
        return symbol;
    }

    private Symbol? CreateSymbol(object runtimeSymbol, MemberSymbol? container)
    {
        Symbol? symbol = null;

        if (container == null)
        {
            if (runtimeSymbol is Type type)
            {
                container ??=
                    (type.DeclaringType != null) ? GetOrCreateSymbol(type.DeclaringType, null) as MemberSymbol
                    : (type.Namespace != null) ? _globalNamespace.GetFirstSymbolFromPath<NamespaceSymbol>(type.Name)
                    : null;
            }
            else if (runtimeSymbol is MemberInfo member)
            {
                container ??= (member.DeclaringType != null)
                    ? GetOrCreateSymbol(member.DeclaringType, null) as MemberSymbol
                    : null;
            }
        }

        switch (runtimeSymbol)
        {
            case FieldInfo field:
                symbol = new FieldSymbol(
                    field.Name,
                    container as TypeSymbol,
                    GetAccess(field),
                    GetModifiers(field),
                    () => GetType(field.FieldType),
                    field);
                break;

            case PropertyInfo property:
                symbol = new PropertySymbol(
                    property.Name,
                    container as TypeSymbol,
                    GetAccess(property),
                    GetModifiers(property),
                    () => GetType(property.PropertyType),
                    fnBackingField: null,
                    me => (MethodSymbol)CreateSymbol(property.GetGetMethod()!, me)!,
                    property.GetSetMethod() != null
                        ? me => (MethodSymbol)CreateSymbol(property.GetSetMethod()!, me)!
                        : null,
                    property);
                break;

            case MethodInfo method:
                symbol = new MethodSymbol(
                    method.Name,
                    container,
                    GetAccess(method),
                    GetModifiers(method),
                    () => GetTypes(method.GetGenericArguments()),
                    me => CreateParameters(me, method),
                    () => GetType(method.ReturnType),
                    method.IsConstructedGenericMethod 
                        ? () => (MethodSymbol)CreateSymbol(
                            method.GetGenericMethodDefinition(), 
                            GetType(method.GetGenericMethodDefinition().DeclaringType!))!
                        : null,
                    method);
                break;

            case ConstructorInfo constructor:
                symbol = new ConstructorSymbol(
                    container as TypeSymbol,
                    GetAccess(constructor),
                    GetModifiers(constructor),
                    me => CreateParameters(me, constructor),
                    constructor);
                break;

            case ParameterInfo parameter:
                symbol = new ParameterSymbol(
                    parameter.Name ?? "",
                    container,
                    () => GetType(parameter.ParameterType),
                    parameter);
                break;

            case Type type:
                var name = type.Name;
                if (type.IsConstructedGenericType)
                {
                    var typeDef = GetType(type.GetGenericTypeDefinition());
                    symbol = new TypeSymbol(
                        type.Name,
                        container,
                        GetAccess(type),
                        GetModifiers(type),
                        () => GetTypes(type.GenericTypeArguments),
                        () => GetBaseTypes(type.BaseType, type.GetInterfaces()),
                        _type => CreateMembers(type, (MemberSymbol)_type),
                        typeDef,
                        type);
                }
                else if (type.IsArray)
                {
                    var elementType = GetType(type.GetElementType()!);
                    symbol = new ArraySymbol(elementType);
                }
                else if (type.IsGenericTypeParameter)
                {
                    symbol = new TypeParameterSymbol(type.Name, type);
                }
                else
                {
                    symbol = new TypeSymbol(
                        type.Name,
                        container,
                        GetAccess(type),
                        GetModifiers(type),
                        () => GetTypes(type.GenericTypeArguments),
                        () => GetBaseTypes(type.BaseType, type.GetInterfaces()),
                        me => CreateMembers(type, me),
                        null,
                        type);
                }
                break;
        }

        if (symbol != null)
            return _symbolMap.GetValue(runtimeSymbol, _ => symbol);

        return null;
    }

    private ImmutableList<Symbol> CreateMembers(Type runtimeType, MemberSymbol? container) =>
        runtimeType.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(m => CreateSymbol(m, container) is Symbol s ? s : null)
            .Where(s => s != null)
            .ToImmutableList()!;

    private ImmutableList<ParameterSymbol> CreateParameters(MemberSymbol? declaringSymbol, MethodBase method) =>
        method.GetParameters().Select(p => (ParameterSymbol)CreateSymbol(p, declaringSymbol)!).ToImmutableList();

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

    public TypeSymbol GetType(Type type)
    {
        if (type == typeof(void))
            return SpecialSymbols.Void;

        if (GetSymbol(type) is TypeSymbol cached)
            return cached;

        if (FindSymbol(type) is TypeSymbol found)
            return found;

        return (TypeSymbol)GetOrCreateSymbol(type, null);
    }

    private Symbol? FindSymbol(object runtimeSymbol)
    {
        if (runtimeSymbol is Type type)
        {
            var foundType = _globalNamespace.GetFirstSymbolFromPath<TypeSymbol>(type.FullName!);
            if (foundType != null)
                return _symbolMap.GetValue(runtimeSymbol, _ => foundType);
        }
        else if (runtimeSymbol is MemberInfo member && member.DeclaringType != null)
        {
            var dottedName = member.DeclaringType.FullName + "." + member.Name;
            var foundMember = _globalNamespace.GetFirstSymbolFromPath(dottedName);
            if (foundMember != null)
                return _symbolMap.GetValue(runtimeSymbol, _ => foundMember);
        }

        return null;
    }

    private ImmutableList<TypeSymbol> GetTypes(IEnumerable<Type> types)
    {
        if (!types.Any())
            return ImmutableList<TypeSymbol>.Empty;
        return types.Select(GetType).ToImmutableList();
    }
}
