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

    public static SymbolCache GetOrCreateCache(ImmutableList<Assembly>? assemblies = null)
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
            .Select(t => GetOrCreateSymbol(t, declaringNamespace) is Symbol s ? s : null)
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

    private Symbol? GetSymbol(object runtimeSymbol)
    {
        _symbolMap.TryGetValue(runtimeSymbol, out var symbol);
        return symbol;
    }

    private Symbol? GetOrCreateSymbol(object runtimeSymbol, Symbol? declaringSymbol)
    {
        if (!_symbolMap.TryGetValue(runtimeSymbol, out var symbol))
        {
            var tmp = CreateSymbol(runtimeSymbol, declaringSymbol);
            if (tmp != null)
            {
                symbol = _symbolMap.GetValue(runtimeSymbol, _ => tmp);
            }
        }

        return symbol;

        // create symbol is nested here to keep me from accidentally calling it outside this method
        Symbol? CreateSymbol(object runtimeSymbol, Symbol? declaringSymbol)
        {
            if (declaringSymbol == null)
            {
                if (runtimeSymbol is Type type)
                {
                    declaringSymbol ??=
                        (type.DeclaringType != null) ? GetOrCreateSymbol(type.DeclaringType, null) as MemberSymbol
                        : (type.Namespace != null) ? _globalNamespace.GetFirstSymbolFromPath<NamespaceSymbol>(type.Name)
                        : null;
                }
                else if (runtimeSymbol is MemberInfo member)
                {
                    declaringSymbol ??= (member.DeclaringType != null)
                        ? GetOrCreateSymbol(member.DeclaringType, null) as MemberSymbol
                        : null;
                }
            }

            switch (runtimeSymbol)
            {
                case FieldInfo field:
                    return new FieldSymbol(
                        field.Name,
                        declaringSymbol as TypeSymbol,
                        GetAccess(field),
                        GetModifiers(field),
                        () => GetType(field.FieldType),
                        field);

                case PropertyInfo property:
                    return new PropertySymbol(
                        property.Name,
                        declaringSymbol as TypeSymbol,
                        GetAccess(property),
                        GetModifiers(property),
                        () => GetType(property.PropertyType),
                        fnBackingField: null,
                        property.GetGetMethod() is MethodInfo gmi
                            ? me => (MethodSymbol)GetOrCreateSymbol(gmi, me)!
                            : null,
                        property.GetSetMethod() is MethodInfo smi
                            ? me => (MethodSymbol)GetOrCreateSymbol(smi, me)!
                            : null,
                        property);

                case MethodInfo method:
                    if (method.IsConstructedGenericMethod)
                    {
                        var constructedFrom = (MethodSymbol?)GetOrCreateSymbol(method.GetGenericMethodDefinition(), null);
                        return new MethodSymbol(
                            StripArity(method.Name),
                            declaringSymbol,
                            GetAccess(method),
                            GetModifiers(method),
                            me => ImmutableList<TypeParameterSymbol>.Empty,
                            () => GetTypes(method.GetGenericArguments()),
                            me => CreateParameters(me, method),
                            () => GetType(method.ReturnType),
                            constructedFrom,
                            method);
                    }
                    else if (method.IsGenericMethod)
                    {
                        var typeArgs = method.GetGenericArguments();
                        return new MethodSymbol(
                            StripArity(method.Name),
                            declaringSymbol,
                            GetAccess(method),
                            GetModifiers(method),
                            me => GetTypes(typeArgs).OfType<TypeParameterSymbol>().ToImmutableList(),
                            () => ImmutableList<TypeSymbol>.Empty,
                            me => CreateParameters(me, method),
                            () => GetType(method.ReturnType),
                            null,
                            method);
                    }
                    else
                    {
                        return new MethodSymbol(
                            method.Name,
                            declaringSymbol,
                            GetAccess(method),
                            GetModifiers(method),
                            me => ImmutableList<TypeParameterSymbol>.Empty,
                            () => ImmutableList<TypeSymbol>.Empty,
                            me => CreateParameters(me, method),
                            () => GetType(method.ReturnType),
                            null,
                            method);
                    }

                case ConstructorInfo constructor:
                    return new ConstructorSymbol(
                        declaringSymbol as TypeSymbol,
                        GetAccess(constructor),
                        GetModifiers(constructor),
                        me => CreateParameters(me, constructor),
                        () => (TypeSymbol)declaringSymbol!,
                        constructor);

                case ParameterInfo parameter:
                    return new ParameterSymbol(
                        parameter.Name ?? "",
                        declaringSymbol,
                        () => GetType(parameter.ParameterType),
                        parameter);

                case Type type:
                    var name = type.Name;
                    if (type.IsArray)
                    {
                        var elementType = GetType(type.GetElementType()!);
                        return new ArraySymbol(elementType);
                    }
                    else if (type.IsGenericTypeParameter)
                    {
                        return new TypeParameterSymbol(type.Name, type);
                    }
                    else if (type.IsConstructedGenericType)
                    {
                        var typeDef = GetType(type.GetGenericTypeDefinition());
                        return new TypeSymbol(
                            StripArity(type.Name),
                            declaringSymbol,
                            GetAccess(type),
                            GetModifiers(type),
                            me => ImmutableList<TypeParameterSymbol>.Empty,
                            () => GetTypes(type.GenericTypeArguments),
                            () => GetBaseTypes(type.BaseType, type.GetInterfaces()),
                            _type => CreateMembers(type, (MemberSymbol)_type),
                            typeDef,
                            type);
                    }
                    else if (type.IsGenericType)
                    {
                        var genericArgs = type.GetGenericArguments();
                        return new TypeSymbol(
                            StripArity(type.Name),
                            declaringSymbol,
                            GetAccess(type),
                            GetModifiers(type),
                            me => GetTypes(genericArgs).OfType<TypeParameterSymbol>().ToImmutableList(),
                            () => ImmutableList<TypeSymbol>.Empty,
                            () => GetBaseTypes(type.BaseType, type.GetInterfaces()),
                            me => CreateMembers(type, me),
                            null,
                            type);
                    }
                    else
                    {
                        return new TypeSymbol(
                            type.Name,
                            declaringSymbol,
                            GetAccess(type),
                            GetModifiers(type),
                            me => ImmutableList<TypeParameterSymbol>.Empty,
                            () => ImmutableList<TypeSymbol>.Empty,
                            () => GetBaseTypes(type.BaseType, type.GetInterfaces()),
                            me => CreateMembers(type, me),
                            null,
                            type);
                    }
            }

            return null;
        }
    }

    private static string StripArity(string name)
    {
        var arityStart = name.IndexOf('`');
        if (arityStart > 0)
            return name.Substring(0, arityStart);
        return name;
    }

    public TypeSymbol GetType(Type type)
    {
        if (type == typeof(void))
            return SpecialSymbols.Void;

        if (GetSymbol(type) is TypeSymbol cached)
            return cached;

        if (FindSymbol(type) is TypeSymbol found)
            return found;

        return (TypeSymbol)GetOrCreateSymbol(type, null)!;
    }

    private ImmutableList<Symbol> CreateMembers(Type runtimeType, MemberSymbol? container) =>
        runtimeType.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(m => GetOrCreateSymbol(m, container) is Symbol s ? s : null)
            .Where(s => s != null)
            .ToImmutableList()!;

    private ImmutableList<ParameterSymbol> CreateParameters(MemberSymbol? declaringSymbol, MethodBase method) =>
        method.GetParameters().Select(p => (ParameterSymbol)GetOrCreateSymbol(p, declaringSymbol)!).ToImmutableList();

    private ImmutableList<TypeSymbol> GetBaseTypes(Type? baseType, Type[] interfaces)
    {
        if (baseType != null)
        {
            if (interfaces == null || interfaces.Length == 0)
                return [GetType(baseType)];

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

    private Symbol? FindSymbol(object runtimeSymbol)
    {
        if (runtimeSymbol is Type type && type.FullName != null)
        {
            return _globalNamespace.GetFirstSymbolFromPath<TypeSymbol>(type.FullName);
        }
        else if (runtimeSymbol is MemberInfo member && member.DeclaringType != null)
        {
            var dottedName = member.DeclaringType.FullName + "." + member.Name;
            return _globalNamespace.GetFirstSymbolFromPath(dottedName);
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
