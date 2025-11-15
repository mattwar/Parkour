using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parkour.Cecil;

using Mono.Cecil.Rocks;
using Parkour;
using Semantics;
using Symbols;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

/// <summary>
/// A <see cref="SemanticEmitter"/> that emits into a <see cref="ModuleBuilder"/>
/// </summary>
public partial class CecilEmitter : SemanticEmitter
{
    private readonly CecilSymbols _externalSymbols;
    private readonly AssemblyDefinition _assembly;
    private readonly ModuleDefinition _module;
    private readonly List<Diagnostic> _diagnostics;

    private readonly Dictionary<Symbol, IMetadataTokenProvider> _symbolToDefinition =
        new Dictionary<Symbol, IMetadataTokenProvider>();

    public AssemblyDefinition Assembly => _assembly;
    public ModuleDefinition Module => _module;

    public CecilEmitter(
        CecilSymbols externalSymbols,
        AssemblyDefinition assembly,
        ModuleDefinition? module = null)
    {
        _externalSymbols = externalSymbols;
        _assembly = assembly;
        _module = module ?? assembly.MainModule;
        _diagnostics = new List<Diagnostic>();
    }

    public CecilEmitter(
        CecilSymbols externalSymbols,
        AssemblyDefinition assembly,
        string? moduleName,
        ModuleKind moduleKind = ModuleKind.Dll)
        : this(
              externalSymbols,
              assembly,
              CreateModule(assembly, moduleName, moduleKind)
              )
    {
    }

