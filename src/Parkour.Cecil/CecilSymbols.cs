using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Parkour.Cecil;

using Mono.CompilerServices.SymbolWriter;
using Parkour;
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
    /// The type system used by the imported assemblies.
    /// </summary>
    private TypeSystem? _typeSystem;

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
        _typeSystem = this.Assemblies.FirstOrDefault()?.MainModule?.TypeSystem;
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
                        me => CreateAttributeInfos(cd.CustomAttributes)
                        );

                case MethodDefinition md:
                    var name = StripArity(md.Name, out var arity);
                    return new MethodSymbol(
                        md.GetName(),
                        declaringSymbol,
                        GetAccess(md),
                        GetModifiers(md),
                        md.GenericParameters.Count >= arity
                            ? me => CreateTypeParameters(me, md.GenericParameters.TakeLast(arity))
                            : me => ImmutableList<TypeParameterSymbol>.Empty,
                        () => ImmutableList<TypeSymbol>.Empty,
                        me => CreateParameters(me, md),
                        () => GetTypeSymbol(md.ReturnType)!,
                        me => CreateAttributeInfos(md.CustomAttributes),
                        me => GetImplementedInterfaceMethods(md)
                        );

                case FieldDefinition field:
                    return new FieldSymbol(
                        field.Name,
                        declaringSymbol as TypeSymbol,
                        GetAccess(field),
                        GetModifiers(field),
                        () => GetTypeSymbol(field.FieldType)!,
                        me => CreateAttributeInfos(field.CustomAttributes),
                        field.IsLiteral ? field.Constant : null);

                case ParameterDefinition parameter:
                    return new ParameterSymbol(
                        parameter.Name ?? "",
                        declaringSymbol,
                        GetModifiers(parameter),
                        () => GetTypeSymbol(parameter.ParameterType)!,
                        me => CreateAttributeInfos(parameter.CustomAttributes)
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
                            () => GetTypeSymbol(property.PropertyType)!,
                            property.GetMethod is MethodReference gm
                                ? me => (MethodSymbol)CreateSymbol(gm, me)!
                                : null,
                            property.SetMethod is MethodReference sm
                                ? me => (MethodSymbol)CreateSymbol(sm, me)!
                                : null,
                            me => CreateAttributeInfos(property.CustomAttributes)
                                );
                    }
                    else
                    {
                        return new PropertySymbol(
                            property.Name,
                            declaringSymbol as TypeSymbol,
                            GetAccess(property),
                            GetModifiers(property),
                            () => GetTypeSymbol(property.PropertyType)!,
                            fnBackingField: null,
                            property.GetMethod is MethodDefinition gm
                                ? me => (MethodSymbol)CreateSymbol(gm, me)!
                                : null,
                            property.SetMethod is MethodDefinition sm
                                ? me => (MethodSymbol)CreateSymbol(sm, me)!
                                : null,
                            me => CreateAttributeInfos(property.CustomAttributes)
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
                    Func<ImmutableList<TypeSymbol>> fnBaseTypes =
                        () => GetBaseTypes(type.BaseType, type.Interfaces);
                    Func<TypeSymbol, ImmutableList<Symbol>> fnMembers =
                        me => CreateMembers(type, me);
                    Func<TypeSymbol, ImmutableList<AttributeInfo>> fnAttributes =
                        me => CreateAttributeInfos(type.CustomAttributes);

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
                            fnAttributes
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
                    //else if (type.BaseType != null
                    //    && (this.TypeReferenceComparer.Equals(type.BaseType, CecilDelegate)
                    //        || this.TypeReferenceComparer.Equals(type.BaseType, CecilMulticastDelegate)))
                    //{
                    //    throw new NotImplementedException();
                    //}
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

                case GenericParameter gp:
                    return new TypeParameterSymbol(gp.Name, declaringSymbol);
            }

            return null;
        }

        ImmutableList<Symbol> CreateMembers(TypeDefinition type, MemberSymbol? declaringSymbol)
        {
            var methods = type.Methods.Where(m => (m.IsConstructor || !m.Name.Contains(".")) && CanCreateMember(m)).Select(m => CreateSymbol(m, declaringSymbol));
            var fields = type.Fields.Where(CanCreateMember).Select(f => CreateSymbol(f, declaringSymbol));
            var properties = type.Properties.Where(p => !p.Name.Contains(".") && CanCreateMember(p)).Select(p => CreateSymbol(p, declaringSymbol));
            var events = type.Events.Where(e => !e.Name.Contains(".") && CanCreateMember(e)).Select(e => CreateSymbol(e, declaringSymbol));
            var nestedTypes = type.NestedTypes.Select(t => CreateSymbol(t, declaringSymbol));

            return methods
                .Concat(fields)
                .Concat(properties)
                .Concat(events)
                .Concat(nestedTypes)
                .Where(s => s != null)
                .ToImmutableList()!;
        }

        bool CanCreateMember(IMemberDefinition member)
        {
            switch (member)
            {
                case MethodDefinition md:
                    return CanCreateTypeReference(md.ReturnType)
                        && CanCreateParameters(md.Parameters);
                case FieldDefinition fd:
                    return CanCreateTypeReference(fd.FieldType);
                case PropertyDefinition pd:
                    return CanCreateTypeReference(pd.PropertyType)
                        && CanCreateParameters(pd.Parameters);
                case EventDefinition ed:
                    return CanCreateTypeReference(ed.EventType);
                default:
                    return false;
            }
        }

        bool CanCreateParameters(IEnumerable<ParameterDefinition> parameters)
        {
            foreach (var pd in parameters)
            {
                if (!CanCreateParameter(pd))
                    return false;
            }
            return true;
        }

        bool CanCreateParameter(ParameterDefinition pd)
        {
            return CanCreateTypeReference(pd.ParameterType);
        }

        bool CanCreateTypeReference(TypeReference typeRef)
        {
            switch (typeRef)
            {
                case GenericInstanceType git:
                    return CanCreateReferences(git.GenericArguments);
                case ByReferenceType brt:
                    return false;
                default:
                    return true;
            }
        }

        bool CanCreateReferences(IEnumerable<TypeReference> typeRefs)
        {
            foreach (var typeRef in typeRefs)
            {
                if (!CanCreateTypeReference(typeRef))
                    return false;
            }
            return true;
        }

        ImmutableList<ParameterSymbol> CreateParameters(MemberSymbol? declaringSymbol, MethodReference method) =>
            method.Parameters.Select(p => (ParameterSymbol)CreateSymbol(p, declaringSymbol)!).ToImmutableList();

        ImmutableList<TypeParameterSymbol> CreateTypeParameters(MemberSymbol? declaringSymbol, IEnumerable<GenericParameter> genericParameters) =>
            genericParameters.Select(p => (TypeParameterSymbol)CreateSymbol(p, declaringSymbol)).ToImmutableList();


        ImmutableList<AttributeInfo> CreateAttributeInfos(IEnumerable<CustomAttribute> data) =>
            data.Select(d => CreateAttributeInfo(d))
                .OfType<AttributeInfo>()
                .ToImmutableList();

        AttributeInfo? CreateAttributeInfo(CustomAttribute data)
        {
            if (TryGetSymbol(data.Constructor, out var symbol)
                && symbol is ConstructorSymbol constructor)
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
                if (arg.Value is TypeReference type)
                {
                    return new AttributeTypeValue(GetTypeSymbol(type));
                }
                else if (arg.Value is CustomAttributeArgument[] args)
                {
                    var elementType = GetTypeSymbol(arg.Type.GetElementType());
                    var values = args.Select(a => CreateValue(a)).ToImmutableList();
                    return new AttributeArrayValue(elementType!, values);
                }
                else
                {
                    return new AttributeConstantValue(arg.Value);
                }
            }
        }
    }

    /// <summary>
    /// Gets the interface methods that this method implements.
    /// </summary>
    private ImmutableList<MethodSymbol> GetImplementedInterfaceMethods(MethodDefinition implementationMethod)
    {
        var declaringType = ((MethodReference)implementationMethod).DeclaringType;

        if (declaringType == null)
            return ImmutableList<MethodSymbol>.Empty;

        List<MethodSymbol>? list = null;

        // find all interface methods that this method implements
        var interfaces = declaringType.Resolve().Interfaces;
        foreach (var iface in interfaces)
        {
            var ifaceSymbol = GetTypeSymbol(iface.InterfaceType);

            foreach (var methodSymbol in ifaceSymbol!.Members.OfType<MethodSymbol>())
            {
                var matchingMember = CecilHelpers.GetMatchingMember(methodSymbol, iface.InterfaceType.Resolve());
                if (matchingMember is MethodReference ifaceMethodRef
                    && CecilHelpers.IsImplicitInterfaceImplementation(implementationMethod, ifaceMethodRef))
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
                return [GetTypeSymbol(baseType)!];

            return GetTypeSymbols(new[] { baseType }.Concat(interfaces.Select(i => i.InterfaceType)));
        }
        else if (interfaces.Count > 0)
        {
            return GetTypeSymbols(interfaces.Select(i => i.InterfaceType));
        }
        else
        {
            return ImmutableList<TypeSymbol>.Empty;
        }
    }

    public static Access GetAccess(IMemberDefinition definition) =>
        definition switch
        {
            TypeDefinition type =>
                type.IsPublic ? Access.Public
                : type.IsNestedPublic ? Access.Public
                : type.IsNestedFamily ? Access.Protected
                : type.IsNestedFamilyAndAssembly ? Access.ProtectedAndInternal
                : type.IsNestedFamilyOrAssembly ? Access.ProtectedOrInternal
                : type.IsPublic ? Access.Public
                : type.IsNotPublic ? Access.Internal
                : Access.Private,
            FieldDefinition field =>
                field.IsPublic ? Access.Public
                : field.IsAssembly ? Access.Internal
                : field.IsFamily ? Access.Protected
                : field.IsFamilyAndAssembly ? Access.ProtectedAndInternal
                : field.IsFamilyOrAssembly ? Access.ProtectedOrInternal
                : Access.Private,
            PropertyDefinition property =>
                GetAccess(property.GetMethod!),
            MethodDefinition method =>
                method.IsPublic ? Access.Public
                : method.IsAssembly ? Access.Internal
                : method.IsFamily ? Access.Protected
                : method.IsFamilyAndAssembly ? Access.ProtectedAndInternal
                : method.IsFamilyOrAssembly ? Access.ProtectedOrInternal
                : Access.Private,
            _ => Access.Private
        };

    public static BitSet<Modifier> GetModifiers(IMemberDefinition definition) =>
        definition switch
        {
            TypeDefinition type =>
                (type.IsAbstract ? Modifier.Abstract : Modifier.None)
                | (type.IsSealed ? Modifier.Sealed : Modifier.None),
            FieldDefinition field =>
                (field.IsStatic ? Modifier.Static : Modifier.None)
                | (field.IsLiteral ? Modifier.Constant : Modifier.None),
            PropertyDefinition property =>
                // borrow modifiers from the get method
                GetModifiers(property.GetMethod!).Remove(Modifier.HideBySig).Remove(Modifier.Special),
            MethodDefinition method =>
                (method.IsStatic ? Modifier.Static : Modifier.None)
                | (method.IsAbstract ? Modifier.Abstract : Modifier.None)
                | (method.IsVirtual ? Modifier.Virtual : Modifier.None)
                | (method.IsFinal ? Modifier.Sealed : Modifier.None)
                | (method.IsHideBySig ? Modifier.HideBySig : Modifier.None)
                | (method.IsSpecialName ? Modifier.Special : Modifier.None),
            _ => Modifier.None
        };

    public static BitSet<Modifier> GetModifiers(ParameterDefinition definition)
    {
        var isIn = (definition.Attributes & ParameterAttributes.In) != 0;
        var isOut = (definition.Attributes & ParameterAttributes.Out) != 0;
        return isIn && isOut ? Modifier.Ref
            : isIn ? Modifier.In
            : isOut ? Modifier.Out
            : Modifier.None;
    }

    #endregion

    #region Get symbols from Cecil types
    /// <summary>
    /// Gets the symbol corresponding to the Cecil metadata object.
    /// </summary>
    private Symbol GetSymbol(IMetadataTokenProvider cecilSymbol)
    {
        if (TryGetSymbol(cecilSymbol, out var symbol))
            return symbol;
        throw new InvalidOperationException($"Cannot determine symbol for {cecilSymbol.GetMetadataName()}");
    }

    /// <summary>
    /// Gets the symbol corresponding to the Cecil metadata object.
    /// </summary>
    private bool TryGetSymbol(IMetadataTokenProvider cecilSymbol, [NotNullWhen(true)] out Symbol? symbol)
    {
        // is this a known symbol?
        if (_cecilToSymbolMap.TryGetValue(cecilSymbol, out symbol))
            return true;

        symbol = Get();
        return symbol != null;

        Symbol? Get()
        {
            if (cecilSymbol is MemberReference member)
            {
                // convert to definition if possible
                if (TryGetDefinition(member, out var definition))
                    member = (MemberReference)definition;

                // is resolved reference a known symbol?
                if (_cecilToSymbolMap.TryGetValue(member, out var symbol))
                    return symbol;

                if (member is ArrayType at)
                {
                    var elementType = GetTypeSymbol(at.ElementType);
                    return this.GetArray(elementType, at.Rank);
                }
                else if (member is GenericInstanceType gt)
                {
                    var elementSymbol = GetTypeSymbol(gt.ElementType);
                    return GetConstructedMember(elementSymbol, gt.GenericArguments.ToList());
                }
                else if (member is GenericInstanceMethod gm)
                {
                    var elementMethod = (MethodSymbol)GetSymbol(gm.ElementMethod);
                    return GetConstructedMember(elementMethod, gm.GenericArguments.ToList());
                }
                else if (member is GenericParameter gp)
                {
                    Symbol? tp = null;
                    var ownerSymbol = (MemberSymbol)GetSymbol(gp.Owner);
                    while (ownerSymbol != null)
                    {
                        if (ownerSymbol is TypeSymbol ts)
                        {
                            tp = ts.TypeParameters.FirstOrDefault(x => gp.MatchesName(x.Name));
                        }
                        else if (ownerSymbol is MethodSymbol ms)
                        {
                            tp = ms.TypeParameters.FirstOrDefault(x => gp.MatchesName(x.Name));
                        }
                        if (tp != null)
                            return tp;
                        ownerSymbol = ownerSymbol.DeclaringSymbol as MemberSymbol;
                    }
                    return tp;
                }
                else if (member.DeclaringType != null)
                {
                    // get declaring type and find equivalent symbol
                    var declaringType = GetTypeSymbol(member.DeclaringType);
                    return FindMember(declaringType, member);
                }
                else if (CecilHelpers.TryGetDottedPath(member, out var path))
                {
                    return GetSymbol(path);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Gets or type symbol corresponding to the Cecil <see cref="TypeReference"/>.
    /// </summary>
    private TypeSymbol GetTypeSymbol(TypeReference type)
    {
        return GetSymbol(type) is TypeSymbol ts 
            ? ts
            : throw new InvalidOperationException("Could not get corresponding type symbol");
    }

    /// <summary>
    /// Gets a list of <see cref="TypeSymbol"/> corresponding to the list of <see cref="TypeReference"/>.
    /// </summary>
    private ImmutableList<TypeSymbol> GetTypeSymbols(IEnumerable<TypeReference> types)
    {
        if (!types.Any())
            return ImmutableList<TypeSymbol>.Empty;
        var list = types.Select(GetTypeSymbol).ToImmutableList();
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
                var parameterTypes = GetTypeSymbols(cd.Parameters.Select(p => p.ParameterType));
                return declaringType.GetFirstMember<ConstructorSymbol>(cs => 
                    cs.IsStatic == cd.IsStatic
                    && Matches(cs.Parameters, parameterTypes));

            case MethodReference mr:
                var name = StripArity(mr.Name, out var arity);
                parameterTypes = GetTypeSymbols(mr.Parameters.Select(p => p.ParameterType));
                return declaringType.FindMethod(name, parameterTypes);

            case FieldReference fr:
                return declaringType.GetFirstMember<FieldSymbol>(fr.Name);

            case PropertyReference pr:
                if (pr.Parameters.Count > 0)
                {
                    parameterTypes = GetTypeSymbols(pr.Parameters.Select(p => p.ParameterType));
                    return declaringType.GetFirstMember<IndexerSymbol>(nx => Matches(nx.GetMethod!.Parameters, parameterTypes));
                }
                else
                {
                    return declaringType.GetFirstMember<PropertySymbol>(pr.Name);
                }

            case GenericParameter gp:
                if (gp.Owner != null)
                {
                    var owner = (MemberSymbol)GetSymbol((MemberReference)gp.Owner);
                    return GetTypeParameter(owner, gp.Name);
                }
                else
                {
                    return GetTypeParameter(declaringType, gp.Name);
                }

            case TypeReference td:
                name = StripArity(td.Name, out arity);
                return declaringType.FindType(name, arity);

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
                    var equivalentSymbol = constructedDeclaringType.Members.OfType<TypeSymbol>().FirstOrDefault(t => t.Definition == symbol);
                    symbol = equivalentSymbol ?? ts;
                }
                else if (symbol is MethodSymbol ms)
                {
                    var equivalentSymbol = constructedDeclaringType.Members.OfType<MethodSymbol>().FirstOrDefault(t => t.Definition == symbol);
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
            var symbolTypeArgs = GetTypeSymbols(typeArgs.Take(symbol.Arity));
            typeArgs.RemoveRange(0, symbol.Arity);
            return this.GetConstructed(symbol, symbolTypeArgs);
        }

        return symbol;
    }

    #endregion

    #region Get corresponding Cecil definitions

    /// <summary>
    /// Gets the equivalent Cecil symbol definition for the given parkour <see cref="Symbol"/>.
    /// </summary>
    public bool TryGetDefinition(Symbol symbol, [NotNullWhen(true)] out IMetadataTokenProvider? cecilDefinition) =>
        _symbolToCecilMap.TryGetValue(symbol, out cecilDefinition);

    /// <summary>
    /// Gets the definition that the reference is referring to.
    /// </summary>
    private bool TryGetDefinition(MemberReference reference, [NotNullWhen(true)] out IMemberDefinition? cecilDefinition)
    {
        // already a definition?
        if (reference is IMemberDefinition md)
        {
            cecilDefinition = md;
            return true;
        }

        switch (reference)
        {
            case GenericInstanceMethod:
            case GenericInstanceType:
            case ArrayType:
                cecilDefinition = null;
                return false;
            case TypeReference typeRef:
                // lookup definition by full name
                cecilDefinition = GetFirstTypeDefinition(typeRef.FullName);
                return cecilDefinition != null;
        }

        var declaringTypeDef = reference.DeclaringType != null
                && TryGetDefinition(reference.DeclaringType, out var declaringDef)
            ? (TypeDefinition)declaringDef
            : null;

        // otherwise, find matching member in declaring type
        if (declaringTypeDef != null)
        {
            cecilDefinition = null;

            switch (reference)
            {
                case MethodReference methodRef:
                    cecilDefinition = declaringTypeDef.Methods.FirstOrDefault(m => m.Name == methodRef.Name 
                        && CecilHelpers.SignatureMatches(m, methodRef));
                    break;
                case FieldReference fieldRef:
                    cecilDefinition = declaringTypeDef.Fields.FirstOrDefault(f => f.Name == fieldRef.Name);
                    break;
                case PropertyReference propertyRef:
                    cecilDefinition = declaringTypeDef.Properties.FirstOrDefault(p => p.Name == propertyRef.Name);
                    break;
                case EventReference eventRef:
                    cecilDefinition = declaringTypeDef.Events.FirstOrDefault(e => e.Name == eventRef.Name);
                    break;
            }

            return cecilDefinition != null;
        }

        cecilDefinition = null;
        return false;
    }
    #endregion

    #region Find Cecil types by name
    /// <summary>
    /// Gets all the <see cref="TypeDefinition"/> objects with the full name.
    /// </summary>
    public ImmutableList<TypeDefinition> GetTypeDefinitions(string fullName)
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
    public TypeDefinition? GetFirstTypeDefinition(string fullName) =>
        GetTypeDefinitions(fullName).FirstOrDefault();

    /// <summary>
    /// The definition of the object type.
    /// </summary>
    internal TypeDefinition CecilObject => _lazyObjectType ??= GetFirstTypeDefinition("System.Object")!;
    private TypeDefinition? _lazyObjectType;

    internal TypeDefinition CecilVoid => _lazyVoidDefinition ??= GetFirstTypeDefinition("System.Void")!;
    private TypeDefinition? _lazyVoidDefinition;

    internal TypeDefinition CecilDecimal => _lazyDecimalType ??= GetFirstTypeDefinition("System.Decimal")!;
    private TypeDefinition? _lazyDecimalType;

    internal TypeDefinition CecilDateTime => _lazyDateTimeType ??= GetFirstTypeDefinition("System.DateTime")!;
    private TypeDefinition? _lazyDateTimeType;

    internal TypeDefinition CecilType => _lazyTypeType ??= GetFirstTypeDefinition("System.Type")!;
    private TypeDefinition? _lazyTypeType;

    internal TypeDefinition CecilDelegate => _lazyDelegateType ??= GetFirstTypeDefinition("System.Delegate")!;
    private TypeDefinition? _lazyDelegateType;

    internal TypeDefinition CecilMulticastDelegate => _lazyMulitcastDelegateType ??= GetFirstTypeDefinition("System.MulticastDelegate")!;
    private TypeDefinition? _lazyMulitcastDelegateType;

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