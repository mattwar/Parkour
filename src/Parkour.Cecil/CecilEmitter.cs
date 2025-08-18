using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Parkour.Cecil;

using Semantics;
using Symbols;
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
                var btr = GetCecilType(bt);
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
                    typeDef.BaseType = GetCecilType(_externalSymbols.GetTypeSymbol(typeof(ValueType)));
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
                    var fieldType = GetCecilType(field.Type);
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
                    var methodDef = new MethodDefinition(method.Name, methodAttributes, GetCecilType(method.ReturnType));
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
                    var propertyType = GetCecilType(property.Type);
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
                    var indexerType = GetCecilType(indexer.ElementType);
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
            var type = GetCecilType(parameter.Type);
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

                if (ts.Modifiers.Contains(SymbolModifier.Abstract))
                    attrs |= TypeAttributes.Abstract;
                else if (ts.Modifiers.Contains(SymbolModifier.Sealed))
                    attrs |= TypeAttributes.Sealed;
                else if (ts.Modifiers.Contains(SymbolModifier.Special))
                    attrs |= TypeAttributes.SpecialName;
                break;
            case InterfaceSymbol:
                attrs |= TypeAttributes.Interface | TypeAttributes.Abstract;
                break;
        }


        var isNested = ts.DeclaringSymbol is TypeSymbol;
        if (ts.Access == SymbolAccess.Private)
        {
            attrs |= isNested ? TypeAttributes.NestedPrivate : TypeAttributes.NotPublic;
        }
        else if (ts.Access == SymbolAccess.Public)
        {
            attrs |= isNested ? TypeAttributes.NestedPublic : TypeAttributes.NotPublic;
        }
        else if (ts.Access == SymbolAccess.Internal)
        {
            attrs |= isNested ? TypeAttributes.NestedAssembly : TypeAttributes.NotPublic;
        }
        else if (ts.Access == SymbolAccess.Protected)
        {
            attrs |= isNested ? TypeAttributes.NestedFamily : TypeAttributes.NotPublic;
        }
        else if (ts.Access == SymbolAccess.ProtectedOrInternal)
        {
            attrs |= isNested ? TypeAttributes.NestedFamORAssem : TypeAttributes.NotPublic;
        }
        else if (ts.Access == SymbolAccess.ProtectedAndInternal)
        {
            attrs |= isNested ? TypeAttributes.NestedFamANDAssem : TypeAttributes.NotPublic;
        }

        return attrs;
    }

    private static FieldAttributes GetFieldAttributes(FieldSymbol field)
    {
        FieldAttributes attrs = default;

        if (field.Modifiers.Contains(SymbolModifier.Static))
            attrs |= FieldAttributes.Static;

        if (field.Modifiers.Contains(SymbolModifier.Special))
            attrs |= FieldAttributes.SpecialName;

        if (field.Modifiers.Contains(SymbolModifier.Constant))
            attrs |= FieldAttributes.Literal;

        if (field.Access == SymbolAccess.Private)
        {
            attrs |= FieldAttributes.Private;
        }
        else if (field.Access == SymbolAccess.Public)
        {
            attrs |= FieldAttributes.Public;
        }
        else if (field.Access == SymbolAccess.Protected)
        {
            attrs |= FieldAttributes.Family;
        }
        else if (field.Access == SymbolAccess.Internal)
        {
            attrs |= FieldAttributes.Assembly;
        }
        else if (field.Access == SymbolAccess.ProtectedOrInternal)
        {
            attrs |= FieldAttributes.FamORAssem;
        }
        else if (field.Access == SymbolAccess.ProtectedAndInternal)
        {
            attrs |= FieldAttributes.FamANDAssem;
        }

        return attrs;
    }

    private static MethodAttributes GetMethodAttributes(MemberSymbol method)
    {
        MethodAttributes attrs = default;

        if (method.Modifiers.Contains(SymbolModifier.Static))
            attrs |= MethodAttributes.Static;

        if (method.DeclaringSymbol == null
            || method.DeclaringSymbol is NamespaceSymbol)
            attrs |= MethodAttributes.Static;

        if (method.Modifiers.Contains(SymbolModifier.Special))
            attrs |= MethodAttributes.SpecialName;

        if (method.Access == SymbolAccess.Private)
        {
            attrs |= MethodAttributes.Private;
        }
        else if (method.Access == SymbolAccess.Public)
        {
            attrs |= MethodAttributes.Public;
        }
        else if (method.Access == SymbolAccess.Protected)
        {
            attrs |= MethodAttributes.Family;
        }
        else if (method.Access == SymbolAccess.Internal)
        {
            attrs |= MethodAttributes.Assembly;
        }
        else if (method.Access == SymbolAccess.ProtectedOrInternal)
        {
            attrs |= MethodAttributes.FamORAssem;
        }
        else if (method.Access == SymbolAccess.ProtectedAndInternal)
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
        var constructor = GetCecilReference<MethodReference>(info.Constructor);
        var argValues = info.Arguments.Select(a => GetValue(a.Value)).ToArray();
        var props = info.Members.Where(m => m.Member is PropertySymbol);
        var propInfos = props.Select(p => GetCecilReference<PropertyReference>(p.Member)).ToArray();
        var propValues = props.Select(p => GetValue(p.Value)).ToArray();
        var fields = info.Members.Where(m => m.Member is FieldSymbol);
        var fieldInfos = fields.Select(f => GetCecilReference<FieldReference>(f.Member)).ToArray();
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
                    var valueTypeRef = GetCecilType(valueTypeSymbol);
                    return new CustomAttributeArgument(valueTypeRef, cv.Value);
                case AttributeTypeValue tv:
                    return new CustomAttributeArgument(_externalSymbols.CecilType, GetCecilType(tv.Type));
                case AttributeArrayValue av:
                    var elemType = GetCecilType(av.ElementType);
                    var arrayType = new ArrayType(elemType);
                    var values = av.Values.Select(v => GetValue(v)).ToArray();
                    return new CustomAttributeArgument(elemType, values);
                default:
                    throw new InvalidOperationException($"Unhandled attribute value '{value.GetType().Name}'");
            }
        }
    }

    internal TypeReference GetCecilType(TypeSymbol typeSymbol) =>
        GetCecilReference<TypeReference>(typeSymbol);

    internal TRef GetCecilReference<TRef>(Symbol symbol)
        where TRef : MemberReference
    {
        var cr = GetCecilReference(symbol);
        if (cr != null && cr is TRef tinfo)
        {
            return tinfo;
        }
        else
        {
            throw new InvalidOperationException($"Could not convert symbol '{symbol.FullName}' to cecil metadata object.");
        }
    }

    internal IMetadataTokenProvider? GetCecilReference(Symbol symbol) =>
        _externalSymbols.TryGetCecilSymbol(symbol, out var obj, GetEmittedSymbol, GetImportedSymbol) ? obj : null;

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
    private IMetadataTokenProvider GetImportedSymbol(IMetadataTokenProvider symbol) =>
        GetImportedSymbol<IMetadataTokenProvider>(symbol);

    /// <summary>
    /// Gets the imported version of the symbol.
    /// </summary>
    private TSymbol GetImportedSymbol<TSymbol>(TSymbol symbol)
        where TSymbol : IMetadataTokenProvider
    {
        if (!_importedSymbols.TryGetValue(symbol, out var imported))
        {
            imported = Import();
            if (imported != null)
                _importedSymbols[symbol] = imported;
        }

        return (TSymbol)(imported ?? symbol);

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
                        default:
                            return _module.ImportReference(typeRef);
                    }
                case MethodReference methodRef:
                    return _module.ImportReference(methodRef);
                case FieldReference fieldRef:
                    return _module.ImportReference(fieldRef);
                //case PropertyReference propertyRef:
                //    return _module.ImportReference(propertyRef);
                default:
                    return null;
            }
        }
    }
}
