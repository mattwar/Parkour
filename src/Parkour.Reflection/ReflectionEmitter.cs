using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace Parkour.Reflection;

using Parkour;
using Semantics;
using Symbols;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;

/// <summary>
/// A <see cref="SemanticEmitter"/> that emits into a <see cref="ModuleBuilder"/>
/// </summary>
public partial class ReflectionEmitter : SemanticEmitter
{
    private readonly AssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;
    private readonly ReflectionSymbols _symbols;
    private readonly List<Diagnostic> _diagnostics;

    private Dictionary<Symbol, object> _symbolToBuilder =
        new Dictionary<Symbol, object>();

    public AssemblyBuilder Assembly => _assemblyBuilder;
    public ModuleBuilder Module => _moduleBuilder;

    public ReflectionEmitter(
        ReflectionSymbols symbols,
        AssemblyBuilder assemblyBuilder,
        ModuleBuilder moduleBuilder)
    {
        _symbols = symbols;
        _assemblyBuilder = assemblyBuilder;
        _moduleBuilder = moduleBuilder;
        _diagnostics = new List<Diagnostic>();
    }

    public ReflectionEmitter(
        ReflectionSymbols symbols,
        AssemblyBuilder assemblyBuilder,
        string? moduleName = null)
        : this(
              symbols,
              assemblyBuilder,
              assemblyBuilder.DefineDynamicModule(
                  moduleName ?? $"Module{assemblyBuilder.GetModules().Length}")
              )
    {
    }

