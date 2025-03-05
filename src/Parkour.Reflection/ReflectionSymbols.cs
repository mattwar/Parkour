using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Parkour.Reflection;

using Symbols;

/// <summary>
/// A <see cref="SymbolTable"/> from <see cref="System.Reflection"/> metadata objects.
/// </summary>
public class ReflectionSymbols : StandardSymbolTable
{
    /// <summary>
    /// The set of assemblies that define the set of types and namespaces.
    /// </summary>
    public ImmutableList<Assembly> Assemblies { get; }

    /// <summary>
    /// Map from Reflection types to symbols.
    /// </summary>
    private readonly ConditionalWeakTable<object, Symbol> _infoToSymbolMap =
        new ConditionalWeakTable<object, Symbol>();

    /// <summary>
    /// Map from symbols to reflection types.
    /// </summary>
    private readonly ConditionalWeakTable<Symbol, object> _symbolToInfoMap =
        new ConditionalWeakTable<Symbol, object>();

    /// <summary>
    /// Constructs a new <see cref="ReflectionSymbols"/> instance.
    /// </summary>
    private ReflectionSymbols(GlobalNamespaceSymbol globalNamespace, ImmutableList<Assembly> assemblies)
        : base(globalNamespace)
    {
        Assemblies = assemblies;
    }

    /// <summary>
    /// Gets or creates the <see cref="ReflectionSymbols"/> associated with the assemblies
    /// </summary>
    public static ReflectionSymbols GetOrCreate(ImmutableList<Assembly> assemblies)
    {
        if (!_assembliesToSymbolTableMap.TryGetValue(assemblies, out var runtimeSymbols))
        {
            runtimeSymbols = _assembliesToSymbolTableMap.GetValue(assemblies, CreateSymbolTable);
            _namespaceToSymbolTableMap.TryAdd(runtimeSymbols.GlobalNamespace, runtimeSymbols);
        }

        return runtimeSymbols;
    }

    private static readonly ConditionalWeakTable<ImmutableList<Assembly>, ReflectionSymbols> _assembliesToSymbolTableMap =
        new ConditionalWeakTable<ImmutableList<Assembly>, ReflectionSymbols>();

    private static readonly ConditionalWeakTable<GlobalNamespaceSymbol, ReflectionSymbols> _namespaceToSymbolTableMap =
        new ConditionalWeakTable<GlobalNamespaceSymbol, ReflectionSymbols>();


    #region Create Symbols
    /// <summary>
    /// Creates a new <see cref="ReflectionSymbols"/> for the given assemblies.
    /// </summary>
    private static ReflectionSymbols CreateSymbolTable(ImmutableList<Assembly> assemblies)
    {
        var types = assemblies.SelectMany(a => a.GetTypes()).ToList();
        var modules = assemblies.SelectMany(a => a.GetModules()).ToList();
        var methods = modules.SelectMany(m => m.GetMethods()).ToList();
        var fields = modules.SelectMany(m => m.GetFields()).ToList();

        ReflectionSymbols? runtimeSymbols = null;

        runtimeSymbols = new ReflectionSymbols(
            new GlobalNamespaceSymbol(_ns => 
                runtimeSymbols!.CreateNamespaceMembers(_ns, "", "", types, methods, fields)), 
            assemblies);

        return runtimeSymbols;
    }

