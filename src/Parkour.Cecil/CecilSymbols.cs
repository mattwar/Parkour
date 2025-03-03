using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Parkour.Cecil;

using Mono.Cecil.Rocks;
using Symbols;

/// <summary>
/// A <see cref="SymbolTable"/> from <see cref="Mono.Cecil"/> metadata objects.
/// </summary>
public class CecilSymbols : StandardSymbolTable
{
    /// <summary>
    /// The set of assemblies that define the set of types and namespaces.
    /// </summary>
    public ImmutableList<AssemblyDefinition> Assemblies { get; }

    /// <summary>
    /// A map from Cecil full names to <see cref="TypeDefinition"/>.
    /// </summary>
    private Dictionary<string, ImmutableList<TypeDefinition>>? _metadataNameToTypeMap;

    /// <summary>
    /// A map from Cecil metadata object to symbol.
    /// </summary>
    private readonly ConditionalWeakTable<IMetadataTokenProvider, Symbol> _cecilToSymbolMap =
        new ConditionalWeakTable<IMetadataTokenProvider, Symbol>();

    /// <summary>
    /// A map from symbol to Cecil metadata object.
    /// </summary>
    private readonly ConditionalWeakTable<Symbol, IMetadataTokenProvider> _symbolToCecilMap =
        new ConditionalWeakTable<Symbol, IMetadataTokenProvider>();

    /// <summary>
    /// Constructs a new <see cref="CecilSymbols"/>.
    /// Use <see cref="GetOrCreate(ImmutableList{AssemblyDefinition})"/>
    /// </summary>
    private CecilSymbols(GlobalNamespaceSymbol globalNamespace, ImmutableList<AssemblyDefinition> assemblies)
        : base(globalNamespace)
    {
        this.Assemblies = assemblies;
    }

    /// <summary>
    /// Gets or creates the <see cref="CecilSymbols"/> associated with the assemblies
    /// </summary>
    public static CecilSymbols GetOrCreate(ImmutableList<AssemblyDefinition> assemblies)
    {
        if (!_assembliesToSymbolsMap.TryGetValue(assemblies, out var runtimeSymbols))
        {
            runtimeSymbols = _assembliesToSymbolsMap.GetValue(assemblies, CreateSymbols);
            _namespaceToRuntimeSymbolsMap.TryAdd(runtimeSymbols.GlobalNamespace, runtimeSymbols);
        }

        return runtimeSymbols;
    }

    /// <summary>
    /// A weak map between a collection of assemblies and the symbols instance.
    /// </summary>
    private static readonly ConditionalWeakTable<ImmutableList<AssemblyDefinition>, CecilSymbols> _assembliesToSymbolsMap =
        new ConditionalWeakTable<ImmutableList<AssemblyDefinition>, CecilSymbols>();

    /// <summary>
    /// A weak map between a global namespace instances and the symbols instances that declared them.
    /// </summary>
    private static readonly ConditionalWeakTable<GlobalNamespaceSymbol, CecilSymbols> _namespaceToRuntimeSymbolsMap =
        new ConditionalWeakTable<GlobalNamespaceSymbol, CecilSymbols>();

    #region Create symbols

    private static CecilSymbols CreateSymbols(ImmutableList<AssemblyDefinition> assemblies)
    {
        var modules = assemblies.SelectMany(a => a.Modules).ToList();
        var types = modules.SelectMany(m => m.GetTypes()).ToList();
        IEnumerable<MethodDefinition>? methods = null; // modules.SelectMany(m => m.GetMethods()).ToList();
        IEnumerable<FieldDefinition>? fields = null; //var fields = modules.SelectMany(m => m.GetFields()).ToList();

        CecilSymbols? symbols = null;

        symbols = new CecilSymbols(
            new GlobalNamespaceSymbol(_ns => symbols!.CreateNamespaceMembers(_ns, "", "", types, methods, fields)),
            assemblies);

        return symbols;
    }

    private ImmutableList<Symbol> CreateNamespaceMembers(
        NamespaceSymbol? declaringNamespace,
        string containingNamespace,
        string namespaceName,
        IEnumerable<TypeDefinition> types,
        IEnumerable<MethodDefinition>? methods,
        IEnumerable<FieldDefinition>? fields)
    {
        var list = new List<Symbol>();

        var namespaceFullName = containingNamespace.Length > 0 && namespaceName.Length > 0
            ? containingNamespace + "." + namespaceName
            : namespaceName;

        var namespaceFullNameWithDot = namespaceFullName.Length > 0 ? namespaceFullName + "." : namespaceFullName;

        var typesInNamespace = types
            .Where(t => t.DeclaringType == null && t.Namespace == namespaceFullName)
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
    }