    public CecilEmitter(
        CecilSymbols externalSymbols,
        string assemblyName,
        string? moduleName = null,
        ModuleKind moduleKind = ModuleKind.Dll)
        : this(
              externalSymbols,
              AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(assemblyName, new Version(1, 0)), $"Module0", ModuleKind.Dll))
    {
    }

    private static ModuleDefinition CreateModule(AssemblyDefinition assembly, string? moduleName, ModuleKind moduleKind)
    {
        var module = ModuleDefinition.CreateModule(moduleName ?? $"Module{assembly.Modules.Count}", moduleKind);
        assembly.Modules.Add(module);
        return module;
    }


    /// <summary>
    /// Emits all lowered types and members into a <see cref="ModuleBuilder"/>.
    /// </summary>
    public override EmitResult Emit(
        SemanticLowering lowering)
    {
        var declarations = lowering.Elements
            .OfType<Declaration>()
            .ToImmutableList();

        Declare(declarations);

        return new EmitResult(_diagnostics.ToImmutableList());
    }

    protected override void DeclareType(TypeDeclaration declaration)
    {
        var typeSymbol = declaration.Symbol as TypeSymbol;
        if (typeSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare type for unbound type declaration '{declaration.Name}'").WithLocation(declaration.Location));
            return;
        }

        if (_symbolToDefinition.ContainsKey(typeSymbol))
        {
            _diagnostics.Add(new Diagnostic($"Type '{typeSymbol.FullName}' is already declared.").WithLocation(declaration.Location));
            return;
        }

        var typeDef = new TypeDefinition(typeSymbol.Namespace, typeSymbol.Name, GetTypeAttributes(typeSymbol));

        if (typeSymbol.DeclaringSymbol is TypeSymbol pts)
        {
            if (_symbolToDefinition.TryGetValue(typeSymbol.DeclaringSymbol, out var parentDef)
                && parentDef is TypeDefinition parentType)
            {
                parentType.NestedTypes.Add(typeDef);
            }
            else
            {
                _diagnostics.Add(new Diagnostic($"Cannot declared nested type '{typeSymbol.FullName}' for undeclared type.").WithLocation(declaration.Location));
                return;
            }
        }
        else
        {
            _module.Types.Add(typeDef);
        }

        _symbolToDefinition.Add(typeSymbol, typeDef);

        // declare type parameters too
        foreach (var tp in typeSymbol.TypeParameters)
        {
            var gp = new GenericParameter(tp.Name, typeDef);
            typeDef.GenericParameters.Add(gp);
            _symbolToDefinition.Add(tp, gp);
        }
    }

    protected override void DeclareBaseTypesAndInterfaces(TypeDeclaration declaration)
    {
        if (declaration.Symbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare base type and interfaces for unbound type declaration '{declaration.Name}'").WithLocation(declaration.Location));
            return;
        }

        var typeSymbol = (TypeSymbol)declaration.Symbol;
        if (_symbolToDefinition.TryGetValue(declaration.Symbol, out var def)
            && def is TypeDefinition typeDef)
        {
            // set base types and interfaces
            var hasBaseType = false;
            foreach (var bt in typeSymbol.BaseTypes)
            {
                var btr = GetEmitTypeReference(bt);
                if (bt.IsInterface)
                {
                    typeDef.Interfaces.Add(new InterfaceImplementation(btr));
                }
                else if (bt.IsClass && !typeSymbol.IsValueType && !hasBaseType)
                {
                    typeDef.BaseType = btr;
                    hasBaseType = true;
                }
            }

            if (!hasBaseType)
            {
                if (typeSymbol.IsValueType)
                {
                    typeDef.BaseType = GetEmitTypeReference(_externalSymbols.GetTypeSymbol(typeof(ValueType)));
                }
                else if (typeSymbol.IsClass)
                {
                    typeDef.BaseType = _module.TypeSystem.Object;
                }
            }
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare base type and interfaces for undeclared type '{typeSymbol.FullName}'").WithLocation(declaration.Location));
        }
    }

    protected override void DeclareMember(MemberDeclaration declaration)
    {
        var memberSymbol = declaration.Symbol as MemberSymbol;
        if (memberSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare member for unbound member declaration '{declaration.Name}'").WithLocation(declaration.Location));
            return;
        }

        if (_symbolToDefinition.TryGetValue(memberSymbol, out _))
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare member for member that is already declared '{declaration.Name}'").WithLocation(declaration.Location));
        }

        if (!_symbolToDefinition.TryGetValue(memberSymbol.DeclaringSymbol!, out var declaringTypeDef))
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare member for undeclared type '{memberSymbol.DeclaringSymbol?.FullName}'").WithLocation(declaration.Location));
            return;
        }

        Declare(declaration, declaringTypeDef);

        IMemberDefinition? Declare(MemberDeclaration declaration, IMetadataTokenProvider declaringTypeDef)
        {
            var declaringType = (TypeDefinition)declaringTypeDef;

            switch (declaration.Symbol)
            {
                case FieldSymbol field:
                    var fieldType = GetEmitTypeReference(field.Type);
                    var fieldAttributes = GetFieldAttributes(field);
                    var fieldDef = new FieldDefinition(field.Name, fieldAttributes, fieldType);
                    _symbolToDefinition.Add(field, fieldDef);
                    declaringType.Fields.Add(fieldDef);
                    return fieldDef;

                case ConstructorSymbol constructor:
                    var ctorAttributes = GetMethodAttributes(constructor);
                    ctorAttributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                    var constructorDef = new MethodDefinition(constructor.Name, ctorAttributes, _module.TypeSystem.Void);
                    _symbolToDefinition.Add(constructor, constructorDef);
                    AddParameters(constructorDef.Parameters, constructor.Parameters);
                    declaringType.Methods.Add(constructorDef);
                    return constructorDef;

                case MethodSymbol method:
                    var methodAttributes = GetMethodAttributes(method);
                    var methodDef = new MethodDefinition(method.Name, methodAttributes, GetEmitTypeReference(method.ReturnType));
                    _symbolToDefinition.Add(method, methodDef);
                    AddParameters(methodDef.Parameters, method.Parameters);
                    foreach (var tp in method.TypeParameters)
                    {
                        var gp = new GenericParameter(tp.Name, methodDef);
                        methodDef.GenericParameters.Add(gp);
                        _symbolToDefinition.Add(tp, gp);
                    }
                    declaringType.Methods.Add(methodDef);
                    return methodDef;

                case PropertySymbol property when declaration is PropertyDeclaration propertyDecl:
                    var propertyAttributes = GetPropertyAttributes(property);
                    var propertyType = GetEmitTypeReference(property.Type);
                    var propertyDef = new PropertyDefinition(property.Name, propertyAttributes, propertyType);
                    _symbolToDefinition.Add(property, propertyDef);
                    declaringType.Properties.Add(propertyDef);
                    if (propertyDecl.GetMethod != null)
                    {
                        var getMethodDef = (MethodDefinition?)Declare(propertyDecl.GetMethod, declaringTypeDef);
                        propertyDef.GetMethod = getMethodDef;
                    }
                    if (propertyDecl.SetMethod != null)
                    {
                        var setMethodDef = (MethodDefinition?)Declare(propertyDecl.SetMethod, declaringTypeDef);
                        propertyDef.SetMethod = setMethodDef;
                    }
                    if (propertyDecl.BackingField != null)
                    {
                        Declare(propertyDecl.BackingField, declaringTypeDef);
                    }
                    return propertyDef;

                case IndexerSymbol indexer when declaration is IndexerDeclaration indexerDecl:
                    var indexerAttributes = GetPropertyAttributes(indexer);
                    var indexerType = GetEmitTypeReference(indexer.ElementType);
                    var indexerDef = new PropertyDefinition(indexer.Name, indexerAttributes, indexerType);
                    _symbolToDefinition.Add(indexer, indexerDef);
                    AddParameters(indexerDef.Parameters, indexer.GetMethod!.Parameters, addToMap: false);
                    declaringType.Properties.Add(indexerDef);
                    if (indexerDecl.GetMethod != null)
                        DeclareMember(indexerDecl.GetMethod);
                    if (indexerDecl.SetMethod != null)
                        DeclareMember(indexerDecl.SetMethod);
                    return indexerDef;

                default:
                    return null;
            }
        }


        void AddParameters(Collection<ParameterDefinition> definitions, IEnumerable<ParameterSymbol> parameters, bool addToMap = true)
        {
            foreach (var p in parameters)
            {
                var pd = CreateParameter(p, addToMap);
                definitions.Add(pd);
            }
        }

        ParameterDefinition CreateParameter(ParameterSymbol parameter, bool addToMap)
        {
            var type = GetEmitTypeReference(parameter.Type);
            var attributes = GetParameterAttributes(parameter);
            var definition = new ParameterDefinition(parameter.Name, attributes, type);
            if (addToMap)
                _symbolToDefinition[parameter] = definition;
            return definition;
        }
    }

    protected override void DeclareAccessors(MemberDeclaration declaration)
    {
    }

    protected override void DeclareAttributes(MemberDeclaration declaration)
    {
        if (declaration.Symbol != null)
        {
            AddCustomAttributes(declaration.Symbol);
        }
    }

    private void AddCustomAttributes<TSymbol>(IEnumerable<TSymbol> symbols) where TSymbol : Symbol
    {
        foreach (var symbol in symbols)
        {
            AddCustomAttributes(symbol);
        }
    }

    private void AddCustomAttributes(Symbol symbol)
    {
        if (symbol != null
            && _symbolToDefinition.TryGetValue(symbol, out var memberDef))
        {
            if (memberDef is ICustomAttributeProvider provider)
            {
                switch (symbol)
                {
                    case TypeParameterSymbol typeParameter:
                        AddAttributes(provider, typeParameter.Attributes);
                        break;
                    case TypeSymbol type:
                        AddAttributes(provider, type.Attributes);
                        AddCustomAttributes(type.TypeParameters);
                        break;
                    case ConstructorSymbol constructor:
                        AddAttributes(provider, constructor.Attributes);
                        AddCustomAttributes(constructor.Parameters);
                        break;
                    case MethodSymbol method:
                        AddAttributes(provider, method.Attributes);
                        AddCustomAttributes(method.Parameters);
                        AddCustomAttributes(method.TypeParameters);
                        break;
                    case ParameterSymbol parameter:
                        AddAttributes(provider, parameter.Attributes);
                        break;
                    case FieldSymbol field:
                        AddAttributes(provider, field.Attributes);
                        break;
                    case PropertySymbol property:
                        AddAttributes(provider, property.Attributes);
                        if (property.GetMethod != null)
                            AddCustomAttributes(property.GetMethod);
                        if (property.SetMethod != null)
                            AddCustomAttributes(property.SetMethod);
                        break;
                    case IndexerSymbol indexer:
                        AddAttributes(provider, indexer.Attributes);
                        if (indexer.GetMethod != null)
                            AddCustomAttributes(indexer.GetMethod);
                        if (indexer.SetMethod != null)
                            AddCustomAttributes(indexer.SetMethod);
                        break;
                }
            }
        }

        void AddAttributes(ICustomAttributeProvider member, IEnumerable<AttributeInfo> attributes)
        {
            // don't re-add attribute if they have already been added
            if (member.CustomAttributes.Count > 0)
                return;

            foreach (var attrInfo in attributes)
            {
                var customAttr = CreateCustomAttribute(attrInfo);
                member.CustomAttributes.Add(customAttr);
            }
        }
    }

    protected override void EmitMemberBody(MemberDeclaration declaration)
    {
        if (declaration.Symbol != null
            && _symbolToDefinition.TryGetValue(declaration.Symbol, out var memberDef))
        {
            switch (declaration)
            {
                case MethodDeclaration methodDecl:
                    if (methodDecl.Body != null 
                        && memberDef is MethodDefinition methodDef
                        && methodDecl.Symbol is MethodSymbol methodSymbol)
                    {
                        var ilEmitter = new ILEmitter(this, _externalSymbols, methodDef.Body, _diagnostics);
                        var bodyBuilder = new StandardBodyBuilder(methodSymbol, ilEmitter);
                        bodyBuilder.BuildBody(methodDecl.Body, methodSymbol.ReturnType, methodDecl.ReturnLabel);
                    }
                    break;
                case ConstructorDeclaration constructorDecl:
                    if (constructorDecl.Body != null 
                        && memberDef is MethodDefinition constructorDef
                        && constructorDecl.Symbol is ConstructorSymbol constructorSymbol)
                    {
                        var ilEmitter = new ILEmitter(this, _externalSymbols, constructorDef.Body, _diagnostics);
                        var bodyBuilder = new StandardBodyBuilder(constructorSymbol, ilEmitter);
                        bodyBuilder.BuildBody(constructorDecl.Body, _externalSymbols.Void, constructorDecl.ReturnLabel);
                    }
                    break;
                case PropertyDeclaration propertyDecl:
                    if (propertyDecl.GetMethod != null)
                        EmitMemberBody(propertyDecl.GetMethod);
                    if (propertyDecl.SetMethod != null)
                        EmitMemberBody(propertyDecl.SetMethod);
                    break;

                case IndexerDeclaration indexerDecl:
                    if (indexerDecl.GetMethod != null)
                        EmitMemberBody(indexerDecl.GetMethod);
                    if (indexerDecl.SetMethod != null)
                        EmitMemberBody(indexerDecl.SetMethod);
                    break;
            }
        }
    }

    static TypeAttributes GetTypeAttributes(TypeSymbol ts)
    {
        TypeAttributes attrs = default;

        switch (ts)
        {
            case ClassSymbol:
            case StructSymbol:
                attrs |= TypeAttributes.Class;

                if (ts.Modifiers.Contains(Modifier.Abstract))
                    attrs |= TypeAttributes.Abstract;
                else if (ts.Modifiers.Contains(Modifier.Sealed))
                    attrs |= TypeAttributes.Sealed;
                else if (ts.Modifiers.Contains(Modifier.Special))
                    attrs |= TypeAttributes.SpecialName;
                break;
            case InterfaceSymbol:
                attrs |= TypeAttributes.Interface | TypeAttributes.Abstract;
                break;
        }


        var isNested = ts.DeclaringSymbol is TypeSymbol;
        if (ts.Access == Access.Private)
        {
            attrs |= isNested ? TypeAttributes.NestedPrivate : TypeAttributes.NotPublic;
        }
        else if (ts.Access == Access.Public)
        {
            attrs |= isNested ? TypeAttributes.NestedPublic : TypeAttributes.NotPublic;
        }
        else if (ts.Access == Access.Internal)
        {
            attrs |= isNested ? TypeAttributes.NestedAssembly : TypeAttributes.NotPublic;
        }
        else if (ts.Access == Access.Protected)
        {
            attrs |= isNested ? TypeAttributes.NestedFamily : TypeAttributes.NotPublic;
        }
        else if (ts.Access == Access.ProtectedOrInternal)
        {
            attrs |= isNested ? TypeAttributes.NestedFamORAssem : TypeAttributes.NotPublic;
        }
        else if (ts.Access == Access.ProtectedAndInternal)
        {
            attrs |= isNested ? TypeAttributes.NestedFamANDAssem : TypeAttributes.NotPublic;
        }

        return attrs;
    }

    private static FieldAttributes GetFieldAttributes(FieldSymbol field)
    {
        FieldAttributes attrs = default;

        if (field.Modifiers.Contains(Modifier.Static))
            attrs |= FieldAttributes.Static;

        if (field.Modifiers.Contains(Modifier.Special))
            attrs |= FieldAttributes.SpecialName;

        if (field.Modifiers.Contains(Modifier.Constant))
            attrs |= FieldAttributes.Literal;

        if (field.Access == Access.Private)
        {
            attrs |= FieldAttributes.Private;
        }
        else if (field.Access == Access.Public)
        {
            attrs |= FieldAttributes.Public;
        }
        else if (field.Access == Access.Protected)
        {
            attrs |= FieldAttributes.Family;
        }
        else if (field.Access == Access.Internal)
        {
            attrs |= FieldAttributes.Assembly;
        }
        else if (field.Access == Access.ProtectedOrInternal)
        {
            attrs |= FieldAttributes.FamORAssem;
        }
        else if (field.Access == Access.ProtectedAndInternal)
        {
            attrs |= FieldAttributes.FamANDAssem;
        }

        return attrs;
    }

    private static MethodAttributes GetMethodAttributes(MemberSymbol method)
    {
        MethodAttributes attrs = default;

        if (method.Modifiers.Contains(Modifier.Static))
            attrs |= MethodAttributes.Static;

        if (method.DeclaringSymbol == null
            || method.DeclaringSymbol is NamespaceSymbol)
            attrs |= MethodAttributes.Static;

        if (method.Modifiers.Contains(Modifier.Special))
            attrs |= MethodAttributes.SpecialName;

        if (method.Access == Access.Private)
        {
            attrs |= MethodAttributes.Private;
        }
        else if (method.Access == Access.Public)
        {
            attrs |= MethodAttributes.Public;
        }
        else if (method.Access == Access.Protected)
        {
            attrs |= MethodAttributes.Family;
        }
        else if (method.Access == Access.Internal)
        {
            attrs |= MethodAttributes.Assembly;
        }
        else if (method.Access == Access.ProtectedOrInternal)
        {
            attrs |= MethodAttributes.FamORAssem;
        }
        else if (method.Access == Access.ProtectedAndInternal)
        {
            attrs |= MethodAttributes.FamANDAssem;
        }

        return attrs;
    }

    private static ParameterAttributes GetParameterAttributes(
        ParameterSymbol parameter)
    {
        return ParameterAttributes.None;
    }

    private static PropertyAttributes GetPropertyAttributes(
        MemberSymbol property)
    {
        return PropertyAttributes.None;
    }

    private CustomAttribute CreateCustomAttribute(AttributeInfo info)
    {
        var constructor = GetEmitSymbolReference<MethodReference>(info.Constructor);
        var argValues = info.Arguments.Select(a => GetValue(a.Value)).ToArray();
        var props = info.Members.Where(m => m.Member is PropertySymbol);
        var propInfos = props.Select(p => GetEmitSymbolReference<PropertyReference>(p.Member)).ToArray();
        var propValues = props.Select(p => GetValue(p.Value)).ToArray();
        var fields = info.Members.Where(m => m.Member is FieldSymbol);
        var fieldInfos = fields.Select(f => GetEmitSymbolReference<FieldReference>(f.Member)).ToArray();
        var fieldValues = fields.Select(f => GetValue(f.Value)).ToArray();

        var attr = new CustomAttribute(constructor);

        foreach (var arg in info.Arguments)
        {
            attr.ConstructorArguments.Add(GetValue(arg.Value));
        }

        foreach (var propArg in info.Members.Where(m => m.Member is PropertySymbol))
        {
            attr.Properties.Add(new CustomAttributeNamedArgument(propArg.Member.Name, GetValue(propArg.Value)));
        }

        foreach (var fieldArg in info.Members.Where(m => m.Member is FieldSymbol))
        {
            attr.Fields.Add(new CustomAttributeNamedArgument(fieldArg.Member.Name, GetValue(fieldArg.Value)));
        }

        return attr;

        CustomAttributeArgument GetValue(AttributeValue value)
        {
            switch (value)
            {
                case AttributeConstantValue cv:
                    var valueTypeSymbol = _externalSymbols.GetTypeSymbol(cv.Value != null ? cv.Value.GetType() : typeof(object));
                    var valueTypeRef = GetEmitTypeReference(valueTypeSymbol);
                    return new CustomAttributeArgument(valueTypeRef, cv.Value);
                case AttributeTypeValue tv:
                    return new CustomAttributeArgument(_externalSymbols.CecilType, GetEmitTypeReference(tv.Type));
                case AttributeArrayValue av:
                    var elemType = GetEmitTypeReference(av.ElementType);
                    var arrayType = new ArrayType(elemType);
                    var values = av.Values.Select(v => GetValue(v)).ToArray();
                    return new CustomAttributeArgument(elemType, values);
                default:
                    throw new InvalidOperationException($"Unhandled attribute value '{value.GetType().Name}'");
            }
        }
    }

    private IMetadataTokenProvider? GetEmittedSymbol(Symbol symbol)
    {
        // this is one of the declared symbols..
        if (_symbolToDefinition.TryGetValue(symbol, out var def))
            return def;
        return null;
    }

    /// <summary>
    /// The symbols that have been imported into the module
    /// </summary>
    private readonly Dictionary<IMetadataTokenProvider, IMetadataTokenProvider> _importedSymbols =
        new Dictionary<IMetadataTokenProvider, IMetadataTokenProvider>();

    /// <summary>
    /// Gets the imported version of the symbol.
    /// </summary>
    private IMetadataTokenProvider GetImportedSymbol(IMetadataTokenProvider symbol)
    {
        if (!_importedSymbols.TryGetValue(symbol, out var imported))
        {
            imported = Import();
            if (imported != null)
                _importedSymbols[symbol] = imported;
        }

        return imported ?? symbol;

        IMetadataTokenProvider? Import()
        {
            switch (symbol)
            {
                case TypeReference typeRef:
                    switch (typeRef.FullName)
                    {
                        case "System.Object":
                            return _module.TypeSystem.Object;
                        case "System.Void":
                            return _module.TypeSystem.Void;
                        case "System.Boolean":
                            return _module.TypeSystem.Boolean;
                        case "System.Int64":
                            return _module.TypeSystem.Int64;
                        case "System.Int32":
                            return _module.TypeSystem.Int32;
                        case "System.Int16":
                            return _module.TypeSystem.Int16;
                        case "System.Int8":
                            return _module.TypeSystem.SByte;
                        case "System.UInt64":
                            return _module.TypeSystem.UInt64;
                        case "System.UInt32":
                            return _module.TypeSystem.UInt32;
                        case "System.UInt16":
                            return _module.TypeSystem.UInt16;
                        case "System.UInt8":
                            return _module.TypeSystem.Byte;
                        case "System.String":
                            return _module.TypeSystem.String;
                        case "System.Char":
                            return _module.TypeSystem.Char;
                        case "System.Single":
                            return _module.TypeSystem.Single;
                        case "System.Double":
                            return _module.TypeSystem.Double;
                        default:
                            return _module.ImportReference(typeRef);
                    }
                case MethodReference methodRef:
                    return _module.ImportReference(methodRef);
                case FieldReference fieldRef:
                    return _module.ImportReference(fieldRef);
                default:
                    return null;
            }
        }
    }

    #region Get Cecil symbols from parkour symbols

    /// <summary>
    /// Gets the Cecil symbol reference corresponding to the parkour <see cref="Symbol"/>
    /// for emitting into IL.
    /// </summary>
    private TRef GetEmitSymbolReference<TRef>(Symbol symbol)
        where TRef : IMetadataTokenProvider
    { 
        var symbolRef = GetEmitSymbolReference(symbol);
        if (symbolRef is TRef tref)
            return tref;
        throw new InvalidOperationException($"Could not convert symbol of type '{symbolRef.GetType().Name}' to {typeof(TRef).Name}.");
    }

    /// <summary>
    /// Gets the Cecil symbol reference corresponding to the parkour <see cref="Symbol"/>
    /// for emitting into IL.
    /// </summary>
    private IMetadataTokenProvider GetEmitSymbolReference(Symbol symbol) =>
        TryGetEmitSymbolReference(symbol, out var cecilSymbol)
            ? cecilSymbol
            : throw new InvalidOperationException($"Could not get Cecil symbol for '{symbol.FullName}'");

    /// <summary>
    /// Gets the Cecil symbol reference corresponding to the parkour <see cref="Symbol"/>
    /// for emitting into IL.
    /// </summary>
    private bool TryGetEmitSymbolReference(
        Symbol symbol,
        [NotNullWhen(true)] out IMetadataTokenProvider? cecilSymbol)
    {
        if (_symbolToDefinition.TryGetValue(symbol, out cecilSymbol))
            return true;

        if (symbol is TypeSymbol typeSymbol
            && TryGetEmitTypeReference(typeSymbol, out var cecilType))
        {
            cecilSymbol = cecilType;
            return true;
        }
        else if (symbol is MemberSymbol memberSymbol
            && TryGetEmitMemberReference(memberSymbol, out var cecilMember))
        {
            cecilSymbol = cecilMember;
            return true;
        }
        else if (symbol is ParameterSymbol parameterSymbol
            && parameterSymbol.DeclaringSymbol is MemberSymbol declaringMemberSymbol
            && TryGetEmitMemberReference(declaringMemberSymbol, out var declaringMember)
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
    /// Gets a Cecil <see cref="TypeReference"/> corresponding with the <see cref="TypeSymbol"/>
    /// for emitting into IL.
    /// </summary>
    private TypeReference GetEmitTypeReference(TypeSymbol typeSymbol) =>
        TryGetEmitTypeReference(typeSymbol, out var cecilType)
            ? cecilType
            : throw new InvalidOperationException($"Could not get Cecil type reference for '{typeSymbol.FullName}'");

    /// <summary>
    /// Gets a Cecil <see cref="TypeReference"/> corresponding with the <see cref="TypeSymbol"/>
    /// for emitting into IL.
    /// </summary>
    private bool TryGetEmitTypeReference(TypeSymbol typeSymbol, [NotNullWhen(true)] out TypeReference? cecilType)
    {
        // paranoid
        if (typeSymbol == null)
        {
            cecilType = null;
            return false;
        }

        // check if we already have the definition in the emitted symbols
        if (_symbolToDefinition.TryGetValue(typeSymbol, out var cecilDefinition)
            && cecilDefinition is TypeDefinition cecilTypeDefinition)
        {
            cecilType = cecilTypeDefinition;
            return true;
        }

        // check if we already have the definition in the external symbols
        if (_externalSymbols.TryGetDefinition(typeSymbol, out cecilDefinition)
            && cecilDefinition is TypeDefinition ctd)
        {
            cecilType = (TypeReference)GetImportedSymbol(ctd);
            return true;
        }

        if (typeSymbol == _externalSymbols.Object
            || typeSymbol == SpecialSymbols.Null
            || typeSymbol == SpecialSymbols.Unknown)
        {
            cecilType = (TypeReference)GetImportedSymbol(_externalSymbols.CecilObject);
            return true;
        }
        else if (
            typeSymbol == _externalSymbols.Void
            || typeSymbol == SpecialSymbols.DoesNotReturn)
        {
            cecilType = (TypeReference)GetImportedSymbol(_externalSymbols.CecilVoid);
            return true;
        }

        if (typeSymbol is ArraySymbol array
            && TryGetEmitTypeReference(array.ElementType, out var elementType))
        {
            cecilType = new ArrayType(elementType);
            return true;
        }
        else if (typeSymbol is TypeParameterSymbol tp)
        {
            if (typeSymbol.DeclaringSymbol is TypeSymbol ts
                && TryGetEmitTypeReference(ts, out var declaringType))
            {
                cecilType = new GenericParameter(tp.Name, declaringType);
                return true;
            }
            else if (typeSymbol.DeclaringSymbol is MethodSymbol ms
                && TryGetEmitSymbolReference(ms, out var declaringMethod))
            {
                cecilType = new GenericParameter(tp.Name, (MethodReference)declaringMethod);
                return true;
            }
        }
        else if (typeSymbol.Definition != null
            && TryGetEmitTypeReference(typeSymbol.Definition, out var elementTypeDef))
        {
            var generic = new GenericInstanceType(elementTypeDef);

            // reference includes all type args (including type args of declaring type)
            var allTypeArgs = GetAllTypeArguments(typeSymbol);
            if (TryGetEmitTypeReferences(allTypeArgs, out var allTypeArgsRefs))
            {
                foreach (var arg in allTypeArgsRefs)
                {
                    generic.GenericArguments.Add(arg);
                }
            }

            cecilType = (TypeReference)GetImportedSymbol(generic);
            return true;
        }
        else if (typeSymbol.Definition == null)
        {
            // find by name
            if (_externalSymbols.GetFirstTypeDefinition(typeSymbol.FullName) is { } td)
            {
                cecilType = (TypeReference)GetImportedSymbol(td);
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
            else if (symbol is MethodSymbol ms && ms.TypeArguments.Count > 0)
            {
                if (typeArgs == null)
                    typeArgs = new List<TypeSymbol>();
                typeArgs.AddRange(ms.TypeArguments);
            }
        }
    }

    /// <summary>
    /// Gets a list of Cecil <see cref="TypeReference"/> corresponding to the list of <see cref="TypeSymbol"/>
    /// for emitting into IL.
    /// </summary>
    private bool TryGetEmitTypeReferences(
        IEnumerable<TypeSymbol> typeSymbols,
        [NotNullWhen(true)] out IReadOnlyList<TypeReference>? types)
    {
        var list = new List<TypeReference>();

        foreach (var typeSymbol in typeSymbols)
        {
            if (!TryGetEmitTypeReference(typeSymbol, out var rt))
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
    /// Gets a <see cref="MemberReference"/> corresponding to the <see cref="MemberSymbol"/>
    /// for emitting into IL.
    /// </summary>
    private MemberReference GetEmitMemberReference(MemberSymbol memberSymbol) =>
        TryGetEmitMemberReference(memberSymbol, out var member)
            ? member
            : throw new InvalidOperationException($"Could not get Cecil member reference for '{memberSymbol.FullName}'");

    /// <summary>
    /// Gets a <see cref="MemberReference"/> corresponding to the <see cref="MemberSymbol"/>
    /// for emitting into IL.
    /// </summary>
    private bool TryGetEmitMemberReference(
        MemberSymbol memberSymbol,
        [NotNullWhen(true)] out MemberReference? member)
    {
        if (_symbolToDefinition.TryGetValue(memberSymbol, out var cecilDefinition)
            && cecilDefinition is MemberReference cecilMemberReference)
        {
            member = cecilMemberReference;
            return true;
        }

        if (_externalSymbols.TryGetDefinition(memberSymbol, out cecilDefinition)
            && cecilDefinition is MemberReference mr)
        {
            member = (MemberReference)GetImportedSymbol(mr);
            return true;
        }

        // if this is a type, defer to TryGetTypeReference
        if (memberSymbol is TypeSymbol typeSymbol
            && TryGetEmitTypeReference(typeSymbol, out var typeRef))
        {
            member = typeRef;
            return true;
        }

        if (memberSymbol.Definition != null
            && TryGetEmitMemberReference(memberSymbol.Definition, out var memberDef))
        {
            var allTypeArgs = GetAllTypeArguments(memberSymbol);

            // we can encode methods as GenericInstanceMethod with all the type args
            // even if they don't have their own type parameters
            if (memberDef is MethodDefinition methodDef && methodDef.GenericParameters.Count > 0)
            {
                var gm = new GenericInstanceMethod(methodDef);
                if (TryGetEmitTypeReferences(allTypeArgs, out var allTypeArgsRefs))
                {
                    foreach (var arg in allTypeArgsRefs)
                    {
                        gm.GenericArguments.Add(arg);
                    }
                }

                member = (MemberReference)GetImportedSymbol(gm);
                return true;
            }
            else
            {
                // make generic instance of declaring type and construct a fake member with it
                if (TryGetEmitTypeReferences(allTypeArgs, out var allTypeArgsRefs))
                {
                    var typeArgs = allTypeArgsRefs.ToArray();
                    var dt = memberDef.DeclaringType.Resolve();
                    var gtd = dt.MakeGenericInstanceType(typeArgs);
                    member = (MemberReference?)CecilHelpers.GetMatchingMember(memberSymbol.Definition, gtd.Resolve());
                    if (member != null)
                    {
                        member.DeclaringType = gtd;
                        member = (MemberReference)GetImportedSymbol(member);
                        return true;
                    }
                }
            }
        }
        else if (memberSymbol.Definition == null
            && memberSymbol.DeclaringType != null
            && TryGetEmitTypeReference(memberSymbol.DeclaringType, out var declaringTypeRef)
            && declaringTypeRef is TypeDefinition td)
        {
            member = (MemberReference?)CecilHelpers.GetMatchingMember(memberSymbol, td);
            member = member != null ? (MemberReference)GetImportedSymbol(member) : null;
            return member != null;
        }

        member = null;
        return false;
    }

    private static readonly CecilEqualityComparer<TypeReference> TypeReferenceComparer = 
        CecilEqualityComparer<TypeReference>.Instance;

#endregion
}