    /// <summary>
    /// Gets a list of all symbols contained in the namespace.
    /// </summary>
    private ImmutableList<Symbol> CreateNamespaceMembers(
        NamespaceSymbol declaringNamespace,
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
            .Select(t => CreateSymbol(t, declaringNamespace))
            .OfType<Symbol>()
            .ToList();

        if (methods != null)
        {
            symbolsInNamespace.AddRange(
                methods.Select(m => CreateSymbol(m, declaringNamespace))
                .OfType<Symbol>());
        }

        if (fields != null)
        {
            symbolsInNamespace.AddRange(
                fields.Select(f => CreateSymbol(f, declaringNamespace))
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
                _ns => CreateNamespaceMembers(_ns, namespaceFullName, ng.Key, ng, null, null)));

        list.AddRange(nestedNamespaces);

        return list.ToImmutableList();

        string GetNextNamespaceName(string fullName, string containingNamespace)
        {
            var start = containingNamespace.Length == 0 ? 0 : containingNamespace.Length + 1;
            var nextDot = fullName.IndexOf('.', start);
            if (nextDot > 0)
                return fullName.Substring(start, nextDot - start);
            return fullName.Substring(start);
        }

        /// <summary>
        /// Gets or creates the symbol associated with the reflection object.
        /// </summary>
        Symbol? CreateSymbol(object runtimeSymbol, Symbol declaringSymbol)
        {
            if (!_infoToSymbolMap.TryGetValue(runtimeSymbol, out var symbol))
            {
                var tmp = Create();
                if (tmp != null)
                {
                    symbol = _infoToSymbolMap.GetValue(runtimeSymbol, _ => tmp);
                    _symbolToInfoMap.TryAdd(symbol, runtimeSymbol);
                }
            }

            return symbol;

            Symbol? Create()
            {
                switch (runtimeSymbol)
                {
                    case ConstructorInfo constructor:
                        return new ConstructorSymbol(
                            (TypeSymbol)declaringSymbol!,
                            GetAccess(constructor),
                            GetModifiers(constructor),
                            me => CreateParameters(constructor, me),
                            me => CreateAttributeInfoList(constructor.GetCustomAttributesData())
                            );

                    case FieldInfo field:
                        return new FieldSymbol(
                            field.Name,
                            declaringSymbol,
                            GetAccess(field),
                            GetModifiers(field),
                            () => GetTypeSymbol(field.FieldType),
                            me => CreateAttributeInfoList(field.GetCustomAttributesData()),
                            field.IsLiteral ? field.GetRawConstantValue() : null);

                    case MethodInfo method:
                        var name = StripArity(method.Name);
                        return new MethodSymbol(
                            name,
                            declaringSymbol,
                            GetAccess(method),
                            GetModifiers(method),
                            fnTypeParameters:
                                method.IsGenericMethodDefinition
                                    ? me => CreateTypeParameters(method.GetGenericArguments())
                                    : me => ImmutableList<TypeParameterSymbol>.Empty,
                            fnTypeArguments:
                                () => ImmutableList<TypeSymbol>.Empty,
                            me => CreateParameters(method, me),
                            () => GetTypeSymbol(method.ReturnType),
                            me => CreateAttributeInfoList(method.GetCustomAttributesData()),
                            me => GetInterfaceMethods(method) is { } methods && methods.Count > 0
                                ? methods.Select(m => (MethodSymbol?)GetSymbolFromInfo(m)).OfType<MethodSymbol>().ToImmutableList()
                                : ImmutableList<MethodSymbol>.Empty
                            );

                    case ParameterInfo parameter:
                        return new ParameterSymbol(
                            parameter.Name ?? "",
                            declaringSymbol,
                            GetModifiers(parameter),
                            () => GetTypeSymbol(parameter.ParameterType),
                            me => CreateAttributeInfoList(parameter.GetCustomAttributesData())
                            );

                    case PropertyInfo property:
                        var indexParameters = property.GetIndexParameters();
                        if (indexParameters.Length > 0)
                        {
                            return new IndexerSymbol(
                                property.Name,
                                declaringSymbol as TypeSymbol,
                                GetAccess(property),
                                GetModifiers(property),
                                () => GetTypeSymbol(property.PropertyType),
                                property.GetGetMethod() is MethodInfo gmi
                                    ? (IndexerSymbol me) => (MethodSymbol)CreateSymbol(gmi, me)!
                                    : null,
                                property.GetSetMethod() is MethodInfo smi
                                    ? (IndexerSymbol me) => (MethodSymbol)CreateSymbol(smi, me)!
                                    : null,
                                me => CreateAttributeInfoList(property.GetCustomAttributesData())
                                    );
                        }
                        else
                        {
                            return new PropertySymbol(
                                property.Name,
                                declaringSymbol as TypeSymbol,
                                GetAccess(property),
                                GetModifiers(property),
                                () => GetTypeSymbol(property.PropertyType),
                                fnBackingField: null,
                                property.GetGetMethod() is MethodInfo gmi
                                    ? (PropertySymbol me) => (MethodSymbol)CreateSymbol(gmi, me)!
                                    : null,
                                property.GetSetMethod() is MethodInfo smi
                                    ? (PropertySymbol me) => (MethodSymbol)CreateSymbol(smi, me)!
                                    : null,
                                me => CreateAttributeInfoList(property.GetCustomAttributesData())
                                );
                        }

                    case Type type:
                        if (type.IsGenericTypeParameter)
                        {
                            return new TypeParameterSymbol(type.Name);
                        }
                        else
                        {
                            name = StripArity(type.Name);
                            var access = GetAccess(type);
                            var modifiers = GetModifiers(type);

                            Func<TypeSymbol, ImmutableList<TypeParameterSymbol>> fnTypeParameters =
                                type.IsGenericTypeDefinition
                                    ? me => CreateTypeParameters(type.GetGenericArguments())
                                    : me => ImmutableList<TypeParameterSymbol>.Empty;

                            Func<ImmutableList<TypeSymbol>> fnTypeArguments =
                                    () => ImmutableList<TypeSymbol>.Empty;

                            Func<ImmutableList<TypeSymbol>> fnBaseTypes =
                                () => GetBaseTypes(type.BaseType, type.GetInterfaces());

                            Func<TypeSymbol, ImmutableList<Symbol>> fnMembers =
                                me => CreateMembers(type, me);

                            Func<TypeSymbol, ImmutableList<AttributeInfo>> fnAttributes =
                                me => CreateAttributeInfoList(type.GetCustomAttributesData());

                            if (type.IsInterface)
                            {
                                return new InterfaceSymbol(
                                    name,
                                    declaringSymbol,
                                    access,
                                    modifiers,
                                    fnTypeParameters,
                                    fnTypeArguments,
                                    fnBaseTypes,
                                    fnMembers,
                                    fnAttributes,
                                    definition: null
                                    );
                            }
                            else if (type.IsValueType)
                            {
                                return new StructSymbol(
                                    name,
                                    declaringSymbol,
                                    access,
                                    modifiers,
                                    fnTypeParameters,
                                    fnTypeArguments,
                                    fnBaseTypes,
                                    fnMembers,
                                    fnAttributes
                                    );
                            }
                            else
                            {
                                return new ClassSymbol(
                                    name,
                                    declaringSymbol,
                                    access,
                                    modifiers,
                                    fnTypeParameters,
                                    fnTypeArguments,
                                    fnBaseTypes,
                                    fnMembers,
                                    fnAttributes
                                    );
                            }
                        }
                }

                return null;
            }
        }

        ImmutableList<Symbol> CreateMembers(Type runtimeType, MemberSymbol declaringSymbol) =>
            GetMemberInfos(runtimeType)
                .Select(m => CreateSymbol(m, declaringSymbol))
                .Where(s => s != null)
                .ToImmutableList()!;

        ImmutableList<ParameterSymbol> CreateParameters(MethodBase method, MemberSymbol declaringSymbol) =>
            method.GetParameters()
            .Select(p => (ParameterSymbol)CreateSymbol(p, declaringSymbol)!)
            .ToImmutableList();

        ImmutableList<TypeParameterSymbol> CreateTypeParameters(IEnumerable<Type> typeParameters) =>
            typeParameters.Select(tp => new TypeParameterSymbol(tp.Name)).ToImmutableList();

        ImmutableList<TypeSymbol> GetBaseTypes(Type? baseType, Type[] interfaces)
        {
            if (baseType != null)
            {
                if (interfaces == null || interfaces.Length == 0)
                    return [GetTypeSymbol(baseType)];

                return GetTypeSymbols(new[] { baseType }.Concat(interfaces));
            }
            else if (interfaces.Length > 0)
            {
                return GetTypeSymbols(interfaces);
            }
            else
            {
                return ImmutableList<TypeSymbol>.Empty;
            }
        }

        ImmutableList<AttributeInfo> CreateAttributeInfoList(IEnumerable<CustomAttributeData> data) =>
            data.Select(d => CreateAttributeInfo(d))
                .OfType<AttributeInfo>()
                .ToImmutableList();

        AttributeInfo? CreateAttributeInfo(CustomAttributeData data)
        {
            var constructor = (ConstructorSymbol?)GetSymbolFromInfo(data.Constructor);
            if (constructor != null)
            {
                var arguments = data.ConstructorArguments
                    .Select((a, i) => new AttributeArgument(constructor.Parameters[i], CreateValue(a.Value)))
                    .ToImmutableList();
                var members = data.NamedArguments
                    .Select(n => new AttributeMember((MemberSymbol)GetSymbolFromInfo(n.MemberInfo)!, CreateValue(n.TypedValue.Value)))
                    .ToImmutableList();
                return new AttributeInfo(constructor, arguments, members);
            }
            return null;

            AttributeValue CreateValue(object? value)
            {
                if (value is Type type)
                {
                    return new AttributeTypeValue(GetTypeSymbol(type));
                }
                else if (value is Array array)
                {
                    var elementType = GetTypeSymbol(array.GetType().GetElementType()!);
                    var values = array.OfType<object>().Select(v => CreateValue(v)).ToImmutableList();
                    return new AttributeArrayValue(elementType, values);
                }
                else
                {
                    return new AttributeConstantValue(value);
                }
            }
        }
    }