    /// <summary>
    /// Creates a member symbol given a definition.
    /// </summary>
    private Symbol CreateSymbol(IMetadataTokenProvider definition, Symbol? declaringSymbol)
    {
        if (!_cecilToSymbolMap.TryGetValue(definition, out var symbol))
        {
            var tmp = Create();
            if (tmp != null)
            {
                symbol = _cecilToSymbolMap.GetValue(definition, _ => tmp);
                _symbolToCecilMap.TryAdd(symbol, definition);
            }
        }

        return symbol!;

        Symbol? Create()
        {
            switch (definition)
            {
                case MethodDefinition cd when cd.IsConstructor:
                    return new ConstructorSymbol(
                        (TypeSymbol)declaringSymbol!,
                        GetAccess(cd),
                        GetModifiers(cd),
                        me => CreateParameters(me, cd),
                        me => CreateAttributeInfo(cd.CustomAttributes)
                        );

                case MethodDefinition md:
                    var name = StripArity(md.Name, out var arity);
                    return new MethodSymbol(
                        StripArity(md.Name),
                        declaringSymbol,
                        GetAccess(md),
                        GetModifiers(md),
                        md.GenericParameters.Count >= arity
                            ? me => CreateTypeParameters(me, md.GenericParameters.TakeLast(arity))
                            : me => ImmutableList<TypeParameterSymbol>.Empty,
                        () => ImmutableList<TypeSymbol>.Empty,
                        me => CreateParameters(me, md),
                        () => GetType(md.ReturnType)!,
                        me => CreateAttributeInfo(md.CustomAttributes),
                        me => GetImplementedInterfaceMethods(md),
                        constructedFrom: null
                        );

                case FieldDefinition field:
                    return new FieldSymbol(
                        field.Name,
                        declaringSymbol as TypeSymbol,
                        GetAccess(field),
                        GetModifiers(field),
                        () => GetType(field.FieldType)!,
                        me => CreateAttributeInfo(field.CustomAttributes),
                        field.IsLiteral ? field.Constant : null);

                case ParameterDefinition parameter:
                    return new ParameterSymbol(
                        parameter.Name ?? "",
                        declaringSymbol,
                        GetModifiers(parameter),
                        () => GetType(parameter.ParameterType)!,
                        me => CreateAttributeInfo(parameter.CustomAttributes)
                        );

                case PropertyDefinition property:
                    var indexParameters = property.Parameters;
                    if (indexParameters.Count > 0)
                    {
                        return new IndexerSymbol(
                            property.Name,
                            declaringSymbol as TypeSymbol,
                            GetAccess(property),
                            GetModifiers(property),
                            () => GetType(property.PropertyType)!,
                            property.GetMethod is MethodReference gm
                                ? me => (MethodSymbol)CreateSymbol(gm, me)!
                                : null,
                            property.SetMethod is MethodReference sm
                                ? me => (MethodSymbol)CreateSymbol(sm, me)!
                                : null,
                            me => CreateAttributeInfo(property.CustomAttributes)
                                );
                    }
                    else
                    {
                        return new PropertySymbol(
                            property.Name,
                            declaringSymbol as TypeSymbol,
                            GetAccess(property),
                            GetModifiers(property),
                            () => GetType(property.PropertyType)!,
                            fnBackingField: null,
                            property.GetMethod is MethodDefinition gm
                                ? me => (MethodSymbol)CreateSymbol(gm, me)!
                                : null,
                            property.SetMethod is MethodDefinition sm
                                ? me => (MethodSymbol)CreateSymbol(sm, me)!
                                : null,
                            me => CreateAttributeInfo(property.CustomAttributes)
                            );
                    }

                case TypeDefinition type:
                {
                    name = StripArity(type.Name, out arity);
                    var access = GetAccess(type);
                    var modifiers = GetModifiers(type);
                    Func<TypeSymbol, ImmutableList<TypeParameterSymbol>> fnTypeParameters =
                        type.GenericParameters.Count >= arity
                            ? me => CreateTypeParameters(me, type.GenericParameters.TakeLast(arity))
                            : me => ImmutableList<TypeParameterSymbol>.Empty;
                    Func<ImmutableList<TypeSymbol>> fnTypeArguments =
                        () => ImmutableList<TypeSymbol>.Empty;
                    TypeSymbol? constructedFromType = null;
                    Func<ImmutableList<TypeSymbol>> fnBaseTypes =
                        () => GetBaseTypes(type.BaseType, type.Interfaces);
                    Func<TypeSymbol, ImmutableList<Symbol>> fnMembers =
                        me => CreateMembers(type, me);
                    Func<TypeSymbol, ImmutableList<AttributeInfo>> fnAttributes =
                        me => CreateAttributeInfo(type.CustomAttributes);

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
                            constructedFromType
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
                            fnAttributes,
                            constructedFromType
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
                            fnAttributes,
                            constructedFromType
                            );
                    }
                }

                case GenericParameter gp:
                    return new TypeParameterSymbol(gp.Name);
            }

