using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Parkour.Symbols;

public class RuntimeSymbols
{
    public SymbolCache Symbols { get; }
    public GlobalNamespaceSymbol GlobalNamespace => Symbols.GlobalNamespace;
    public ImmutableList<Assembly> Assemblies { get; }

    private RuntimeSymbols(GlobalNamespaceSymbol globalNamespace, ImmutableList<Assembly> assemblies)
    {
        Symbols = SymbolCache.From(globalNamespace);
        Assemblies = assemblies;
    }

    /// <summary>
    /// The <see cref="RuntimeSymbols"/> associated with the current mscorlib assembly.
    /// </summary>
    public static RuntimeSymbols CurrentMscorlib => 
        GetOrCreate(_mscorlibAssemblies);

    private static ImmutableList<Assembly> _mscorlibAssemblies =
        [typeof(int).Assembly];

    private static readonly ConditionalWeakTable<ImmutableList<Assembly>, RuntimeSymbols> _assemblyToRuntimeSymbolsMap =
        new ConditionalWeakTable<ImmutableList<Assembly>, RuntimeSymbols>();

    private static readonly ConditionalWeakTable<NamespaceSymbol, RuntimeSymbols> _namespaceToRuntimeSymbolsMap =
        new ConditionalWeakTable<NamespaceSymbol, RuntimeSymbols>();

    /// <summary>
    /// Gets or creates the <see cref="RuntimeSymbols"/> associated with the assemblies
    /// </summary>
    public static RuntimeSymbols GetOrCreate(ImmutableList<Assembly> assemblies)
    {
        if (!_assemblyToRuntimeSymbolsMap.TryGetValue(assemblies, out var runtimeSymbols))
        {
            runtimeSymbols = _assemblyToRuntimeSymbolsMap.GetValue(assemblies, CreateRuntimeSymbols);
            _namespaceToRuntimeSymbolsMap.TryAdd(runtimeSymbols.GlobalNamespace, runtimeSymbols);
        }

        return runtimeSymbols;
    }

    /// <summary>
    /// Get the <see cref="RuntimeSymbols"/> with the specified global namespace instance.
    /// </summary>
    public static bool TryGet(GlobalNamespaceSymbol globalNamespace, [NotNullWhen(true)] out RuntimeSymbols? runtimeSymbols)
    {
        return _namespaceToRuntimeSymbolsMap.TryGetValue(globalNamespace, out runtimeSymbols);
    }

    private static RuntimeSymbols CreateRuntimeSymbols(ImmutableList<Assembly>? assemblies)
    {
        assemblies = assemblies ?? _mscorlibAssemblies;
        var types = assemblies.SelectMany(a => a.GetTypes()).ToList();
        var modules = assemblies.SelectMany(a => a.GetModules()).ToList();
        var methods = modules.SelectMany(m => m.GetMethods()).ToList();
        var fields = modules.SelectMany(m => m.GetFields()).ToList();

        RuntimeSymbols? runtimeSymbols = null;

        var ns = new GlobalNamespaceSymbol(_ns =>
        {
            return runtimeSymbols!.GetNamespaceMembers(_ns, "", "", types, methods, fields);
        });

        runtimeSymbols = new RuntimeSymbols(ns, assemblies);
        return runtimeSymbols;
    }

    private ImmutableList<Symbol> GetNamespaceMembers(
        NamespaceSymbol? declaringNamespace,
        string containingNamespace,
        string namespaceName,
        IEnumerable<Type> types,
        IEnumerable<MethodInfo>? methods,
        IEnumerable<FieldInfo>? fields)
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
            .Select(t => GetOrCreateSymbol(t, declaringNamespace))
            .OfType<Symbol>()
            .ToList();

        if (methods != null)
        {
            symbolsInNamespace.AddRange(
                methods.Select(m => GetOrCreateSymbol(m, declaringNamespace))
                .OfType<Symbol>());
        }