    private static SymbolAccess GetAccess(MemberInfo info) =>
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

    private static BitSet<SymbolModifier> GetModifiers(MemberInfo info) =>
        info switch
        {
            System.Type type =>
                (type.IsAbstract ? SymbolModifier.Abstract : SymbolModifier.None)
                | (type.IsSealed ? SymbolModifier.Sealed : SymbolModifier.None),
            FieldInfo field =>
                (field.IsStatic ? SymbolModifier.Static : SymbolModifier.None)
                | (field.IsLiteral ? SymbolModifier.Constant : SymbolModifier.None),
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

    private static BitSet<SymbolModifier> GetModifiers(ParameterInfo info)
    {
        var isIn = (info.Attributes & ParameterAttributes.In) != 0;
        var isOut = (info.Attributes & ParameterAttributes.Out) != 0;
        return isIn && isOut ? SymbolModifier.Ref
            : isIn ? SymbolModifier.In
            : isOut ? SymbolModifier.Out
            : SymbolModifier.None;
    }

    #endregion

    #region Get Symbol from Reflection info
    /// <summary>
    /// Gets the <see cref="TypeSymbol"/> for the corresponding reflection type.
    /// </summary>
    public override bool TryGetTypeSymbol(Type type, [NotNullWhen(true)] out TypeSymbol? typeSymbol)
    {
        typeSymbol = GetSymbolFromInfo(type) as TypeSymbol;
        return typeSymbol != null;
    }

    /// <summary>
    /// Gets the symbol corresponding to the reflection info.
    /// </summary>
    private Symbol? GetSymbolFromInfo(object reflectionInfo)
    {
        if (_infoToSymbolMap.TryGetValue(reflectionInfo, out var symbol))
            return symbol;

        if (reflectionInfo is MemberInfo member)
        {
            if (member is Type type)
            {
                if (type.IsConstructedGenericType)
                {
                    var definition = GetTypeSymbol(type.GetGenericTypeDefinition())!;
                    var typeArgs = GetTypeSymbols(type.GetGenericArguments());
                    return this.GetConstructed(definition, typeArgs);
                }
                else if (type.IsGenericTypeParameter && type.DeclaringType != null)
                {
                    var declaringSymbol = GetSymbolFromInfo(type.DeclaringType);
                    return FindMember(declaringSymbol!, type);
                }
                else if (type.IsGenericMethodParameter && type.DeclaringMethod != null)
                {
                    var declaringSymbol = GetSymbolFromInfo(type.DeclaringMethod);
                    return FindMember(declaringSymbol!, type);
                }
                else if (type.IsArray)
                {
                    var elementSymbol = GetTypeSymbol(type.GetElementType()!);
                    return GetArray(elementSymbol, type.GetArrayRank());
                }
            }
            else if (member is MethodInfo method && method.IsConstructedGenericMethod)
            {
                var definition = (MethodSymbol)GetSymbolFromInfo(method.GetGenericMethodDefinition())!;
                var typeArgs = GetTypeSymbols(method.GetGenericArguments());
                return this.GetConstructed<MethodSymbol>(definition, typeArgs);
            }

            if (member.DeclaringType != null)
            {
                var declaringSymbol = GetSymbolFromInfo(member.DeclaringType);
                if (declaringSymbol != null)
                {
                    return FindMember(declaringSymbol, member);
                }
            }
        }
        else if (reflectionInfo is ParameterInfo parameter 
            && parameter.Member != null)
        {
            var memberSymbol = GetSymbolFromInfo(parameter.Member);

            if (memberSymbol is MethodSymbol method)
            {
                return method.Parameters[parameter.Position];
            }
            else if (memberSymbol is ConstructorSymbol constructor)
            {
                return constructor.Parameters[parameter.Position];
            }
            else if (memberSymbol is IndexerSymbol indexer && indexer.GetMethod != null)
            {
                return indexer.GetMethod.Parameters[parameter.Position];
            }
        }

        // nothing else worked, try dotted path as last resort
        var path = GetDottedPath(reflectionInfo);
        return GetSymbol(path);
    }

    /// <summary>
    /// Finds the member of the symbol matching the reflection info.
    /// </summary>
    private Symbol? FindMember(Symbol declaringSymbol, MemberInfo member)
    {
        if (declaringSymbol is TypeSymbol declaringType)
        {
            switch (member)
            {
                case ConstructorInfo constructor:
                    var parameterTypes = constructor.GetParameters().Select(p => GetTypeSymbol(p.ParameterType)).ToImmutableList();
                    return declaringType.GetFirstMember<ConstructorSymbol>(
                        cs => cs.IsSealed && constructor.IsStatic
                            && Matches(cs.Parameters, parameterTypes)
                            );
                case MethodInfo method:
                    var name = StripArity(method.Name, out var arity);
                    parameterTypes = GetTypeSymbols(method.GetParameters().Select(p => p.ParameterType));
                    return declaringType.FindMethod(name, parameterTypes);    
                case FieldInfo field:
                    return declaringType.GetFirstMember<FieldSymbol>(field.Name);
                case PropertyInfo property:
                    var parameters = property.GetIndexParameters();
                    if (parameters.Length > 0)
                    {
                        parameterTypes = GetTypeSymbols(parameters.Select(p => p.ParameterType));
                        return declaringType.GetFirstMember<IndexerSymbol>(nx => Matches(nx.GetMethod!.Parameters, parameterTypes));
                    }
                    else
                    {
                        return declaringType.GetFirstMember<PropertySymbol>(property.Name);
                    }
                case Type type:
                    if (type.IsGenericParameter)
                    {
                        return declaringType.TypeParameters.FirstOrDefault(tp => tp.Name == type.Name);
                    }
                    else
                    {
                        name = StripArity(type.Name, out arity);
                        return declaringType.FindType(type.Name, arity);
                    }
            }
        }
        else if (declaringSymbol is MethodSymbol declaringMethod)
        {
            if (member is Type type && type.IsGenericMethodParameter)
            {
                return declaringMethod.TypeParameters.FirstOrDefault(tp => tp.Name == type.Name);
            }
        }
        else if (declaringSymbol is NamespaceSymbol declaringNamespace)
        {
            if (member is Type type)
            {
                var name = StripArity(type.Name, out var arity);
                return declaringNamespace.FindType(name, arity);
            }
        }

        return null;
    }

    private static bool Matches(IReadOnlyList<ParameterSymbol> parameters, IReadOnlyList<TypeSymbol> types)
    {
        if (parameters.Count != types.Count)
            return false;
        for (int i = 0; i < parameters.Count; i++)
        {
            if (!TypeEqualityComparer.Instance.Equals(parameters[i].Type, types[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Gets the dotted path of a System.Reflection info compatible
    /// that can be used to search for that symbol in the global namespace.
    /// </summary>
    private static string GetDottedPath(object reflectionInfo)
    {
        if (reflectionInfo is Type type)
        {
            if (type.DeclaringType != null)
            {
                return GetDottedPath(type.DeclaringType) + "." + type.Name;
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
        else if (reflectionInfo is MemberInfo member)
        {
            if (member.DeclaringType != null)
            {
                return GetDottedPath(member.DeclaringType) + "." + member.Name;
            }
            else
            {
                return member.Name;
            }
        }
        else if (reflectionInfo is ParameterInfo parameter)
        {
            if (parameter.Member != null)
            {
                return GetDottedPath(parameter.Member) + "." + parameter.Name;
            }
            else
            {
                return parameter.Name!;
            }
        }
        else
        {
            throw new InvalidOperationException($"Unhandled runtime info: '{reflectionInfo.GetType().Name}' in {nameof(ReflectionSymbols)}.{nameof(GetDottedPath)}");
        }
    }
    #endregion

    #region Get reflection info for symbol

    /// <summary>
    /// Gets the System.Reflection info (Type, MemberInfo, ParameterInfo) corresponding to the <see cref="Symbol"/>.
    /// </summary>
    public bool TryGetInfo(Symbol symbol, [NotNullWhen(true)] out object? reflectionInfo) =>
        TryGetInfo(symbol, out reflectionInfo, null);

    /// <summary>
    /// Gets the System.Reflection info (Type, MemberInfo, ParameterInfo) corresponding to the <see cref="Symbol"/>.
    /// </summary>
    public bool TryGetInfo(Symbol symbol, [NotNullWhen(true)] out object? reflectionInfo, Func<Symbol, object?>? alternateSource)
    {
        if (alternateSource?.Invoke(symbol) is object altInfo)
        {
            reflectionInfo = altInfo;
            return true;
        }

        if (symbol is TypeSymbol typeSymbol
            && TryGetType(typeSymbol, out var runtimeType, alternateSource))
        {
            reflectionInfo = runtimeType;
            return true;
        }
        else if (symbol is MemberSymbol memberSymbol
            && TryGetMemberInfo(memberSymbol, out var memberInfo, alternateSource))
        {
            reflectionInfo = memberInfo;
            return true;
        }
        else if (symbol is ParameterSymbol parameterSymbol
            && parameterSymbol.DeclaringSymbol is MemberSymbol declaringMemberSymbol
            && TryGetMemberInfo(declaringMemberSymbol, out var declaringMemberInfo, alternateSource)
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
                reflectionInfo = parameterInfos[index];
                return true;
            }
        }

        reflectionInfo = null;
        return false;
    }

    /// <summary>
    /// Gets the <see cref="Type"/> corresponding to the <see cref="TypeSymbol"/>
    /// </summary>
    public override bool TryGetType(TypeSymbol typeSymbol, [NotNullWhen(true)] out Type? type) =>
        TryGetType(typeSymbol, out type, null);

    /// <summary>
    /// Gets the <see cref="Type"/> corresponding to the <see cref="TypeSymbol"/> if possible.
    /// </summary>
    public bool TryGetType(TypeSymbol typeSymbol, [NotNullWhen(true)] out Type? type, Func<Symbol, object?>? alternateSource)
    {
        if (alternateSource?.Invoke(typeSymbol) is Type altType)
        {
            type = altType;
            return true;
        }

        if (_symbolToInfoMap.TryGetValue(typeSymbol, out var runtimeInfo)
            && runtimeInfo is Type rt)
        {
            type = rt;
            return true;
        }

        if (base.TryGetType(typeSymbol, out type))
            return true;

        if (typeSymbol is ArraySymbol array
            && TryGetType(array.ElementType, out var elementType, alternateSource))
        {
            type = elementType.MakeArrayType();
            return true;
        }
        else if (typeSymbol is DelegateSymbol lambda
            && TryGetTypes(lambda.Parameters.Select(p => p.Type), out var parameterTypes, alternateSource)
            && TryGetType(lambda.ReturnType, out var returnType, alternateSource))
        {
            Type[] types = [.. parameterTypes, returnType];
            type = global::System.Linq.Expressions.Expression.GetDelegateType(types);
            return true;
        }
        else if (typeSymbol.Definition != null
            && TryGetType(typeSymbol.Definition, out var typeDef, alternateSource)
            && TryGetTypes(typeSymbol.TypeArguments, out var typeArgs, alternateSource))
        {
            type = typeDef.MakeGenericType(typeArgs.ToArray());
            return true;
        }

        // find type via declaring type
        if (typeSymbol.DeclaringSymbol is TypeSymbol declaringTypeSymbol
            && TryGetType(declaringTypeSymbol, out var declaringType, alternateSource))
        {
            // assume member index in current symbol is same as index in runtime type's members
            // (since using same function to fetch them)
            var index = declaringTypeSymbol.Members.IndexOf(typeSymbol);
            if (index >= 0)
            {
                var members = GetMemberInfos(declaringType);
                if (members[index] is Type mt)
                {
                    type = mt;
                    return true;
                }
            }
        }

        // look for type by metadata name
        foreach (var assembly in this.Assemblies)
        {
            if (assembly.GetType(typeSymbol.FullName) is Type t)
            {
                type = t;
                return true;
            }
        }

        type = null;
        return false;
    }

    /// <summary>
    /// Get the list of <see cref="Type"/> correspnding to the list of <see cref="TypeSymbol"/>.
    /// </summary>
    private bool TryGetTypes(IEnumerable<TypeSymbol> typeSymbols, [NotNullWhen(true)] out IReadOnlyList<Type>? types, Func<Symbol, object?>? alternateSource)
    {
        var list = new List<Type>();

        foreach (var typeSymbol in typeSymbols)
        {
            if (!TryGetType(typeSymbol, out var rt, alternateSource))
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
    /// Tries to get the <see cref="MemberInfo"/> corresponding with the <see cref="MemberSymbol"/>
    /// </summary>
    public bool TryGetMemberInfo(MemberSymbol memberSymbol, [NotNullWhen(true)] out MemberInfo? memberInfo) =>
        TryGetMemberInfo(memberSymbol, out memberInfo, null);

    /// <summary>
    /// Tries to get the <see cref="MemberInfo"/> corresponding with the <see cref="MemberSymbol"/>
    /// </summary>
    private bool TryGetMemberInfo(MemberSymbol memberSymbol, [NotNullWhen(true)] out MemberInfo? memberInfo, Func<Symbol, object?>? alternateSource)
    {
        if (alternateSource?.Invoke(memberSymbol) is MemberInfo ami)
        {
            memberInfo = ami;
            return true;
        }

        if (_symbolToInfoMap.TryGetValue(memberSymbol, out var runtimeInfo)
            && runtimeInfo is MemberInfo mi)
        {
            memberInfo = mi;
            return true;
        }

        if (memberSymbol is TypeSymbol typeSymbol
            && TryGetType(typeSymbol, out var runtimeType, alternateSource))
        {
            memberInfo = runtimeType;
            return true;

        }
        else if (memberSymbol is MethodSymbol methodSymbol
            && methodSymbol.IsConstructed
            && methodSymbol.Definition != null
            && TryGetMemberInfo(methodSymbol.Definition, out var methodDef, alternateSource)
            && methodDef is MethodInfo methodInfo
            && TryGetTypes(methodSymbol.TypeArguments, out var typeArgs, alternateSource))
        {
            memberInfo = methodInfo.MakeGenericMethod(typeArgs.ToArray());
            return true;
        }

        // find member via declaring type
        if (memberSymbol.DeclaringSymbol is TypeSymbol declaringTypeSymbol
            && TryGetType(declaringTypeSymbol, out var declaringType, alternateSource))
        {
            // assume member index in current symbol is same as index in runtime type's members
            // (since using same function to fetch them)
            var index = declaringTypeSymbol.Members.IndexOf(memberSymbol);
            if (index >= 0)
            {
                var members = GetMemberInfos(declaringType);
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
    #endregion

    #region Reflection helpers

    public static IReadOnlyList<MemberInfo> GetMemberInfos(Type runtimeType) =>
        runtimeType.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

    private ImmutableDictionary<Type, ImmutableDictionary<MethodInfo, ImmutableList<MethodInfo>>> _typeToInterfaceMethodMap =
        ImmutableDictionary<Type, ImmutableDictionary<MethodInfo, ImmutableList<MethodInfo>>>.Empty;

    /// <summary>
    /// Gets the list of interface methods that the implementation method implements.
    /// </summary>
    public ImmutableList<MethodInfo> GetInterfaceMethods(MethodInfo implementationMethod)
    {
        if (implementationMethod.DeclaringType == null
            || implementationMethod.DeclaringType.IsInterface)
            return ImmutableList<MethodInfo>.Empty;

        if (!_typeToInterfaceMethodMap.TryGetValue(implementationMethod.DeclaringType, out var methodMap))
        {
            var tmp = CreateMethodMap();
            methodMap = ImmutableInterlocked.GetOrAdd(ref _typeToInterfaceMethodMap, implementationMethod.DeclaringType, tmp);

            ImmutableDictionary<MethodInfo, ImmutableList<MethodInfo>> CreateMethodMap()
            {
                var map = new Dictionary<MethodInfo, List<MethodInfo>>();

                var interfaces = implementationMethod.DeclaringType.GetInterfaces();
                foreach (var iface in interfaces)
                {
                    var interfaceMap = implementationMethod.DeclaringType.GetInterfaceMap(iface);
                    for (int i = 0; i < interfaceMap.TargetMethods.Length; i++)
                    {
                        var targetMethod = interfaceMap.TargetMethods[i];
                        var interfaceMethod = interfaceMap.InterfaceMethods[i];
                        if (!map.TryGetValue(targetMethod, out var interfaceMethods))
                        {
                            interfaceMethods = new List<MethodInfo>();
                        }
                        interfaceMethods.Add(interfaceMethod);
                    }
                }

                return map.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableList());
            }
        }

        methodMap.TryGetValue(implementationMethod, out var interfaceMethods);
        return interfaceMethods ?? ImmutableList<MethodInfo>.Empty;
    }

    #endregion

    #region mscorlib

    /// <summary>
    /// The <see cref="ReflectionSymbols"/> corresponding to the current mscorlib loaded into this process.
    /// </summary>
    public static ReflectionSymbols CurrentMscorlib =>
        GetOrCreate([MscorlibAssembly]);

    /// <summary>
    /// The <see cref="Assembly"/> for the current mscorlib loaded into this process.
    /// </summary>
    private static Assembly MscorlibAssembly =
        typeof(int).Assembly;

    #endregion
}