            return null;
        }

        ImmutableList<Symbol> CreateMembers(TypeDefinition type, MemberSymbol? declaringSymbol)
        {
            var methods = type.Methods.Select(m => CreateSymbol(m, declaringSymbol));
            var fields = type.Fields.Select(f => CreateSymbol(f, declaringSymbol));
            var properties = type.Properties.Select(p => CreateSymbol(p, declaringSymbol));
            var events = type.Events.Select(e => CreateSymbol(e, declaringSymbol));
            var nestedTypes = type.NestedTypes.Select(t => CreateSymbol(t, declaringSymbol));

            return methods
                .Concat(fields)
                .Concat(properties)
                .Concat(events)
                .Concat(nestedTypes)
                .Where(s => s != null)
                .ToImmutableList()!;
        }

        ImmutableList<ParameterSymbol> CreateParameters(MemberSymbol? declaringSymbol, MethodReference method) =>
            method.Parameters.Select(p => (ParameterSymbol)CreateSymbol(p, declaringSymbol)!).ToImmutableList();

        ImmutableList<TypeParameterSymbol> CreateTypeParameters(MemberSymbol? declaringSymbol, IEnumerable<GenericParameter> genericParameters) =>
            genericParameters.Select(p => (TypeParameterSymbol)CreateSymbol(p, declaringSymbol)).ToImmutableList();
    }

    /// <summary>
    /// Gets the interface methods that this method implements.
    /// </summary>
    ImmutableList<MethodSymbol> GetImplementedInterfaceMethods(MethodDefinition implementationMethod)
    {
        if (implementationMethod.DeclaringType == null)
            return ImmutableList<MethodSymbol>.Empty;

        List<MethodSymbol>? list = null;

        // find all interface methods that this method implements
        var interfaces = implementationMethod.DeclaringType.Interfaces;
        foreach (var iface in interfaces)
        {
            var ifaceSymbol = GetType(iface.InterfaceType);

            foreach (var methodSymbol in ifaceSymbol!.Members.OfType<MethodSymbol>())
            {
                if (TryGetMemberReference(methodSymbol, out var cecilMember)
                    && cecilMember is MethodReference ifaceMethodRef
                    && IsImplicitInterfaceImplementation(implementationMethod, ifaceMethodRef))
                {
                    if (list == null)
                        list = new List<MethodSymbol>();
                    list.Add(methodSymbol);
                }
            }
        }

        return list != null
            ? list.ToImmutableList()
            : ImmutableList<MethodSymbol>.Empty;
    }


    private ImmutableList<TypeSymbol> GetBaseTypes(TypeReference? baseType, IList<InterfaceImplementation> interfaces)
    {
        if (baseType != null)
        {
            if (interfaces == null || interfaces.Count == 0)
                return [GetType(baseType)!];

            return GetTypes(new[] { baseType }.Concat(interfaces.Select(i => i.InterfaceType)));
        }
        else if (interfaces.Count > 0)
        {
            return GetTypes(interfaces.Select(i => i.InterfaceType));
        }
        else
        {
            return ImmutableList<TypeSymbol>.Empty;
        }
    }

    public static SymbolAccess GetAccess(IMemberDefinition definition) =>
        definition switch
        {
            TypeDefinition type =>
                type.IsPublic ? SymbolAccess.Public
                : type.IsNestedPublic ? SymbolAccess.Public
                : type.IsNestedFamily ? SymbolAccess.Protected
                : type.IsNestedFamilyAndAssembly ? SymbolAccess.ProtectedAndInternal
                : type.IsNestedFamilyOrAssembly ? SymbolAccess.ProtectedOrInternal
                : type.IsPublic ? SymbolAccess.Public
                : type.IsNotPublic ? SymbolAccess.Internal
                : SymbolAccess.Private,
            FieldDefinition field =>
                field.IsPublic ? SymbolAccess.Public
                : field.IsAssembly ? SymbolAccess.Internal
                : field.IsFamily ? SymbolAccess.Protected
                : field.IsFamilyAndAssembly ? SymbolAccess.ProtectedAndInternal
                : field.IsFamilyOrAssembly ? SymbolAccess.ProtectedOrInternal
                : SymbolAccess.Private,
            PropertyDefinition property =>
                GetAccess(property.GetMethod!),
            MethodDefinition method =>
                method.IsPublic ? SymbolAccess.Public
                : method.IsAssembly ? SymbolAccess.Internal
                : method.IsFamily ? SymbolAccess.Protected
                : method.IsFamilyAndAssembly ? SymbolAccess.ProtectedAndInternal
                : method.IsFamilyOrAssembly ? SymbolAccess.ProtectedOrInternal
                : SymbolAccess.Private,
            _ => SymbolAccess.Private
        };

    public static BitSet<SymbolModifier> GetModifiers(IMemberDefinition definition) =>
        definition switch
        {
            TypeDefinition type =>
                (type.IsAbstract ? SymbolModifier.Abstract : SymbolModifier.None)
                | (type.IsSealed ? SymbolModifier.Sealed : SymbolModifier.None),
            FieldDefinition field =>
                (field.IsStatic ? SymbolModifier.Static : SymbolModifier.None)
                | (field.IsLiteral ? SymbolModifier.Constant : SymbolModifier.None),
            PropertyDefinition property =>
                // borrow modifiers from the get method
                GetModifiers(property.GetMethod!).Remove(SymbolModifier.HideBySig).Remove(SymbolModifier.Special),
            MethodDefinition method =>
                (method.IsStatic ? SymbolModifier.Static : SymbolModifier.None)
                | (method.IsAbstract ? SymbolModifier.Abstract : SymbolModifier.None)
                | (method.IsVirtual ? SymbolModifier.Virtual : SymbolModifier.None)
                | (method.IsFinal ? SymbolModifier.Sealed : SymbolModifier.None)
                | (method.IsHideBySig ? SymbolModifier.HideBySig : SymbolModifier.None)
                | (method.IsSpecialName ? SymbolModifier.Special : SymbolModifier.None),
            _ => SymbolModifier.None
        };

    public static BitSet<SymbolModifier> GetModifiers(ParameterDefinition definition)
    {
        var isIn = (definition.Attributes & ParameterAttributes.In) != 0;
        var isOut = (definition.Attributes & ParameterAttributes.Out) != 0;
        return isIn && isOut ? SymbolModifier.Ref
            : isIn ? SymbolModifier.In
            : isOut ? SymbolModifier.Out
            : SymbolModifier.None;
    }

    public ImmutableList<AttributeInfo> CreateAttributeInfo(IEnumerable<CustomAttribute> data) =>
        data.Select(d => CreateAttributeInfo(d))
            .OfType<AttributeInfo>()
            .ToImmutableList();

    private AttributeInfo? CreateAttributeInfo(CustomAttribute data)
    {
        var constructor = (ConstructorSymbol?)GetSymbol(data.Constructor);
        if (constructor != null)
        {
            var type = constructor.ConstructedType;
            var arguments = data.ConstructorArguments
                .Select((a, i) => new AttributeArgument(constructor.Parameters[i], CreateValue(a)))
                .ToImmutableList();
            var properties = data.Properties
                .Select(n => new AttributeMember(type.GetFirstMember<PropertySymbol>(n.Name)!, CreateValue(n.Argument)));
            var fields = data.Fields
                .Select(n => new AttributeMember(type.GetFirstMember<FieldSymbol>(n.Name)!, CreateValue(n.Argument)));
            var members = properties.Concat(fields).ToImmutableList();
            return new AttributeInfo(constructor, arguments, members);
        }

        return null;

        AttributeValue CreateValue(CustomAttributeArgument arg)
        {
            if (arg.Value is Type type)
            {
                return new AttributeTypeValue(GetTypeSymbol(type));
            }
            else if (arg.Value is CustomAttributeArgument[] args)
            {
                var elementType = GetType(arg.Type.GetElementType());
                var values = args.Select(a => CreateValue(a)).ToImmutableList();
                return new AttributeArrayValue(elementType!, values);
            }
            else
            {
                return new AttributeConstantValue(arg.Value);
            }
        }
    }