    public ReflectionEmitter(
        ReflectionSymbols symbols,
        string assemblyName)
        : this(
              symbols,
              AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(assemblyName),
                AssemblyBuilderAccess.RunAndCollect))
    {
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

        // declare all types and emit all IL
        Declare(declarations);

        // finalize types (so they can be used).
        CreateTypes();

        return new EmitResult(_diagnostics.ToImmutableList());
    }

    private void CreateTypes()
    {
        // TODO: do these need to be in topographical order?
        var typeBuilders = _symbolToBuilder
            .Where(kvp => kvp.Key is TypeSymbol)
            .Select(kvp => kvp.Value)
            .OfType<TypeBuilder>()
            .ToList();

        foreach (var typeBuilder in typeBuilders)
        {
            typeBuilder.CreateType();
        }

        _moduleBuilder.CreateGlobalFunctions();
    }

    protected override void DeclareType(TypeDeclaration declaration)
    {
        var typeSymbol = declaration.Symbol as TypeSymbol;
        if (typeSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare type for unbound type declaration '{declaration.Name}'").WithLocation(declaration.Location));
            return;
        }

        var name = typeSymbol.FullName;
        TypeBuilder typeBuilder;

        if (typeSymbol.DeclaringSymbol is TypeSymbol pts)
        {
            if (_symbolToBuilder.TryGetValue(typeSymbol.DeclaringSymbol, out var pb)
                && pb is TypeBuilder parentBuilder)
            {
                var attrs = GetTypeAttributes(typeSymbol);
                typeBuilder = parentBuilder.DefineNestedType(name, attrs);
            }
            else
            {
                _diagnostics.Add(new Diagnostic($"Cannot declared nested type '{typeSymbol.FullName}' for undeclared type.").WithLocation(declaration.Location));
                return;
            }
        }
        else if (_symbolToBuilder.ContainsKey(typeSymbol))
        {
            _diagnostics.Add(new Diagnostic($"Type '{typeSymbol.FullName}' is already declared.").WithLocation(declaration.Location));
            return;
        }
        else
        {
            typeBuilder = _moduleBuilder.DefineType(name, GetTypeAttributes(typeSymbol));
        }

        _symbolToBuilder.Add(typeSymbol, typeBuilder);

        // declare type parameters too
        if (typeSymbol.TypeParameters.Count > 0)
        {
            var typeParamBuilders = typeBuilder.DefineGenericParameters(typeSymbol.TypeParameters.Select(tp => tp.Name).ToArray());
            for (int i = 0; i < typeParamBuilders.Length; i++)
            {
                _symbolToBuilder.Add(typeSymbol.TypeParameters[i], typeParamBuilders[i]);
            }
        }
    }

    protected override void DeclareBaseTypesAndInterfaces(TypeDeclaration declaration)
    {
        var typeSymbol = declaration.Symbol as TypeSymbol;
        if (typeSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare type references for unbound type declaration '{declaration.Name}'").WithLocation(declaration.Location));
            return;
        }

        if (_symbolToBuilder.TryGetValue(typeSymbol, out var builder)
            && builder is TypeBuilder typeBuilder)
        {
            // set base types and interfaces
            var hasBaseType = false;
            foreach (var bt in typeSymbol.BaseTypes)
            {
                var rt = GetReflectionType(bt);
                if (bt.IsInterface)
                {
                    typeBuilder.AddInterfaceImplementation(rt);
                }
                else if (bt.IsClass && !typeSymbol.IsValueType && !hasBaseType)
                {
                    typeBuilder.SetParent(rt);
                    hasBaseType = true;
                }
            }

            if (typeSymbol.IsValueType && !hasBaseType)
            {
                typeBuilder.SetParent(typeof(ValueType));
            }
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare type references for undeclared type '{typeSymbol.FullName}'").WithLocation(declaration.Location));
        }
    }

    protected override void DeclareMember(MemberDeclaration declaration)
    {
        var memberSymbol = declaration.Symbol as MemberSymbol;
        if (memberSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare unbound member '{declaration.Name}'.").WithLocation(declaration.Location));
            return;
        }

        // global method special case
        if (memberSymbol.DeclaringSymbol is GlobalNamespaceSymbol
            && memberSymbol is MethodSymbol globalMethodSymbol)
        {
            DeclareGlobalMethod(globalMethodSymbol);
            return;
        }

        if (memberSymbol.DeclaringType == null)  
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare member '{memberSymbol.FullName}' outside type.").WithLocation(declaration.Location));
            return;
        }

        if (!TryGetBuilder<TypeBuilder>(memberSymbol.DeclaringType, out var typeBuilder))
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare member '{memberSymbol.FullName}' for undeclared type.").WithLocation(declaration.Location));
            return;
        }

        switch (memberSymbol)
        {
            case FieldSymbol fieldSymbol when declaration is FieldDeclaration fieldDecl:
                DeclareField(fieldSymbol, fieldDecl, typeBuilder);
                break;
            case MethodSymbol methodSymbol:
                DeclareMethod(methodSymbol, typeBuilder);
                break;
            case ConstructorSymbol constructorSymbol:
                DeclareConstructor(constructorSymbol, typeBuilder);
                break;
            case PropertySymbol propertySymbol when declaration is PropertyDeclaration property:
                DeclareProperty(propertySymbol, property, typeBuilder);
                break;
            case IndexerSymbol indexerSymbol:
                DeclareIndexer(indexerSymbol, typeBuilder);
                break;
            default:
                _diagnostics.Add(new Diagnostic($"Cannot declare unsupported member '{declaration.GetType().Name}'."));
                break;
        }
        
        void DeclareField(FieldSymbol fieldSymbol, FieldDeclaration fieldDecl, TypeBuilder typeBuilder)
        {
            var fieldType = GetReflectionType(fieldSymbol.Type);
            var fieldAttrs = GetFieldAttributes(fieldSymbol);
            var fieldBuilder = typeBuilder.DefineField(fieldSymbol.Name, fieldType, fieldAttrs);
            _symbolToBuilder.Add(fieldSymbol, fieldBuilder);

            // set constant value
            if (fieldSymbol.Modifiers.Contains(Modifier.Constant)
                && fieldDecl.Initializer is ConstantExpression fieldConst)
            {
                fieldBuilder.SetConstant(fieldConst.Value);
            }
        }

        MethodBuilder DeclareMethod(MethodSymbol methodSymbol, TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod(
                methodSymbol.Name,
                GetMethodAttributes(methodSymbol));
            _symbolToBuilder.Add(methodSymbol, methodBuilder);
            return DeclareMethodCore(methodSymbol, methodBuilder, typeBuilder);
        }

        void DeclareGlobalMethod(MethodSymbol methodSymbol)
        {
            var methodBuilder = _moduleBuilder.DefineGlobalMethod(
                globalMethodSymbol.Name,
                GetMethodAttributes(globalMethodSymbol),
                null,
                null
                );
            _symbolToBuilder.Add(globalMethodSymbol, methodBuilder);
            DeclareMethodCore(globalMethodSymbol, methodBuilder, null);
        }

        MethodBuilder DeclareMethodCore(MethodSymbol methodSymbol, MethodBuilder methodBuilder, TypeBuilder? typeBuilder)
        {
            // define type parameters (before items that may reference them)
            if (methodSymbol.TypeParameters.Count > 0)
            {
                var typeParameterBuilders = methodBuilder.DefineGenericParameters(
                    methodSymbol.TypeParameters.Select(tp => tp.Name).ToArray());

                for (int i = 0; i < typeParameterBuilders.Length; i++)
                {
                    var typeParameter = methodSymbol.TypeParameters[i];
                    var tpBuilder = typeParameterBuilders[i];
                    _symbolToBuilder.Add(typeParameter, tpBuilder);
                }
            }

            // set return type
            methodBuilder.SetReturnType(GetReflectionType(methodSymbol.ReturnType));

            // set parameter types
            var parameterTypes = methodSymbol.Parameters
                .Select(p => GetReflectionType(p.Type))
                .ToArray();
            methodBuilder.SetParameters(parameterTypes);

            // set parameter info
            for (int i = 0; i < methodSymbol.Parameters.Count; i++)
            {
                var parameterSymbol = methodSymbol.Parameters[i];
                var parameterBuilder = methodBuilder.DefineParameter(i, GetParameterAttributes(parameterSymbol), parameterSymbol.Name);
                _symbolToBuilder.Add(parameterSymbol, parameterBuilder);

                // declare custom attributes
                foreach (var attr in parameterSymbol.Attributes)
                {
                    var customAttr = CreateCustomAttribute(attr);
                    parameterBuilder.SetCustomAttribute(customAttr);
                }
            }

            if (typeBuilder != null)
            {
                foreach (var impl in methodSymbol.Implements)
                {
                    var interfaceMethod = GetReflectionInfo<MethodInfo>(impl);
                    if (interfaceMethod != null)
                    {
                        typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
                    }
                }
            }

            return methodBuilder;
        }

        void DeclareConstructor(ConstructorSymbol constructorSymbol, TypeBuilder typeBuilder)
        {
            if (constructorSymbol.IsStatic && constructorSymbol.Parameters.Count > 0)
            {
                _diagnostics.Add(new Diagnostic($"static constructors cannot have arguments").WithLocation(declaration.Location));
                return;
            }

            ConstructorBuilder constructorBuilder;
            if (constructorSymbol.IsStatic)
            {
                constructorBuilder = typeBuilder.DefineTypeInitializer();
            }
            else
            {
                constructorBuilder = typeBuilder.DefineConstructor(
                    GetMethodAttributes(constructorSymbol),
                    CallingConventions.Standard,
                    constructorSymbol.Parameters.Select(p => GetReflectionType(p.Type)).ToArray()
                    );
            }
            _symbolToBuilder.Add(constructorSymbol, constructorBuilder);

            for (int i = 0; i < constructorSymbol.Parameters.Count; i++)
            {
                var parameterSymbol = constructorSymbol.Parameters[i];
                var parameterBuilder = constructorBuilder.DefineParameter(i, GetParameterAttributes(parameterSymbol), parameterSymbol.Name);
                _symbolToBuilder.Add(parameterSymbol, parameterBuilder);

                // declare custom attributes
                foreach (var attr in parameterSymbol.Attributes)
                {
                    var customAttr = this.CreateCustomAttribute(attr);
                    parameterBuilder.SetCustomAttribute(customAttr);
                }
            }
        }

        void DeclareProperty(PropertySymbol propertySymbol, PropertyDeclaration propertyDecl, TypeBuilder typeBuilder)
        {
            // TODO: this backing field should already have been lowered to its own declaration
            if (propertyDecl.BackingField != null)
            {
                DeclareField((FieldSymbol)propertyDecl.BackingField.Symbol!, propertyDecl.BackingField, typeBuilder);
            }

            DeclarePropertyOrIndexer(
                typeBuilder,
                propertySymbol,
                propertySymbol.Type,
                propertySymbol.GetMethod,
                propertySymbol.SetMethod
                );
        }

        void DeclareIndexer(IndexerSymbol indexerSymbol, TypeBuilder typeBuilder)
        {
            DeclarePropertyOrIndexer(
                typeBuilder,
                indexerSymbol,
                indexerSymbol.ElementType,
                indexerSymbol.GetMethod,
                indexerSymbol.SetMethod
                );
        }


        void DeclarePropertyOrIndexer(
            TypeBuilder typeBuilder,
            MemberSymbol propertySymbol,
            TypeSymbol propertyType,
            MethodSymbol? getMethodSymbol,
            MethodSymbol? setMethodSymbol)
        {
            var propertyBuilder = typeBuilder.DefineProperty(
                propertySymbol.Name,
                GetPropertyAttributes(propertySymbol),
                GetReflectionType(propertyType),
                []);

            _symbolToBuilder.Add(propertySymbol, propertyBuilder);

            if (getMethodSymbol != null)
            {
                propertyBuilder.SetGetMethod(DeclareMethod(getMethodSymbol, typeBuilder));
            }

            if (setMethodSymbol != null)
            {
                propertyBuilder.SetSetMethod(DeclareMethod(setMethodSymbol, typeBuilder));
            }
        }
    }

    protected override void DeclareAccessors(MemberDeclaration declaration)
    {
        // already handled in DeclareMember
    }

    protected override void DeclareAttributes(MemberDeclaration declaration)
    {
        if (declaration.Symbol != null)
            Declare(declaration.Symbol);

        void Declare(Symbol symbol)
        {
            switch (symbol)
            {
                case FieldSymbol fieldSymbol:
                    if (TryGetBuilder<FieldBuilder>(fieldSymbol, out var fieldBuilder))
                    {
                        foreach (var attr in fieldSymbol.Attributes)
                        {
                            var customAttr = CreateCustomAttribute(attr);
                            fieldBuilder.SetCustomAttribute(customAttr);
                        }
                    }
                    break;
                case MethodSymbol methodSymbol:
                    if (TryGetBuilder<MethodBuilder>(methodSymbol, out var methodBuilder))
                    {
                        foreach (var attr in methodSymbol.Attributes)
                        {
                            var customAttr = CreateCustomAttribute(attr);
                            methodBuilder.SetCustomAttribute(customAttr);
                        }

                        DeclareAll(methodSymbol.Parameters);
                        DeclareAll(methodSymbol.TypeParameters);
                    }
                    break;
                case ConstructorSymbol constructorSymbol:
                    if (TryGetBuilder<ConstructorBuilder>(constructorSymbol, out var constructorBuilder))
                    {
                        foreach (var attr in constructorSymbol.Attributes)
                        {
                            var customAttr = CreateCustomAttribute(attr);
                            constructorBuilder.SetCustomAttribute(customAttr);
                        }

                        DeclareAll(constructorSymbol.Parameters);
                    }
                    break;
                case ParameterSymbol parameterSymbol:
                    if (TryGetBuilder<ParameterBuilder>(parameterSymbol, out var parameterBuilder))
                    {
                        foreach (var attr in parameterSymbol.Attributes)
                        {
                            var customAttr = CreateCustomAttribute(attr);
                            parameterBuilder.SetCustomAttribute(customAttr);
                        }
                    }
                    break;
                case TypeParameterSymbol typeParameterSymbol:
                    if (TryGetBuilder<GenericTypeParameterBuilder>(typeParameterSymbol, out var typeParameterBuilder))
                    {
                        foreach (var attr in typeParameterSymbol.Attributes)
                        {
                            var customAttr = CreateCustomAttribute(attr);
                            typeParameterBuilder.SetCustomAttribute(customAttr);
                        }
                    }
                    break;
                case PropertySymbol propertySymbol:
                    if (TryGetBuilder<PropertyBuilder>(symbol, out var propertyBuilder))
                    {
                        foreach (var attr in propertySymbol.Attributes)
                        {
                            var customAttr = CreateCustomAttribute(attr);
                            propertyBuilder.SetCustomAttribute(customAttr);
                        }
                    }
                    break;
                case IndexerSymbol indexerSymbol:
                    if (TryGetBuilder<PropertyBuilder>(symbol, out var indexerBuilder))
                    {
                        foreach (var attr in indexerSymbol.Attributes)
                        {
                            var customAttr = CreateCustomAttribute(attr);
                            indexerBuilder.SetCustomAttribute(customAttr);
                        }
                    }
                    break;
                case TypeSymbol typeSymbol:
                    if (TryGetBuilder<TypeBuilder>(symbol, out var typeBuilder))
                    {
                        // declare attributes if any
                        if (typeSymbol.Attributes.Count > 0)
                        {
                            foreach (var attr in typeSymbol.Attributes)
                            {
                                var customAttr = CreateCustomAttribute(attr);
                                typeBuilder.SetCustomAttribute(customAttr);
                            }
                        }
                    }
                    break;
            }
        }

        void DeclareAll<TSymbol>(IEnumerable<TSymbol> symbols) where TSymbol : Symbol
        {
            foreach (var symbol in symbols)
            {
                Declare(symbol);
            }
        }
    }

    private bool TryGetBuilder<TBuilder>(Symbol symbol, [NotNullWhen(true)] out TBuilder? builder)
    {
        if (symbol != null 
            && _symbolToBuilder.TryGetValue(symbol, out var b) && b is TBuilder tb)
        {
            builder = tb;
            return true;
        }
        {
            builder = default;
            return false;
        }
    }

    protected override void EmitMemberBody(MemberDeclaration declaration)
    {
        var memberSymbol = declaration.Symbol as MemberSymbol;
        if (memberSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare body for unbound member declaration.").WithLocation(declaration.Location));
            return;
        }

        if (!_symbolToBuilder.TryGetValue(memberSymbol, out var builder))
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare body of unbound member declaration.").WithLocation(declaration.Location));
            return;
        }

        switch (declaration)
        {
            case MethodDeclaration methodDecl:
                {
                    var methodBuilder = (MethodBuilder)builder;
                    var methodSymbol = (MethodSymbol)memberSymbol;
                    var ilEmitter = new ILEmitter(this, methodBuilder.GetILGenerator());
                    var bodyBuilder = new StandardBodyBuilder(methodSymbol, ilEmitter);
                    bodyBuilder.BuildBody(methodDecl.Body, methodSymbol.ReturnType, methodDecl.ReturnLabel);
                }
                break;

            case ConstructorDeclaration constructorDecl:
                {
                    var constructorBuilder = (ConstructorBuilder)builder;
                    var constructorSymbol = (ConstructorSymbol)memberSymbol;
                    var ilEmitter = new ILEmitter(this, constructorBuilder.GetILGenerator());
                    var bodyBuilder = new StandardBodyBuilder(constructorSymbol, ilEmitter);
                    bodyBuilder.BuildBody(constructorDecl.Body, _symbols.Void, constructorDecl.ReturnLabel);
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

    private static TypeAttributes GetTypeAttributes(TypeSymbol ts)
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
        switch (ts.Access)
        {
            case RuntimeAccess.Private:
                attrs |= isNested ? TypeAttributes.NestedPrivate : TypeAttributes.NotPublic;
                break;
            case RuntimeAccess.Public:
                attrs |= isNested ? TypeAttributes.NestedPublic : TypeAttributes.NotPublic;
                break;
            case RuntimeAccess.Internal:
                attrs |= isNested ? TypeAttributes.NestedAssembly : TypeAttributes.NotPublic;
                break;
            case RuntimeAccess.Protected:
                attrs |= isNested ? TypeAttributes.NestedFamily : TypeAttributes.NotPublic;
                break;
            case RuntimeAccess.ProtectedOrInternal:
                attrs |= isNested ? TypeAttributes.NestedFamORAssem : TypeAttributes.NotPublic;
                break;
            case RuntimeAccess.ProtectedAndInternal:
                attrs |= isNested ? TypeAttributes.NestedFamANDAssem : TypeAttributes.NotPublic;
                break;
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

    private CustomAttributeBuilder CreateCustomAttribute(AttributeInfo info)
    {
        var constructor = GetReflectionInfo<ConstructorInfo>(info.Constructor);
        var argValues = info.Arguments.Select(a => GetValue(a.Value)).ToArray();
        var props = info.Members.Where(m => m.Member is PropertySymbol);
        var propInfos = props.Select(p => GetReflectionInfo<PropertyInfo>(p.Member)).ToArray();
        var propValues = props.Select(p => GetValue(p.Value)).ToArray();
        var fields = info.Members.Where(m => m.Member is FieldSymbol);
        var fieldInfos = fields.Select(f => GetReflectionInfo<FieldInfo>(f.Member)).ToArray();
        var fieldValues = fields.Select(f => GetValue(f.Value)).ToArray();

        return new CustomAttributeBuilder(
            constructor,
            argValues,
            propInfos,
            propValues,
            fieldInfos,
            fieldValues
            );

        object? GetValue(AttributeValue value)
        {
            switch (value)
            {
                case AttributeConstantValue cv:
                    return cv.Value;
                case AttributeTypeValue tv:
                    var atype = GetReflectionType(tv.Type);
                    return atype;
                case AttributeArrayValue av:
                    var elemType = GetReflectionType(av.ElementType);
                    var values = av.Values.Select(v => GetValue(v)).ToArray();
                    if (elemType == typeof(object))
                        return values;
                    var typedArray = Array.CreateInstance(elemType, values.Length);
                    for (int i = 0; i < values.Length; i++)
                    {
                        var v = values[i];
                        v = Convert.ChangeType(v, elemType);
                        typedArray.SetValue(v, i);
                    }
                    return typedArray;
               default:
                    return null;
            }
        }
    }

    private Type GetReflectionType(TypeSymbol typeSymbol) =>
        GetReflectionInfo<TypeInfo>(typeSymbol);

    private TInfo GetReflectionInfo<TInfo>(Symbol symbol)
        where TInfo : class
    {
        var info = GetReflectionInfo(symbol);
        if (info != null && info is TInfo tinfo)
        {
            return tinfo;
        }
        else
        {
            throw new InvalidOperationException($"Could not convert symbol '{symbol.FullName}' to runtime type");
        }
    }

    private object? GetReflectionInfo(Symbol symbol) =>
        _symbols.TryGetInfo(symbol, out var info, GetReflectionInfoFromDeclarations) ? info : null;

    private object? GetReflectionInfoFromDeclarations(Symbol symbol) =>
        _symbolToBuilder.TryGetValue(symbol, out var builder) ? builder : null;
}