        if (fields != null)
        {
            symbolsInNamespace.AddRange(
                fields.Select(f => GetOrCreateSymbol(f, declaringNamespace))
                .OfType<Symbol>());
        }

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
                _ns => GetNamespaceMembers(_ns, namespaceFullName, ng.Key, ng, null, null)));

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

    private readonly ConditionalWeakTable<object, Symbol> _runtimeInfoToSymbolMap =
        new ConditionalWeakTable<object, Symbol>();

    private readonly ConditionalWeakTable<Symbol, object> _symbolToRuntimeInfoMap =
        new ConditionalWeakTable<Symbol, object>();

    private Symbol? GetSymbol(object runtimeSymbol)
    {
        _runtimeInfoToSymbolMap.TryGetValue(runtimeSymbol, out var symbol);
        return symbol;
    }

    private Symbol? GetOrCreateSymbol(object runtimeSymbol, Symbol? declaringSymbol)
    {
        if (!_runtimeInfoToSymbolMap.TryGetValue(runtimeSymbol, out var symbol))
        {
            var tmp = CreateSymbol(runtimeSymbol, declaringSymbol);
            if (tmp != null)
            {
                symbol = _runtimeInfoToSymbolMap.GetValue(runtimeSymbol, _ => tmp);
                _symbolToRuntimeInfoMap.TryAdd(symbol, runtimeSymbol);
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
                        : (type.Namespace != null) ? this.GlobalNamespace.GetFirstSymbolFromPath<NamespaceSymbol>(type.Name)
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
                        () => GetType(field.FieldType));

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
                            : null);

                case MethodInfo method:
                    return CreateMethod(method, declaringSymbol);

                case ConstructorInfo constructor:
                    return new ConstructorSymbol(
                        (TypeSymbol)declaringSymbol!,
                        GetAccess(constructor),
                        GetModifiers(constructor),
                        me => CreateParameters(me, constructor));

                case ParameterInfo parameter:
                    return new ParameterSymbol(
                        parameter.Name ?? "",
                        declaringSymbol,
                        () => GetType(parameter.ParameterType));

                case Type type:
                    if (type.IsArray)
                    {
                        var elementType = GetType(type.GetElementType()!);
                        return new ArraySymbol(elementType);
                    }
                    else if (type.IsGenericTypeParameter)
                    {
                        return new TypeParameterSymbol(type.Name);
                    }
                    else
                    {
                        return CreateType(type, declaringSymbol);
                    }
            }

            return null;       
        }

        MethodSymbol CreateMethod(MethodInfo method, Symbol? declaringSymbol)
        {
            Func<MethodSymbol, ImmutableList<TypeParameterSymbol>> fnTypeParameters =
                method.IsGenericMethod && !method.IsConstructedGenericMethod
                    ? me => GetTypes(method.GetGenericArguments()).OfType<TypeParameterSymbol>().ToImmutableList()
                    : me => ImmutableList<TypeParameterSymbol>.Empty;

            Func<ImmutableList<TypeSymbol>> fnTypeArguments =
                method.IsConstructedGenericMethod
                    ? () => GetTypes(method.GetGenericArguments())
                    : () => ImmutableList<TypeSymbol>.Empty;

            var constructedFrom = method.IsConstructedGenericMethod
                ? (MethodSymbol?)GetOrCreateSymbol(method.GetGenericMethodDefinition(), null)
                : null;

            return new MethodSymbol(
                StripArity(method.Name),
                declaringSymbol,
                GetAccess(method),
                GetModifiers(method),
                fnTypeParameters,
                fnTypeArguments,
                me => CreateParameters(me, method),
                () => GetType(method.ReturnType),
                constructedFrom);
        }

        TypeSymbol CreateType(Type type, Symbol? declaringSymbol)
        {
            var access = GetAccess(type);
            var modifiers = GetModifiers(type);

            Func<TypeSymbol, ImmutableList<TypeParameterSymbol>> fnTypeParameters =
                type.IsGenericType && !type.IsConstructedGenericType
                    ? me => GetTypes(type.GetGenericArguments()).OfType<TypeParameterSymbol>().ToImmutableList()
                    : me => ImmutableList<TypeParameterSymbol>.Empty;

            Func<ImmutableList<TypeSymbol>> fnTypeArguments =
                type.IsConstructedGenericType
                    ? () => GetTypes(type.GenericTypeArguments)
                    : () => ImmutableList<TypeSymbol>.Empty;

            var contructedFromType = type.IsConstructedGenericType
                ? GetType(type.GetGenericTypeDefinition())
                : null;

            Func<ImmutableList<TypeSymbol>> fnBaseTypes = 
                () => GetBaseTypes(type.BaseType, type.GetInterfaces());

            Func<TypeSymbol, ImmutableList<Symbol>> fnMembers = 
                me => CreateMembers(type, me);

            if (type.IsInterface)
            {
                return new InterfaceSymbol(
                    type.Name,
                    declaringSymbol,
                    access,
                    modifiers,
                    fnTypeParameters,
                    fnTypeArguments,
                    fnBaseTypes,
                    fnMembers,
                    contructedFromType);
            }
            else if (type.IsValueType)
            {
                return new ValueTypeSymbol(
                    type.Name,
                    declaringSymbol,
                    access,
                    modifiers,
                    fnTypeParameters,
                    fnTypeArguments,
                    fnBaseTypes,
                    fnMembers,
                    contructedFromType);
            }
            else
            {
                return new TypeSymbol(
                    type.Name,
                    declaringSymbol,
                    access,
                    modifiers,
                    fnTypeParameters,
                    fnTypeArguments,
                    fnBaseTypes,
                    fnMembers,
                    contructedFromType);
            }
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

    public static IReadOnlyList<MemberInfo> GetMembers(Type runtimeType) =>
        runtimeType.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

    private ImmutableList<Symbol> CreateMembers(Type runtimeType, MemberSymbol? container) =>
        GetMembers(runtimeType)
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
        if (runtimeSymbol is Type type)
        {
            return this.GlobalNamespace.GetFirstSymbolFromPath<TypeSymbol>(GetFullName(type));
        }
        else if (runtimeSymbol is MemberInfo member)
        {
            return this.GlobalNamespace.GetFirstSymbolFromPath(GetFullName(member));
        }

        return null;
    }

    private ImmutableList<TypeSymbol> GetTypes(IEnumerable<Type> types)
    {
        if (!types.Any())
            return ImmutableList<TypeSymbol>.Empty;
        return types.Select(GetType).ToImmutableList();
    }

    /// <summary>
    /// Gets the full name of a symbol compatible with searching for that symbol
    /// from the global namespace.
    /// </summary>
    private static string GetFullName(object runtimeInfo)
    {
        if (runtimeInfo is Type type)
        {
            if (type.DeclaringType != null)
            {
                return GetFullName(type.DeclaringType) + "." + type.Name;
            }
            else if (type.Namespace != null)
            {
                return type.Namespace + "." + type.Name;
            }
            else
            {
                return type.Name;
            }
        }
        else if (runtimeInfo is MemberInfo member)
        {
            if (member.DeclaringType != null)
            {
                return GetFullName(member.DeclaringType) + "." + member.Name;
            }
            else
            {
                return member.Name;
            }
        }
        else
        {
            throw new InvalidOperationException($"Unhandled runtime info: '{runtimeInfo.GetType().Name}' in {nameof(RuntimeSymbols)}.{nameof(GetFullName)}");
        }
    }

    /// <summary>
    /// Tries to get the runtime info (Type, MemberInfo, ParameterInfo) associated with the <see cref="Symbol"/>.
    /// </summary>
    public bool TryGetRuntimeInfo(Symbol symbol, [NotNullWhen(true)] out object? runtimeInfo) =>
        TryGetRuntimeInfo(symbol, out runtimeInfo, null);

    /// <summary>
    /// Tries to get the runtime info (Type, MemberInfo, ParameterInfo) associated with the <see cref="Symbol"/>.
    /// </summary>
    public bool TryGetRuntimeInfo(Symbol symbol, [NotNullWhen(true)] out object? runtimeInfo, Func<Symbol, object?>? alternateSource)
    {
        if (alternateSource?.Invoke(symbol) is object altInfo)
        {
            runtimeInfo = altInfo;
            return true;
        }

        if (symbol is TypeSymbol typeSymbol
            && TryGetRuntimeType(typeSymbol, out var runtimeType))
        {
            runtimeInfo = runtimeType;
            return true;
        }
        else if (symbol is MemberSymbol memberSymbol
            && TryGetRuntimeMember(memberSymbol, out var memberInfo))
        {
            runtimeInfo = memberInfo;
            return true;
        }
        else if (symbol is ParameterSymbol parameterSymbol
            && parameterSymbol.DeclaringSymbol is MemberSymbol declaringMemberSymbol
            && TryGetRuntimeMember(declaringMemberSymbol, out var declaringMemberInfo)
            && declaringMemberInfo is MethodBase declaringMethodBase)
        {
            var index = declaringMemberSymbol switch
            {
                MethodSymbol ms => ms.Parameters.IndexOf(parameterSymbol),
                ConstructorSymbol cs => cs.Parameters.IndexOf(parameterSymbol),
                _ => -1
            };

            var parameterInfos = declaringMethodBase.GetParameters();
            if (index >= 0 && index < parameterInfos.Length)
            {
                runtimeInfo = parameterInfos[index];
                return true;
            }
        }

        runtimeInfo = null;
        return false;
    }

    /// <summary>
    /// Tries to get the <see cref="Type"/> associated with the <see cref="TypeSymbol"/>
    /// </summary>
    public bool TryGetRuntimeType(TypeSymbol typeSymbol, [NotNullWhen(true)] out Type? runtimeType) =>
        TryGetRuntimeType(typeSymbol, out runtimeType, null);

    /// <summary>
    /// Tries to get the <see cref="Type"/> associated with the <see cref="TypeSymbol"/>
    /// </summary>
    public bool TryGetRuntimeType(TypeSymbol typeSymbol, [NotNullWhen(true)] out Type? runtimeType, Func<Symbol, object?>? alternateSource)
    {
        if (alternateSource?.Invoke(typeSymbol) is Type altType)
        {
            runtimeType = altType;
            return true;
        }

        if (_symbolToRuntimeInfoMap.TryGetValue(typeSymbol, out var runtimeInfo)
            && runtimeInfo is Type rt)
        {
            runtimeType = rt;
            return true;
        }

        if (typeSymbol == SpecialSymbols.Void
            || typeSymbol == SpecialSymbols.DoesNotReturn)
        {
            runtimeType = typeof(void);
            return true;
        }
        else if (typeSymbol == SpecialSymbols.Any
            || typeSymbol == SpecialSymbols.Null
            || typeSymbol == SpecialSymbols.Unknown)
        {
            runtimeType = typeof(object);
            return true;
        }

        if (typeSymbol is ArraySymbol array
            && TryGetRuntimeType(array.ElementType, out var elementType, alternateSource))
        {
            runtimeType = elementType.MakeArrayType();
            return true;
        }
        else if (typeSymbol is FunctionSymbol lambda
            && TryGetRuntimeTypes(lambda.Parameters.Select(p => p.ParameterType), out var parameterTypes, alternateSource)
            && TryGetRuntimeType(lambda.ReturnType, out var returnType, alternateSource))
        {
            Type[] types = [.. parameterTypes, returnType];
            runtimeType = System.Linq.Expressions.Expression.GetDelegateType(types);
            return true;
        }
        else if (typeSymbol.ConstructedFrom != null
            && TryGetRuntimeType(typeSymbol.ConstructedFrom, out var typeDef, alternateSource)
            && TryGetRuntimeTypes(typeSymbol.TypeArguments, out var typeArgs, alternateSource))
        {
            runtimeType = typeDef.MakeGenericType(typeArgs.ToArray());
            return true;
        }

        // find type via declaring type
        if (typeSymbol.DeclaringSymbol is TypeSymbol declaringTypeSymbol
            && TryGetRuntimeType(declaringTypeSymbol, out var declaringType))
        {
            // assume member index in current symbol is same as index in runtime type's members
            // (since using same function to fetch them)
            var index = declaringTypeSymbol.Members.IndexOf(typeSymbol);
            if (index >= 0)
            {
                var members = GetMembers(declaringType);
                if (members[index] is Type mt)
                {
                    runtimeType = mt;
                    return true;
                }
            }
        }

        runtimeType = null;
        return false;
    }

    /// <summary>
    /// Tries to get the list of <see cref="Type"/> associated with the list of <see cref="TypeSymbol"/>
    /// </summary>
    private bool TryGetRuntimeTypes(IEnumerable<TypeSymbol> typeSymbols, [NotNullWhen(true)] out IReadOnlyList<Type>? types, Func<Symbol, object?>? alternateSource)
    {
        var list = new List<Type>();

        foreach (var typeSymbol in typeSymbols)
        {
            if (!TryGetRuntimeType(typeSymbol, out var rt, alternateSource))
            {
                types = null;
                return false;
            }

            list.Add(rt);
        }

        types = list;
        return true;
    }

    /// <summary>
    /// Tries to get the <see cref="MemberInfo"/> associated with the <see cref="MemberSymbol"/>
    /// </summary>
    public bool TryGetRuntimeMember(MemberSymbol memberSymbol, [NotNullWhen(true)] out MemberInfo? memberInfo) =>
        TryGetRuntimeMember(memberSymbol, out memberInfo, null);

    /// <summary>
    /// Tries to get the <see cref="MemberInfo"/> associated with the <see cref="MemberSymbol"/>
    /// </summary>
    private bool TryGetRuntimeMember(MemberSymbol memberSymbol, [NotNullWhen(true)] out MemberInfo? memberInfo, Func<Symbol, object?>? alternateSource)
    {
        if (alternateSource?.Invoke(memberSymbol) is MemberInfo ami)
        {
            memberInfo = ami;
            return true;
        }

        if (_symbolToRuntimeInfoMap.TryGetValue(memberSymbol, out var runtimeInfo)
            && runtimeInfo is MemberInfo mi)
        {
            memberInfo = mi;
            return true;
        }

        if (memberSymbol is TypeSymbol typeSymbol
            && TryGetRuntimeType(typeSymbol, out var runtimeType, alternateSource))
        {
            memberInfo = runtimeType;
            return true;

        }
        else if (memberSymbol is MethodSymbol methodSymbol
            && methodSymbol.ConstructedFrom != null
            && TryGetRuntimeMember(methodSymbol.ConstructedFrom, out var methodDef, alternateSource)
            && methodDef is MethodInfo methodInfo
            && TryGetRuntimeTypes(methodSymbol.TypeArguments, out var typeArgs, alternateSource))
        {
            memberInfo = methodInfo.MakeGenericMethod(typeArgs.ToArray());
            return true;
        }

        // find member via declaring type
        if (memberSymbol.DeclaringSymbol is TypeSymbol declaringTypeSymbol
            && TryGetRuntimeType(declaringTypeSymbol, out var declaringType, alternateSource))
        {
            // assume member index in current symbol is same as index in runtime type's members
            // (since using same function to fetch them)
            var index = declaringTypeSymbol.Members.IndexOf(memberSymbol);
            if (index >= 0)
            {
                var members = GetMembers(declaringType);
                if (members[index] is MemberInfo minfo)
                {
                    memberInfo = minfo;
                    return true;
                }
            }
        }

        memberInfo = null;
        return false;
    }
}