#endregion

    #region Get symbols from Cecil types
    /// <summary>
    /// Gets the symbol corresponding to the Cecil metadata object.
    /// </summary>
    private Symbol? GetSymbol(IMetadataTokenProvider cecilSymbol)
    {
        // is this a known symbol?
        if (_cecilToSymbolMap.TryGetValue(cecilSymbol, out var symbol))
            return symbol;

        symbol = Get();
        return symbol;

        Symbol? Get()
        {
            if (cecilSymbol is MemberReference member)
            {
                member = ResolveReference(member);

                // is resolved reference a known symbol?
                if (_cecilToSymbolMap.TryGetValue(member, out symbol))
                    return symbol;

                if (member.DeclaringType != null)
                {
                    // get declaring type and find equivalent symbol
                    var declaringType = GetType(member.DeclaringType);
                    return FindMember(declaringType, member);
                }
                else if (member is GenericInstanceType gt)
                {
                    var elementSymbol = GetType(gt.ElementType);
                    return GetConstructedMember(elementSymbol, gt.GenericArguments.ToList());
                }
                else if (member is GenericInstanceMethod gm)
                {
                    var elementMethod = (MethodSymbol)GetSymbol(gm.ElementMethod)!;
                    return GetConstructedMember(elementMethod, gm.GenericArguments.ToList());
                }
                else if (member is GenericParameter gp)
                {
                    return _cecilToSymbolMap.GetValue(gp, _ => new TypeParameterSymbol(gp.Name));
                }
                else
                {
                    var path = GetDottedPath(member);
                    return GetSymbol(path);                   
                }
            }
            else if (cecilSymbol is ArrayType at)
            {
                var elementType = GetType(at.ElementType);
                return this.GetArray(elementType, at.Rank);
            }
            else
            {
                throw new NotImplementedException($"Unhandled metadata object: '{cecilSymbol.GetType().Name}'");
            }
        }
    }

    /// <summary>
    /// Gets or type symbol corresponding to the Cecil <see cref="TypeReference"/>.
    /// </summary>
    private TypeSymbol GetType(TypeReference type)
    {
        return (TypeSymbol)GetSymbol(type)!;
    }

    /// <summary>
    /// Gets a list of <see cref="TypeSymbol"/> corresponding to the list of <see cref="TypeReference"/>.
    /// </summary>
    private ImmutableList<TypeSymbol> GetTypes(IEnumerable<TypeReference> types)
    {
        if (!types.Any())
            return ImmutableList<TypeSymbol>.Empty;
        var list = types.Select(GetType).ToImmutableList();
        return list!;
    }

    /// <summary>
    /// Finds the member symbol within the declaring type that corresponds to the reference.
    /// </summary>
    private Symbol? FindMember(TypeSymbol declaringType, MemberReference member)
    {
        switch (member)
        {
            case MethodDefinition cd when cd.IsConstructor:
                var parameterTypes = GetTypes(cd.Parameters.Select(p => p.ParameterType));
                return declaringType.GetFirstMember<ConstructorSymbol>(cs => 
                    cs.IsStatic == cd.IsStatic
                    && Matches(cs.Parameters, parameterTypes));

            case MethodReference mr:
                var name = StripArity(mr.Name, out var arity);
                parameterTypes = GetTypes(mr.Parameters.Select(p => p.ParameterType));
                return declaringType.GetFirstMember<MethodSymbol>(
                    name,
                    ms => Matches(ms.Parameters, parameterTypes)
                    );

            case FieldReference fr:
                return declaringType.GetFirstMember<FieldSymbol>(fr.Name);

            case PropertyReference pr:
                if (pr.Parameters.Count > 0)
                {
                    parameterTypes = GetTypes(pr.Parameters.Select(p => p.ParameterType));
                    return declaringType.GetFirstMember<IndexerSymbol>(nx => Matches(nx.GetMethod!.Parameters, parameterTypes));
                }
                else
                {
                    return declaringType.GetFirstMember<PropertySymbol>(pr.Name);
                }

            case GenericParameter gp:
                if (gp.Owner != null)
                {
                    var owner = (MemberSymbol)GetSymbol((MemberReference)gp.Owner)!;
                    return GetTypeParameter(owner, gp.Name);
                }
                else
                {
                    return GetTypeParameter(declaringType, gp.Name);
                }

            case TypeReference td:
                name = StripArity(td.Name, out arity);
                return declaringType.GetFirstMember<TypeSymbol>(name, ts => ts.Arity == arity);

            default:
                throw new NotImplementedException();
        }

        TypeParameterSymbol? GetTypeParameter(MemberSymbol member, string name)
        {
            if (member is TypeSymbol typeSymbol)
            {
                var typeParam = typeSymbol.TypeParameters.FirstOrDefault(tp => tp.Name == name);
                if (typeParam == null && typeSymbol.DeclaringType != null)
                {
                    return GetTypeParameter(typeSymbol.DeclaringType, name);
                }
                return typeParam;
            }
            else if (member is MethodSymbol method)
            {
                var typeParam = method.TypeParameters.FirstOrDefault(tp => tp.Name == name);
                if (typeParam == null && method.DeclaringType != null)
                {
                    return GetTypeParameter(method.DeclaringType, name);
                }
                return typeParam;
            }
            return null;
        }
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

    private MemberSymbol GetConstructedMember(MemberSymbol symbol, List<TypeReference> typeArgs)
    {
        if (symbol.DeclaringSymbol is TypeSymbol declaringType)
        {
            var constructedDeclaringType = (TypeSymbol)GetConstructedMember(declaringType, typeArgs);
            if (constructedDeclaringType != declaringType)
            {
                if (symbol is TypeSymbol ts)
                {
                    // find equivalent nested type in the constructed type
                    var equivalentSymbol = constructedDeclaringType.Members.OfType<TypeSymbol>().FirstOrDefault(t => t.ConstructedFrom == symbol);
                    symbol = equivalentSymbol ?? ts;
                }
                else if (symbol is MethodSymbol ms)
                {
                    var equivalentSymbol = constructedDeclaringType.Members.OfType<MethodSymbol>().FirstOrDefault(t => t.ConstructedFrom == symbol);
                    symbol = equivalentSymbol ?? ms;
                }
                else if (symbol is ConstructorSymbol cs)
                {
                }
                else if (symbol is FieldSymbol fs)
                {
                    var equivalentSymbol = constructedDeclaringType.Members.OfType<FieldSymbol>().FirstOrDefault(t => t.Name == fs.Name);
                    symbol = equivalentSymbol ?? fs;
                }
                else if (symbol is PropertySymbol ps)
                {
                    var equivalentSymbol = constructedDeclaringType.Members.OfType<PropertySymbol>().FirstOrDefault(t => t.Name == ps.Name);
                    symbol = equivalentSymbol ?? ps;
                }
                else if (symbol is IndexerSymbol nx)
                {
                    var equivalentSymbol = constructedDeclaringType.Members.OfType<IndexerSymbol>().FirstOrDefault(t => t.Name == nx.Name);
                    symbol = equivalentSymbol ?? nx;
                }
            }
        }

        if (symbol.Arity > 0)
        {
            var symbolTypeArgs = GetTypes(typeArgs.Take(symbol.Arity));
            typeArgs.RemoveRange(0, symbol.Arity);
            return this.GetConstructed(symbol, symbolTypeArgs);
        }

        return symbol;
    }

    #endregion

    #region Get Cecil types from symbols
    /// <summary>
    /// Gets the Cecil metadata object corresponding to the <see cref="Symbol"/>.
    /// </summary>
    public bool TryGetMetadataObject(Symbol symbol, [NotNullWhen(true)] out IMetadataTokenProvider? cecilSymbol) =>
        TryGetMetadataObject(symbol, out cecilSymbol, null);

    /// <summary>
    /// Gets the Cecil metadata object corresponding to the <see cref="Symbol"/>.
    /// </summary>
    public bool TryGetMetadataObject(Symbol symbol, [NotNullWhen(true)] out IMetadataTokenProvider? cecilSymbol, Func<Symbol, object?>? alternateSource)
    {
        if (alternateSource?.Invoke(symbol) is IMetadataTokenProvider alt)
        {
            cecilSymbol = alt;
            return true;
        }

        if (symbol is TypeSymbol typeSymbol
            && TryGetTypeReference(typeSymbol, out var cecilType))
        {
            cecilSymbol = cecilType;
            return true;
        }
        else if (symbol is MemberSymbol memberSymbol
            && TryGetMemberReference(memberSymbol, out var cecilMember))
        {
            cecilSymbol = cecilMember;
            return true;
        }
        else if (symbol is ParameterSymbol parameterSymbol
            && parameterSymbol.DeclaringSymbol is MemberSymbol declaringMemberSymbol
            && TryGetMemberReference(declaringMemberSymbol, out var declaringMember)
            && declaringMember is MethodDefinition declaringMethod)
        {
            var index = declaringMemberSymbol switch
            {
                MethodSymbol ms => ms.Parameters.IndexOf(parameterSymbol),
                ConstructorSymbol cs => cs.Parameters.IndexOf(parameterSymbol),
                _ => -1
            };

            var parameters = declaringMethod.Parameters;
            if (index >= 0 && index < parameters.Count)
            {
                cecilSymbol = parameters[index];
                return true;
            }
        }

        cecilSymbol = null;
        return false;
    }

    /// <summary>
    /// Gets a Cecil <see cref="TypeReference"/> correpsonding with the <see cref="TypeSymbol"/>
    /// </summary>
    public bool TryGetTypeReference(TypeSymbol typeSymbol, [NotNullWhen(true)] out TypeReference? cecilType) =>
        TryGetTypeReference(typeSymbol, out cecilType, null);

    /// <summary>
    /// Gets a Cecil <see cref="TypeReference"/> corresponding with the <see cref="TypeSymbol"/>
    /// </summary>
    public bool TryGetTypeReference(TypeSymbol typeSymbol, [NotNullWhen(true)] out TypeReference? cecilType, Func<Symbol, object?>? alternateSource)
    {
        if (alternateSource?.Invoke(typeSymbol) is TypeDefinition altType)
        {
            cecilType = altType;
            return true;
        }

        if (_symbolToCecilMap.TryGetValue(typeSymbol, out var runtimeInfo)
            && runtimeInfo is TypeDefinition rt)
        {
            cecilType = rt;
            return true;
        }

        if (typeSymbol == this.Object
            || typeSymbol == SpecialSymbols.Null
            || typeSymbol == SpecialSymbols.Unknown)
        {
            cecilType = this.ObjectDefinition;
            return true;
        }
        else if (
            typeSymbol == this.Void
            || typeSymbol == SpecialSymbols.DoesNotReturn)
        {
            cecilType = this.VoidDefinition;
            return true;
        }

        if (typeSymbol is ArraySymbol array
            && TryGetTypeReference(array.ElementType, out var elementType, alternateSource))
        {
            cecilType = new ArrayType(elementType);
            return true;
        }
        else if (typeSymbol.ConstructedFrom != null
            && TryGetTypeReference(typeSymbol.ConstructedFrom, out var elementTypeDef, alternateSource))
        {
            var generic = new GenericInstanceType(elementTypeDef);

            // reference includes all type args (including type args of declaring type)
            var allTypeArgs = GetAllTypeArguments(typeSymbol);
            if (TryGetTypeReferences(allTypeArgs, out var allTypeArgsRefs, alternateSource))
            {
                foreach (var arg in allTypeArgsRefs)
                {
                    generic.GenericArguments.Add(arg);
                }
            }

            // declaring type is element's declaring type?
            //generic.DeclaringType = elementTypeDef.DeclaringType;

            cecilType = generic;
            return true;
        }
        else if (typeSymbol.ConstructedFrom == null)
        {
            if (GetFirstTypeDefinition(typeSymbol.FullName) is { } td)
            {
                cecilType = td;
                return true;
            }
        }

        cecilType = null;
        return false;
    }

    /// <summary>
    /// Gets all the type arguments of the symbol and declaring symbol(s)
    /// in order as would be needed to represented in metadata.
    /// </summary>
    private ImmutableList<TypeSymbol> GetAllTypeArguments(MemberSymbol symbol)
    {
        List<TypeSymbol>? typeArgs = null;
        Gather(symbol);
        return typeArgs != null ? typeArgs.ToImmutableList() : ImmutableList<TypeSymbol>.Empty;

        void Gather(MemberSymbol symbol)
        {
            if (symbol.DeclaringSymbol is TypeSymbol declaringType)
            {
                Gather(declaringType);
            }

            if (symbol is TypeSymbol ts && ts.TypeArguments.Count > 0)
            {
                if (typeArgs == null)
                    typeArgs = new List<TypeSymbol>();
                typeArgs.AddRange(ts.TypeArguments);

            }
            else if (symbol is MethodSymbol ms  && ms.TypeArguments.Count > 0)
            {
                if (typeArgs == null)
                    typeArgs = new List<TypeSymbol>();
                typeArgs.AddRange(ms.TypeArguments);
            }
        }
    }

    /// <summary>
    /// Gets a list of Cecil <see cref="TypeReference"/> corresponding to the list of <see cref="TypeSymbol"/>.
    /// </summary>
    private bool TryGetTypeReferences(
        IEnumerable<TypeSymbol> typeSymbols, 
        [NotNullWhen(true)] out IReadOnlyList<TypeReference>? types,
        Func<Symbol, object?>? alternateSource)
    {
        var list = new List<TypeReference>();

        foreach (var typeSymbol in typeSymbols)
        {
            if (!TryGetTypeReference(typeSymbol, out var rt, alternateSource))
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
    /// Gets a Cecil <see cref="MemberReference"/> corresponding to the <see cref="MemberSymbol"/>.
    /// </summary>
    public bool TryGetMemberReference(MemberSymbol memberSymbol, [NotNullWhen(true)] out MemberReference? member) =>
        TryGetMemberReference(memberSymbol, out member, null);

    /// <summary>
    /// Gets a Cecil <see cref="MemberReference"/> corresponding to the <see cref="MemberSymbol"/>.
    /// </summary>
    private bool TryGetMemberReference(MemberSymbol memberSymbol, [NotNullWhen(true)] out MemberReference? member, Func<Symbol, object?>? alternateSource)
    {
        if (alternateSource?.Invoke(memberSymbol) is MemberReference amr)
        {
            member = amr;
            return true;
        }

        if (_symbolToCecilMap.TryGetValue(memberSymbol, out var runtimeInfo)
            && runtimeInfo is MemberReference mr)
        {
            member = mr;
            return true;
        }

        if (memberSymbol is TypeSymbol typeSymbol
            && TryGetTypeReference(typeSymbol, out var runtimeType, alternateSource))
        {
            member = runtimeType;
            return true;
        }
        else if (memberSymbol is MethodSymbol methodSymbol
            && methodSymbol.ConstructedFrom != null
            && TryGetMemberReference(methodSymbol.ConstructedFrom, out var methodRef, alternateSource)
            && methodRef is MethodDefinition methodDef)
        {
            var gm = new GenericInstanceMethod(methodDef);

            var allTypeArgs = GetAllTypeArguments(methodSymbol);
            if (TryGetTypeReferences(allTypeArgs, out var allTypeArgsRefs, alternateSource))
            {
                foreach (var arg in allTypeArgsRefs)
                {
                    gm.GenericArguments.Add(arg);
                }
            }

            gm.DeclaringType = methodDef.DeclaringType;

            member = gm;
            return true;
        }

        if (memberSymbol.DeclaringSymbol is TypeSymbol declaringTypeSymbol
            && ((declaringTypeSymbol.ConstructedFrom != null && TryGetTypeReference(declaringTypeSymbol.ConstructedFrom, out var declaringTypeRef, alternateSource)
                || TryGetTypeReference(declaringTypeSymbol, out declaringTypeRef, alternateSource))))
        {
            // find member via declaring type           
            if (declaringTypeRef is TypeDefinition td)
            {
                member = null;

                if (memberSymbol is MethodSymbol method)
                {
                    // need parameter types to match
                    member = td.Methods.FirstOrDefault(m => 
                        m.Name == method.Name
                        && m.Parameters.Count == method.Parameters.Count
                        && m.GenericParameters.Count == method.TypeParameters.Count
                        );
                }
                else if (memberSymbol is ConstructorSymbol constructor)
                {
                    member = td.Methods.FirstOrDefault(m =>
                        m.IsConstructor
                        && m.IsStatic == constructor.IsStatic
                        && m.Parameters.Count == constructor.Parameters.Count
                        );
                }
                else if (memberSymbol is PropertySymbol property)
                {
                    member = td.Properties.FirstOrDefault(p =>
                        p.Name == property.Name
                        && p.Parameters.Count == 0
                        );
                }
                else if (memberSymbol is IndexerSymbol indexer)
                {
                    member = td.Properties.FirstOrDefault(p =>
                        p.Name == indexer.Name
                        && p.Parameters.Count == indexer.GetMethod!.Parameters.Count
                        );
                }

                return member != null;
            }
        }

        member = null;
        return false;
    }

    /// <summary>
    /// Gets the definition that the reference is referring to.
    /// </summary>
    private bool TryGetDefinition(MemberReference reference, [NotNullWhen(true)] out IMemberDefinition? definition)
    {
        // already a definition?
        if (reference is IMemberDefinition md)
        {
            definition = md;
            return true;
        }

        switch (reference)
        {
            case GenericInstanceMethod:
            case GenericInstanceType:
            case ArrayType:
                definition = null;
                return false;
            case TypeReference typeRef:
                // lookup definition by full name
                definition = GetFirstTypeDefinition(typeRef.FullName);
                return definition != null;
        }

        var declaringTypeDef = reference.DeclaringType != null
                && TryGetDefinition(reference.DeclaringType, out var declaringDef)
            ? (TypeDefinition)declaringDef
            : null;

        // otherwise, find matching member in declaring type
        if (declaringTypeDef != null)
        {
            definition = null;

            switch (reference)
            {
                case MethodReference methodRef:
                    definition = declaringTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name && SignatureMatches(m, methodRef));
                    break;
                case FieldReference fieldRef:
                    definition = declaringTypeDef.Fields.FirstOrDefault(f => f.Name == fieldRef.Name);
                    break;
                case PropertyReference propertyRef:
                    definition = declaringTypeDef.Properties.FirstOrDefault(p => p.Name == propertyRef.Name);
                    break;
                case EventReference eventRef:
                    definition = declaringTypeDef.Events.FirstOrDefault(e => e.Name == eventRef.Name);
                    break;
            }

            return definition != null;
        }

        definition = null;
        return false;
    }

    #endregion

    #region Find Cecil types by name
    /// <summary>
    /// Gets all the <see cref="TypeDefinition"/> objects with the full name.
    /// </summary>
    private ImmutableList<TypeDefinition> GetTypeDefinitions(string fullName)
    {
        if (_metadataNameToTypeMap == null)
        {
            _metadataNameToTypeMap =
                this.Assemblies.SelectMany(a => a.Modules.SelectMany(m => m.Types))
                .ToLookup(x => x.FullName)
                .ToDictionary(x => x.Key, x => x.Any() ? x.ToImmutableList() : ImmutableList<TypeDefinition>.Empty);
        }

        return _metadataNameToTypeMap.TryGetValue(fullName, out var list) ? list : ImmutableList<TypeDefinition>.Empty;
    }

    /// <summary>
    /// Gets the first <see cref="TypeDefinition"/> found for the name or null if not found.
    /// </summary>
    private TypeDefinition? GetFirstTypeDefinition(string fullName) =>
        GetTypeDefinitions(fullName).FirstOrDefault();

    /// <summary>
    /// The definition of the object type.
    /// </summary>
    private TypeDefinition ObjectDefinition => _lazyObjectType ??= GetFirstTypeDefinition("System.Object")!;
    private TypeDefinition? _lazyObjectType;

    private TypeDefinition VoidDefinition => _lazyVoidDefinition ?? GetFirstTypeDefinition("System.Void")!;
    private TypeDefinition? _lazyVoidDefinition;

    #endregion

    #region Cecil helpers
    /// <summary>
    /// Resolves name-only references to definitions if possible.
    /// Returns the definition if found, otherwise the original reference.
    /// </summary>
    private MemberReference ResolveReference(MemberReference reference)
    {
        if (TryGetDefinition(reference, out var definition))
        {
            return (MemberReference)definition;
        }
        return reference;
    }

    /// <summary>
    /// Implicit interface implementations are based only on method's name and signature equivalence.
    /// </summary>
    private bool IsImplicitInterfaceImplementation(MethodDefinition method, MethodReference interfaceMethod)
    {
        // check that the 'overridden' method is iface method and the iface is implemented by method.DeclaringType
        if (!IsInterface(interfaceMethod.DeclaringType) ||
            !method.DeclaringType.Interfaces.Any(i => Equals(i.InterfaceType, interfaceMethod.DeclaringType)))
        {
            return false;
        }

        // check whether the type contains some other explicit implementation of the method
        if (method.DeclaringType.Methods.SelectMany(m => m.Overrides).Any(m => Equals(m, interfaceMethod)))
        {
            // explicit implementation -> no implicit implementation possible
            return false;
        }

        // now it is enough to just match the signatures and names:
        return method.Name == interfaceMethod.Name && SignatureMatches(method, interfaceMethod);
    }

    /// <summary>
    /// True if the type is known to be an interface.
    /// </summary>
    private bool IsInterface(TypeReference type)
    {
        var resolved = ResolveReference(type);
        if (resolved is TypeDefinition td)
            return td.IsInterface;
        else if (resolved is GenericInstanceType git)
            return IsInterface(git.ElementType);
        return false;
    }

    /// <summary>
    /// True if the two method signatures match
    /// </summary>
    private bool SignatureMatches(MethodReference method1, MethodReference method2)
    {
        method1 = (MethodReference)ResolveReference(method1);
        method2 = (MethodReference)ResolveReference(method2);

        if (method1 is GenericInstanceMethod gm1 && method2 is GenericInstanceMethod gm2)
        {
            if (!SignatureMatches(gm1.ElementMethod, gm2.ElementMethod))
                return false;

            return Equals(gm1.GenericArguments, gm2.GenericArguments);
        }
        else
        {
            if (method1.GenericParameters.Count != method2.GenericParameters.Count)
                return false;

            if (method1.Parameters.Count != method2.Parameters.Count)
                return false;

            for (int i = 0; i < method1.Parameters.Count; i++)
            {
                if (!Equals(method1.Parameters[i].ParameterType, method2.Parameters[i].ParameterType))
                    return false;
            }
        }

        return true;
    }

#if false
    /// <summary>
    /// True if this method overrides the specified method.
    /// </summary>
    private bool Overrides(MethodDefinition method, MethodReference overridden)
    {
        bool explicitIfaceImplementation = method.Overrides.Any(overrides => Equals(overrides, overridden));
        if (explicitIfaceImplementation)
        {
            return true;
        }

        if (IsImplicitInterfaceImplementation(method, overridden))
        {
            return true;
        }

        // new slot method cannot override any base classes' method by convention:
        if (method.IsNewSlot)
        {
            return false;
        }

        // check base-type overrides using Cecil's helper method GetOriginalBaseMethod()
        return Equals(method.GetOriginalBaseMethod(), overridden);
    }
#endif

    /// <summary>
    /// Gets the dotted path to a symbol in the global namespace.
    /// </summary>
    private static string GetDottedPath(IMetadataTokenProvider cecilSymbol)
    {
        if (cecilSymbol is TypeDefinition type)
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
        else if (cecilSymbol is IMemberDefinition member)
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
        else
        {
            throw new InvalidOperationException($"Unhandled runtime info: '{cecilSymbol.GetType().Name}' in {nameof(CecilSymbols)}.{nameof(GetDottedPath)}");
        }
    }

    #endregion

    #region Cecil object equality

    /// <summary>
    /// Returns true if the two Cecil metadata objects are the same or refer to the same definition or instantiation.
    /// </summary>
    private bool Equals(IMetadataTokenProvider? x, IMetadataTokenProvider? y)
    {
        if (x == y)
            return true;

        if (x == null || y == null)
            return false;

        if (x.GetType() != y.GetType())
            return false;

        if (x is MemberReference xmr && y is MemberReference ymr)
        {
            xmr = ResolveReference(xmr);
            ymr = ResolveReference(ymr);

            if (x is GenericInstanceType xgt && y is GenericInstanceType ygt)
            {
                return Equals(xgt.ElementType, ygt.ElementType)
                    && Equals(xgt.GenericArguments, ygt.GenericArguments);
            }
            else if (x is GenericInstanceMethod xgm && y is GenericInstanceMethod ygm)
            {
                return Equals(xgm.ElementMethod, ygm.ElementMethod)
                    && Equals(xgm.GenericArguments, ygm.GenericArguments);
            }
            else if (x is ArrayType xat && y is ArrayType yat)
            {
                return xat.Rank == yat.Rank
                    && Equals(xat.ElementType, yat.ElementType);
            }
            else if (xmr.IsDefinition && ymr.IsDefinition)
            {
                return xmr == ymr;
            }
            else
            {
                return Equals(xmr.DeclaringType, ymr.DeclaringType)
                    && xmr.Name == ymr.Name;
            }
        }
        else if (x is ParameterReference xpr && y is ParameterReference ypr)
        {
            return xpr.Name == ypr.Name
                && Equals(xpr.ParameterType, ypr.ParameterType);
        }

        return x == y;
    }

    private bool Equals<T>(IList<T> x, IList<T> y) where T : IMetadataTokenProvider
    {
        if (x.Count != y.Count)
            return false;

        for (int i = 0; i < x.Count; i++)
        {
            if (!Equals(x[i], y[i]))
                return false;
        }

        return true;
    }

    public int GetHashCode(IMetadataTokenProvider symbol)
    {
        if (symbol is MemberReference mr)
        {
            mr = ResolveReference(mr);

            if (mr is GenericInstanceType gt)
            {
                return HashCode.Combine(
                    GetHashCode(gt.ElementType),
                    GetHashCode(gt.GenericArguments)
                    );
            }
            else if (mr is GenericInstanceMethod gm)
            {
                return HashCode.Combine(
                    GetHashCode(gm.ElementMethod),
                    GetHashCode(gm.GenericArguments)
                    );
            }
            else if (mr is ArrayType at)
            {
                return GetHashCode(at.ElementType);
            }
            else if (!mr.IsDefinition)
            {
                if (mr is TypeReference tr)
                {
                    return HashCode.Combine(tr.FullName);
                }
                else if (mr.DeclaringType != null)
                {
                    return HashCode.Combine(GetHashCode(mr.DeclaringType), mr.Name);
                }
            }
        }
        else if (symbol is ParameterReference pr)
        {
            HashCode.Combine(pr.Name.GetHashCode(), pr.ParameterType.GetHashCode());
        }

        return symbol.GetHashCode();
    }

    private int GetHashCode<T>(IList<T> symbols) where T : IMetadataTokenProvider
    {
        int hash = 0;
        foreach (var symbol in symbols)
        {
            hash = HashCode.Combine(hash, symbol.GetHashCode());
        }
        return hash;
    }

    /// <summary>
    /// Compares Cecil symbols for equality.
    /// Definitions must be same instance.
    /// References must refer to same definition and have same instantiation etc.
    /// </summary>
    public class CecilEqualityComparer : EqualityComparer<IMetadataTokenProvider>
    {
        private readonly CecilSymbols _symbols;

        public CecilEqualityComparer(CecilSymbols symbols)
        {
            _symbols = symbols;
        }

        public override bool Equals(IMetadataTokenProvider? x, IMetadataTokenProvider? y)
        {
            return _symbols.Equals(x, y);
        }

        public override int GetHashCode([DisallowNull] IMetadataTokenProvider symbol)
        {
            return _symbols.GetHashCode(symbol);
        }
    }

    #endregion

    #region mscorlib
    /// <summary>
    /// The <see cref="CecilSymbols"/> corresponding with the mscorlib assembly used at runtime.
    /// </summary>
    public static CecilSymbols CurrentMscorlib =>
        GetOrCreate([CurrentMscorlibAssembly]);

    /// <summary>
    /// The <see cref="AssemblyDefinition"/> for the current mscorlib loaded in this process.
    /// </summary>
    public static AssemblyDefinition CurrentMscorlibAssembly
    {
        get
        {
            if (_lazyCurrentMscorlibAssembly == null)
            {
                var tmp = AssemblyDefinition.ReadAssembly(typeof(object).Assembly.Location);
                Interlocked.CompareExchange(ref _lazyCurrentMscorlibAssembly, tmp, null);
            }

            return _lazyCurrentMscorlibAssembly;
        }
    }

    private static AssemblyDefinition? _lazyCurrentMscorlibAssembly;
    #endregion
}