using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace Parkour.Reflection;

using Semantics;
using Symbols;

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

        DeclareTypes(declarations);
        DeclareTypeReferences(declarations);
        DeclareTypeMembers(declarations);
        BuildMemberBodies(declarations);
        CreateTypes();

        return new EmitResult(_diagnostics.ToImmutableList());
    }

    private void DeclareTypes(ImmutableList<Declaration> declarations)
    {
        foreach (var decl in declarations)
        {
            Declare(decl);
        }

        void Declare(Declaration decl)
        {
            if (decl is NamespaceDeclaration nd)
            {
                DeclareTypes(nd.Declarations);
            }
            else if (decl is TypeDeclaration td)
            {
                this.DeclareType(td);
                DeclareTypes(td.Declarations);
            }
        }
    }

    private void DeclareTypeReferences(ImmutableList<Declaration> declarations)
    {
        foreach (var d in declarations)
        {
            Declare(d);
        }

        void Declare(Declaration decl)
        {
            if (decl is NamespaceDeclaration nd)
            {
                DeclareTypeReferences(nd.Declarations);
            }
            else if (decl is TypeDeclaration td)
            {
                this.DeclareTypeReferences(td);
                DeclareTypeReferences(td.Declarations);
            }
        }
    }

    private void DeclareTypeMembers(ImmutableList<Declaration> declarations)
    {
        foreach (var decl in declarations)
        {
            Declare(decl);
        }

        void Declare(Declaration decl)
        {
            if (decl is NamespaceDeclaration nd)
            {
                DeclareTypeMembers(nd.Declarations);
            }
            else if (decl is TypeDeclaration td)
            {
                DeclareTypeMembers(td.Declarations);
            }
            else if (decl is PropertyDeclaration pd)
            {
                this.DeclareTypeMember(pd);
            }
            else if (decl is IndexerDeclaration xd)
            {
                this.DeclareTypeMember(xd);
            }
            else if (decl is MemberDeclaration md)
            {
                this.DeclareTypeMember(md);
            }
        }
    }

    private void BuildMemberBodies(ImmutableList<Declaration> declarations)
    {
        foreach (var decl in declarations)
        {
            Build(decl);
        }

        void Build(Declaration decl)
        {
            if (decl is NamespaceDeclaration nd)
            {
                BuildMemberBodies(nd.Declarations);
            }
            else if (decl is TypeDeclaration td)
            {
                BuildMemberBodies(td.Declarations);
            }
            else if (decl is MemberDeclaration md)
            {
                this.EmitMemberBody(md);
            }
        }
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

    private void DeclareType(TypeDeclaration declaration)
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

    private void DeclareTypeReferences(TypeDeclaration declaration)
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
                var rt = GetRuntimeType(bt);
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

            // declare attributes if any
            if (typeSymbol.Attributes.Count > 0)
            {
                foreach (var attr in typeSymbol.Attributes)
                {
                    var customAttr = GetCustomAttribute(attr);
                    typeBuilder.SetCustomAttribute(customAttr);
                }
            }

            // declare type parameter references
            foreach (var typeParameter in typeSymbol.TypeParameters)
            {
                this.DeclareTypeParameterReferences(typeParameter);
            }
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare type references for undeclared type '{typeSymbol.FullName}'").WithLocation(declaration.Location));
        }
    }

    /// <summary>
    /// Declares type parameter constraints and custom attributes
    /// </summary>
    /// <param name="typeParameter"></param>
    protected virtual void DeclareTypeParameterReferences(TypeParameterSymbol typeParameter)
    {
        if (_symbolToBuilder.TryGetValue(typeParameter, out var builder)
            && builder is GenericTypeParameterBuilder tpBuilder)
        {
            foreach (var attr in typeParameter.Attributes)
            {
                var customAttr = GetCustomAttribute(attr);
                tpBuilder.SetCustomAttribute(customAttr);
            }
        }
    }

    private void DeclareTypeMember(MemberDeclaration declaration)
    {
        switch (declaration)
        {
            case FieldDeclaration field:
                this.DeclareField(field);
                break;
            case MethodDeclaration method:
                this.DeclareMethod(method);
                break;
            case ConstructorDeclaration constructor:
                this.DeclareConstructor(constructor);
                break;
            case PropertyDeclaration property:
                this.DeclareProperty(property);
                break;
            case IndexerDeclaration indexer:
                this.DeclareIndexer(indexer);
                break;
            default:
                _diagnostics.Add(new Diagnostic($"Cannot declare unsupported member '{declaration.GetType().Name}'."));
                break;
        }
    }

    private void DeclareField(FieldDeclaration declaration)
    {
        var fieldSymbol = declaration.Symbol;
        if (fieldSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare field for unbound field declaration '{declaration.Name}'."));
            return;
        }

        if (fieldSymbol.DeclaringType == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare field '{fieldSymbol.FullName}' outside type.").WithLocation(declaration.Location));
            return;
        }

        if (fieldSymbol.DeclaringType.IsInterface)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare field '{fieldSymbol.FullName}' for interface type.").WithLocation(declaration.Location));
            return;
        }

        if (!TryGetDeclaringTypeBuilder(fieldSymbol, out var typeBuilder))
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare field '{fieldSymbol.FullName}' for undeclared type.").WithLocation(declaration.Location));
            return;
        }

        var fieldType = GetRuntimeType(fieldSymbol.Type);
        var fieldAttrs = GetFieldAttributes(fieldSymbol);
        var fieldBuilder = typeBuilder.DefineField(
            fieldSymbol.Name,
            fieldType,
            fieldAttrs);

        // declare attributes
        foreach (var attr in fieldSymbol.Attributes)
        {
            var customAttr = GetCustomAttribute(attr);
            fieldBuilder.SetCustomAttribute(customAttr);
        }

        _symbolToBuilder.Add(fieldSymbol, fieldBuilder);
    }

    private void DeclareMethod(MethodDeclaration declaration)
    {
        var methodSymbol = declaration.Symbol;
        if (methodSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare method for unbound method declaration '{declaration.Name}'.").WithLocation(declaration.Location));
            return;
        }

        MethodBuilder? methodBuilder = null;
        TypeBuilder? typeBuilder = null;

        if (methodSymbol.DeclaringType != null)
        {
            if (!TryGetDeclaringTypeBuilder(methodSymbol, out typeBuilder))
            {
                _diagnostics.Add(new Diagnostic($"Cannot declare method '{methodSymbol.FullName}' for undeclared type.").WithLocation(declaration.Location));
                return;
            }

            methodBuilder = typeBuilder.DefineMethod(
                methodSymbol.Name,
                GetMethodAttributes(methodSymbol));
        }
        else if (methodSymbol.DeclaringSymbol is GlobalNamespaceSymbol)
        {
            methodBuilder = _moduleBuilder.DefineGlobalMethod(
                methodSymbol.Name,
                GetMethodAttributes(methodSymbol),
                null,
                null
                );
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare global method '{methodSymbol.FullName}' in non-global namespace.").WithLocation(declaration.Location));
            return;
        }

        _symbolToBuilder.Add(methodSymbol, methodBuilder);

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
                this.DeclareTypeParameterReferences(typeParameter);
            }
        }

        // set return type
        methodBuilder.SetReturnType(GetRuntimeType(methodSymbol.ReturnType));

        // set parameter types
        var parameterTypes = methodSymbol.Parameters
            .Select(p => GetRuntimeType(p.Type))
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
                var customAttr = GetCustomAttribute(attr);
                parameterBuilder.SetCustomAttribute(customAttr);
            }
        }

        if (typeBuilder != null)
        {
            foreach (var impl in methodSymbol.Implements)
            {
                var interfaceMethod = GetRuntimeInfo<MethodInfo>(impl);
                if (interfaceMethod != null)
                {
                    typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
                }
            }
        }

        // declare custom attributes
        foreach (var attr in methodSymbol.Attributes)
        {
            var customAttr = GetCustomAttribute(attr);
            methodBuilder.SetCustomAttribute(customAttr);
        }
    }

    private void DeclareConstructor(ConstructorDeclaration declaration)
    {
        var constructorSymbol = declaration.Symbol;
        if (constructorSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare constructor for unbound constructor declaration.").WithLocation(declaration.Location));
            return;
        }

        if (constructorSymbol.DeclaringType == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare constructor outside type declaration.").WithLocation(declaration.Location));
            return;
        }

        if (!TryGetDeclaringTypeBuilder(constructorSymbol, out var typeBuilder))
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare constructor '{constructorSymbol.FullName}' for undeclared type.").WithLocation(declaration.Location));
            return;
        }

        if (typeBuilder.IsInterface)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare constructor '{constructorSymbol.FullName}' for interface type.").WithLocation(declaration.Location));
            return;
        }

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
                constructorSymbol.Parameters.Select(p => GetRuntimeType(p.Type)).ToArray()
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
                var customAttr = this.GetCustomAttribute(attr);
                parameterBuilder.SetCustomAttribute(customAttr);
            }
        }

        // declare custom attributes
        foreach (var attr in constructorSymbol.Attributes)
        {
            var customAttr = this.GetCustomAttribute(attr);
            constructorBuilder.SetCustomAttribute(customAttr);
        }
    }

    private void DeclareProperty(PropertyDeclaration declaration)
    {
        var propertySymbol = declaration.Symbol;
        if (propertySymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare property for unbound property declaration.").WithLocation(declaration.Location));
            return;
        }

        if (propertySymbol.DeclaringType == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare property '{propertySymbol.FullName}' outside type.").WithLocation(declaration.Location));
            return;
        }

        if (!TryGetDeclaringTypeBuilder(propertySymbol, out var typeBuilder))
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare property '{propertySymbol.FullName}' for undeclared type.").WithLocation(declaration.Location));
            return;
        }

        if (declaration.BackingField != null)
        {
            this.DeclareField(declaration.BackingField);
        }

        DeclarePropertyOrIndexer(
            typeBuilder,
            declaration,
            propertySymbol,
            propertySymbol.Type,
            declaration.GetMethod,
            declaration.SetMethod
            );
    }

    private void DeclareIndexer(IndexerDeclaration declaration)
    {
        var indexerSymbol = declaration.Symbol;
        if (indexerSymbol == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare indexer for unbound indexer declaration.").WithLocation(declaration.Location));
            return;
        }

        if (indexerSymbol.DeclaringType == null)
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare indexer outside type.").WithLocation(declaration.Location));
            return;
        }

        if (!TryGetDeclaringTypeBuilder(indexerSymbol, out var typeBuilder))
        {
            _diagnostics.Add(new Diagnostic($"Cannot declare indexer for undeclared type.").WithLocation(declaration.Location));
            return;
        }

        DeclarePropertyOrIndexer(
            typeBuilder,
            declaration,
            indexerSymbol,
            indexerSymbol.ElementType,
            declaration.GetMethod,
            declaration.SetMethod
            );
    }

    private void DeclarePropertyOrIndexer(
        TypeBuilder typeBuilder,
        MemberDeclaration declaration,
        MemberSymbol propertySymbol,
        TypeSymbol propertyType,
        MethodDeclaration? getMethod, 
        MethodDeclaration? setMethod)
    {
        var propertyBuilder = typeBuilder.DefineProperty(
            propertySymbol.Name,
            GetPropertyAttributes(propertySymbol),
            GetRuntimeType(propertyType),
            []);

        _symbolToBuilder.Add(propertySymbol, propertyBuilder);

        if (getMethod != null)
        {
            this.DeclareMethod(getMethod);

            if (getMethod.Symbol != null && _symbolToBuilder.TryGetValue(getMethod.Symbol, out var getMethodBuilder))
            {
                propertyBuilder.SetGetMethod((MethodBuilder)getMethodBuilder);
            }
        }

        if (setMethod != null)
        {
            this.DeclareMethod(setMethod);

            if (setMethod.Symbol != null && _symbolToBuilder.TryGetValue(setMethod.Symbol, out var setMethodBuilder))
            {
                propertyBuilder.SetSetMethod((MethodBuilder)setMethodBuilder);
            }
        }

        foreach (var attr in propertySymbol.Attributes)
        {
            var customAttr = this.GetCustomAttribute(attr);
            propertyBuilder.SetCustomAttribute(customAttr);
        }
    }

    private bool TryGetDeclaringTypeBuilder(MemberSymbol memberSymbol, out TypeBuilder typeBuilder)
    {
        if (memberSymbol.DeclaringSymbol is TypeSymbol declaringTypeSymbol
            && _symbolToBuilder.TryGetValue(declaringTypeSymbol, out var builder)
            && builder is TypeBuilder tb)
        {
            typeBuilder = tb;
            return true;
        }
        else
        {
            typeBuilder = null!;
            return false;
        }
    }

    private void EmitMemberBody(MemberDeclaration declaration)
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
                    var ilEmitter = new ReflectionILEmitter(this, methodBuilder.GetILGenerator());
                    var bodyBuilder = new StandardBodyBuilder(methodSymbol, ilEmitter);
                    bodyBuilder.BuildBody(methodDecl.Body, methodSymbol.ReturnType, methodDecl.ReturnLabel);
                }
                break;

            case ConstructorDeclaration constructorDecl:
                {
                    var constructorBuilder = (ConstructorBuilder)builder;
                    var constructorSymbol = (ConstructorSymbol)memberSymbol;
                    var ilEmitter = new ReflectionILEmitter(this, constructorBuilder.GetILGenerator());
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


    private Type GetRuntimeType(TypeSymbol typeSymbol) =>
        GetRuntimeInfo<TypeInfo>(typeSymbol);

    private TInfo GetRuntimeInfo<TInfo>(Symbol symbol)
        where TInfo : class
    {
        var info = GetRuntimeInfo(symbol);
        if (info != null && info is TInfo tinfo)
        {
            return tinfo;
        }
        else
        {
            throw new InvalidOperationException($"Could not convert symbol '{symbol.FullName}' to runtime type");
        }
    }

    private object? GetRuntimeInfo(Symbol symbol)
    {
        _symbols.TryGetRuntimeInfo(symbol, out var info, GetFromBuilders);
        return info;

        object? GetFromBuilders(Symbol symbol)
        {
            _symbolToBuilder.TryGetValue(symbol, out var builder);
            return builder;
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

    private CustomAttributeBuilder GetCustomAttribute(AttributeInfo info)
    {
        var constructor = GetRuntimeInfo<ConstructorInfo>(info.Constructor);
        var argValues = info.Arguments.Select(a => GetValue(a.Value)).ToArray();
        var props = info.Members.Where(m => m.Member is PropertySymbol);
        var propInfos = props.Select(p => GetRuntimeInfo<PropertyInfo>(p.Member)).ToArray();
        var propValues = props.Select(p => GetValue(p.Value)).ToArray();
        var fields = info.Members.Where(m => m.Member is FieldSymbol);
        var fieldInfos = fields.Select(f => GetRuntimeInfo<FieldInfo>(f.Member)).ToArray();
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
                    var atype = GetRuntimeType(tv.Type);
                    return atype;
                case AttributeArrayValue av:
                    var elemType = GetRuntimeType(av.ElementType);
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
}