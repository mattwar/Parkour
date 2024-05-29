using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace Parkour.Reflection;

using Semantics;
using Symbols;

/// <summary>
/// A <see cref="SemanticEmitter"/> that emits into a <see cref="ModuleBuilder"/>
/// </summary>
public class ReflectionEmitter : SemanticEmitter
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
        var argValues = info.Arguments.Select(a => a.Value).ToArray();
        var props = info.Members.Where(m => m.Member is PropertySymbol);
        var propInfos = props.Select(p => GetRuntimeInfo<PropertyInfo>(p.Member)).ToArray();
        var propValues = props.Select(p => p.Value).ToArray();
        var fields = info.Members.Where(m => m.Member is FieldSymbol);
        var fieldInfos = fields.Select(f => GetRuntimeInfo<FieldInfo>(f.Member)).ToArray();
        var fieldValues = fields.Select(f => f.Value).ToArray();

        return new CustomAttributeBuilder(
            constructor,
            argValues,
            propInfos,
            propValues,
            fieldInfos,
            fieldValues
            );
    }

    /// <summary>
    /// Emits into <see cref="System.Reflection.Emit.ILGenerator"/>
    /// </summary>
    private class ReflectionILEmitter : Semantics.ILEmitter
    {
        private readonly ReflectionEmitter _emitter;
        private readonly ILGenerator _ilgen;

        private readonly Dictionary<Type, Stack<LocalBuilder>> _localPool =
            new Dictionary<Type, Stack<LocalBuilder>>();

        public ReflectionILEmitter(ReflectionEmitter builder, ILGenerator ilgen)
        {
            _emitter = builder;
            _ilgen = ilgen;
        }

        public override SymbolTable ExternalSymbols =>
            _emitter._symbols;

        private readonly Dictionary<LabelSymbol, Label> _labelSymbolToLabelMap =
            new Dictionary<LabelSymbol, Label>();

        private Label GetLabel(LabelSymbol labelSymbol)
        {
            if (!_labelSymbolToLabelMap.TryGetValue(labelSymbol, out var label))
            {
                label = _ilgen.DefineLabel();
                _labelSymbolToLabelMap.Add(labelSymbol, label);
            }

            return label;
        }

        public override void MarkLabel(LabelSymbol labelSymbol)
        {
            var label = GetLabel(labelSymbol);
            _ilgen.MarkLabel(label);
        }

        private readonly Dictionary<VariableSymbol, LocalBuilder> _variableToLocalMap =
            new Dictionary<VariableSymbol, LocalBuilder>();

        public override void DeclareVariableStart(VariableSymbol variable)
        {
            _ = GetLocal(variable);
        }

        public override void DeclareVariableEnd(VariableSymbol variable)
        {
            if (_variableToLocalMap.TryGetValue(variable, out var local))
            {
                _variableToLocalMap.Remove(variable);
                FreeLocal(local);
            }
        }

        private LocalBuilder GetLocal(VariableSymbol variable)
        {
            if (!_variableToLocalMap.TryGetValue(variable, out var local))
            {
                var variableType = _emitter.GetRuntimeType(variable.Type);
                local = AllocateLocal(variableType);
                _variableToLocalMap.Add(variable, local);
            }

            return local;
        }

        private LocalBuilder AllocateLocal(Type type)
        {
            if (_localPool.TryGetValue(type, out var localStack)
                && localStack.Count > 0)
            {
                return localStack.Pop();

            }

            return _ilgen.DeclareLocal(type);
        }

        private void FreeLocal(LocalBuilder local)
        {
            if (!_localPool.TryGetValue(local.LocalType, out var localStack))
            {
                localStack = new Stack<LocalBuilder>();
                _localPool.Add(local.LocalType, localStack);
            }

            localStack.Push(local);
        }

        public override void EmitDup()
        {
            _ilgen.Emit(OpCodes.Dup);
        }

        public override void EmitPop()
        {
            _ilgen.Emit(OpCodes.Pop);
        }

        public override void EmitReturn()
        {
            _ilgen.Emit(OpCodes.Ret);
        }

        public override void EmitBranch(LabelSymbol labelSymbol)
        {
            _ilgen.Emit(OpCodes.Br, GetLabel(labelSymbol));
        }

        public override void EmitBranchTrue(LabelSymbol labelSymbol)
        {
            _ilgen.Emit(OpCodes.Brtrue, GetLabel(labelSymbol));
        }

        public override void EmitBranchFalse(LabelSymbol labelSymbol)
        {
            _ilgen.Emit(OpCodes.Brfalse, GetLabel(labelSymbol));
        }

        public override void EmitLoadInstance()
        {
            EmitLoadArg(0);
        }

        public override void EmitLoadInstanceAddress()
        {
            EmitLoadArgAddress(0);
        }

        public override void EmitLoadParameter(ParameterSymbol parameter)
        {
            if (parameter.DeclaringSymbol is MemberSymbol memberSymbol
                && GetParameterIndex(parameter) is int index
                && index >= 0)
            {
                EmitLoadArg(memberSymbol.IsStatic ? index : index + 1);
            }
        }

        public override void EmitLoadParameterAddress(ParameterSymbol parameter)
        {
            if (parameter.DeclaringSymbol is MemberSymbol memberSymbol
                && GetParameterIndex(parameter) is int index
                && index >= 0)
            {
                EmitLoadArgAddress(memberSymbol.IsStatic ? index : index + 1);
            }
        }

        public override void EmitStoreParameter(ParameterSymbol parameter)
        {
            if (parameter.DeclaringSymbol is MemberSymbol memberSymbol
                && GetParameterIndex(parameter) is int index
                && index >= 0)
            {
                EmitStoreArg(memberSymbol.IsStatic ? index : index + 1);
            }
        }

        private int GetParameterIndex(ParameterSymbol parameter) =>
            parameter.DeclaringSymbol is MethodSymbol ms ? ms.Parameters.IndexOf(parameter)
                : parameter.DeclaringSymbol is ConstructorSymbol cs ? cs.Parameters.IndexOf(parameter)
                : parameter.DeclaringSymbol is DelegateSymbol fs ? fs.Parameters.IndexOf(parameter)
                : -1;

        private void EmitLoadArg(int n)
        {
            switch (n)
            {
                case 0:
                    _ilgen.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    _ilgen.Emit(OpCodes.Ldarg_1);
                    break;
                default:
                    _ilgen.Emit(OpCodes.Ldarg, n);
                    break;
            }
        }

        private void EmitLoadArgAddress(int n)
        {
            if (n >= 0 && n < 256)
            {
                _ilgen.Emit(OpCodes.Ldarga_S, (byte)n);
            }
            else
            {
                _ilgen.Emit(OpCodes.Ldarga, n);
            }
        }

        private void EmitStoreArg(int n)
        {
            if (n >= 0 && n < 256)
            {
                _ilgen.Emit(OpCodes.Starg_S, (byte)n);
            }
            else
            {
                _ilgen.Emit(OpCodes.Starg, n);
            }
        }

        public override void EmitLoadArrayElement(TypeSymbol elementTypeSymbol)
        {
            var type = _emitter.GetRuntimeType(elementTypeSymbol);

            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.SByte:
                    _ilgen.Emit(OpCodes.Ldelem_I1);
                    break;
                case TypeCode.Byte:
                    _ilgen.Emit(OpCodes.Ldelem_U1);
                    break;
                case TypeCode.Int16:
                    _ilgen.Emit(OpCodes.Ldelem_I2);
                    break;
                case TypeCode.UInt16:
                    _ilgen.Emit(OpCodes.Ldelem_U2);
                    break;
                case TypeCode.Int32:
                    _ilgen.Emit(OpCodes.Ldelem_I4);
                    break;
                case TypeCode.UInt32:
                    _ilgen.Emit(OpCodes.Ldelem_U4);
                    break;
                case TypeCode.Int64:
                    _ilgen.Emit(OpCodes.Ldelem_I8);
                    break;
                case TypeCode.Single:
                    _ilgen.Emit(OpCodes.Ldelem_R4);
                    break;
                case TypeCode.Double:
                    _ilgen.Emit(OpCodes.Ldelem_R8);
                    break;
                default:
                    if (!type.IsValueType)
                    {
                        _ilgen.Emit(OpCodes.Ldelem_Ref);
                    }
                    else if (type == typeof(nint))
                    {
                        _ilgen.Emit(OpCodes.Ldelem_I);
                    }
                    else
                    {
                        _ilgen.Emit(OpCodes.Ldelem, type);
                    }
                    break;
            }
        }

        public override void EmitLoadArrayElementAddress(TypeSymbol elementTypeSymbol)
        {
            _ilgen.Emit(OpCodes.Ldelema);
        }

        public override void EmitStoreArrayElement(TypeSymbol elementTypeSymbol)
        {
            var type = _emitter.GetRuntimeType(elementTypeSymbol);

            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                    _ilgen.Emit(OpCodes.Stelem_I1);
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    _ilgen.Emit(OpCodes.Stelem_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    _ilgen.Emit(OpCodes.Stelem_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    _ilgen.Emit(OpCodes.Stelem_I8);
                    break;
                case TypeCode.Single:
                    _ilgen.Emit(OpCodes.Stelem_R4);
                    break;
                case TypeCode.Double:
                    _ilgen.Emit(OpCodes.Stelem_R8);
                    break;
                default:
                    if (!type.IsValueType)
                    {
                        _ilgen.Emit(OpCodes.Stelem_Ref);
                    }
                    else if (type == typeof(nint))
                    {
                        _ilgen.Emit(OpCodes.Stelem_I);
                    }
                    else
                    {
                        _ilgen.Emit(OpCodes.Stelem, type);
                    }
                    break;
            }
        }

        public override void EmitLoadField(FieldSymbol field)
        {
            var fi = _emitter.GetRuntimeInfo<FieldInfo>(field);
            if (fi.IsStatic)
            {
                _ilgen.Emit(OpCodes.Ldsfld, fi);
            }
            else
            {
                _ilgen.Emit(OpCodes.Ldfld, fi);
            }
        }

        public override void EmitLoadFieldAddress(FieldSymbol field)
        {
            var fi = _emitter.GetRuntimeInfo<FieldInfo>(field);
            if (fi.IsStatic)
            {
                _ilgen.Emit(OpCodes.Ldsflda, fi);
            }
            else
            {
                _ilgen.Emit(OpCodes.Ldflda, fi);
            }
        }

        public override void EmitStoreField(FieldSymbol field)
        {
            var fi = _emitter.GetRuntimeInfo<FieldInfo>(field);
            if (fi.IsStatic)
            {
                _ilgen.Emit(OpCodes.Stsfld, fi);
            }
            else
            {
                _ilgen.Emit(OpCodes.Stfld, fi);
            }
        }

        public override void EmitLoadVariable(VariableSymbol variable)
        {
            var loc = GetLocal(variable);
            _ilgen.Emit(OpCodes.Ldloc, loc);
        }

        public override void EmitLoadVariableAddress(VariableSymbol variable)
        {
            var loc = GetLocal(variable);
            _ilgen.Emit(OpCodes.Ldloca, loc);
        }

        public override void EmitStoreVariable(VariableSymbol variable)
        {
            var loc = GetLocal(variable);
            _ilgen.Emit(OpCodes.Stloc, loc);
        }

        public override void EmitLoadNull()
        {
            _ilgen.Emit(OpCodes.Ldnull);
        }

        public override void EmitLoadBool(bool value)
        {
            _ilgen.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        }

        public override void EmitLoadSByte(sbyte value)
        {
            EmitLoadInt32(value);
        }

        public override void EmitLoadByte(byte value)
        {
            EmitLoadInt32(value);
        }

        public override void EmitLoadInt16(short value)
        {
            EmitLoadInt32(value);
        }

        public override void EmitLoadUInt16(ushort value)
        {
            EmitLoadInt32(value);
        }

        public override void EmitLoadUInt32(uint value)
        {
            EmitLoadInt32(unchecked((int)value));
        }

        public override void EmitLoadInt32(int value)
        {
            switch (value)
            {
                case 0:
                    _ilgen.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    _ilgen.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    _ilgen.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    _ilgen.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    _ilgen.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    _ilgen.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    _ilgen.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    _ilgen.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    _ilgen.Emit(OpCodes.Ldc_I4_8);
                    break;
                case -1:
                    _ilgen.Emit(OpCodes.Ldc_I4_M1);
                    break;
                default:
                    if (value >= 0 && value < 256)
                    {
                        _ilgen.Emit(OpCodes.Ldc_I4_S, (byte)value);
                    }
                    else
                    {
                        _ilgen.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }

        public override void EmitLoadInt64(long value)
        {
            _ilgen.Emit(OpCodes.Ldc_I8, value);
        }

        public override void EmitLoadUInt64(ulong value)
        {
            EmitLoadInt64(unchecked((long)value));
        }

        public override void EmitLoadSingle(float value)
        {
            _ilgen.Emit(OpCodes.Ldc_R4, value);
        }

        public override void EmitLoadDouble(double value)
        {
            _ilgen.Emit(OpCodes.Ldc_R8, value);
        }

        public override void EmitLoadDecimal(decimal value)
        {
            var dec = (decimal)value;
            Span<int> bits = stackalloc int[4];
            decimal.GetBits(dec, bits);
            var scale = (bits[3] & int.MaxValue) >> 16;
            EmitLoadInt32(bits[0]);
            EmitLoadInt32(bits[1]);
            EmitLoadInt32(bits[2]);
            EmitLoadInt32((bits[3] & 0x80000000) != 0 ? 1 : 0);
            EmitLoadInt32(scale);
            _ilgen.Emit(OpCodes.Call, Decimal_Constructor);
        }

        private static ConstructorInfo Decimal_Constructor =
           typeof(decimal).GetConstructor([typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte)])!;

        public override void EmitLoadString(string value)
        {
            _ilgen.Emit(OpCodes.Ldstr, value);
        }

        public override void EmitLoadChar(char value)
        {
            EmitLoadUInt16(value);
        }

        public override void EmitLoadMethod(MethodSymbol methodSymbol)
        {
            var info = _emitter.GetRuntimeInfo<MethodInfo>(methodSymbol);
            _ilgen.Emit(OpCodes.Ldftn, info);
        }

        public override void EmitLoadToken(MemberSymbol symbol)
        {
            var info = _emitter.GetRuntimeInfo(symbol);
            switch (info)
            {
                case MethodInfo mi:
                    _ilgen.Emit(OpCodes.Ldtoken, mi);
                    break;
                case FieldInfo fi:
                    _ilgen.Emit(OpCodes.Ldtoken, fi);
                    break;
                case Type type:
                    _ilgen.Emit(OpCodes.Ldtoken, type);
                    break;
            }
        }

        private static FieldInfo DateTime_Default =
            typeof(DateTime).GetField("MinValue", BindingFlags.Static | BindingFlags.Public)!;

        private static FieldInfo Decimal_Default =
            typeof(decimal).GetField("Zero", BindingFlags.Static | BindingFlags.Public)!;

        public override void EmitDefault(TypeSymbol typeSymbol)
        {
            var type = _emitter.GetRuntimeType(typeSymbol);
            EmitDefault(type);
        }

        private void EmitDefault(Type type)
        {
            switch (TypeInfo.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    _ilgen.Emit(OpCodes.Ldc_I4_0);
                    break;

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    _ilgen.Emit(OpCodes.Ldc_I4_0);
                    _ilgen.Emit(OpCodes.Conv_I8);
                    break;

                case TypeCode.Single:
                    _ilgen.Emit(OpCodes.Ldc_R4, 0.0f);
                    break;

                case TypeCode.Double:
                    _ilgen.Emit(OpCodes.Ldc_R8, 0.0);
                    break;

                case TypeCode.Decimal:
                    _ilgen.Emit(OpCodes.Ldsfld, Decimal_Default);
                    break;

                case TypeCode.DateTime:
                    _ilgen.Emit(OpCodes.Ldsfld, DateTime_Default);
                    break;

                default:
                    if (type.IsValueType)
                    {
                        var local = AllocateLocal(type);
                        _ilgen.Emit(OpCodes.Ldloca, local);
                        _ilgen.Emit(OpCodes.Initobj, type);
                        _ilgen.Emit(OpCodes.Ldloc, local);
                        FreeLocal(local);
                    }
                    else
                    {
                        EmitLoadNull();
                    }
                    break;
            }
        }

        public override void EmitCall(MethodSymbol methodSymbol)
        {
            var method = _emitter.GetRuntimeInfo<MethodInfo>(methodSymbol);

            var instanceIsValueType = (method.DeclaringType != null && method.DeclaringType.IsValueType);
            var op = method.IsStatic || instanceIsValueType
                ? OpCodes.Call
                : OpCodes.Callvirt;

            _ilgen.Emit(op, method);
        }

        public override void EmitCall(ConstructorSymbol constructorSymbol)
        {
            var info = _emitter.GetRuntimeInfo<ConstructorInfo>(constructorSymbol);
            _ilgen.Emit(OpCodes.Call, info);
        }

        public override void EmitNew(ConstructorSymbol constructorSymbol)
        {
            var info = _emitter.GetRuntimeInfo<ConstructorInfo>(constructorSymbol);
            _ilgen.Emit(OpCodes.Newobj, info);
        }

        public override void EmitNewSZArray(TypeSymbol elementTypeSymbol)
        {
            var info = _emitter.GetRuntimeType(elementTypeSymbol);
            _ilgen.Emit(OpCodes.Newarr, info);
        }

        public override void EmitInit(TypeSymbol typeSymbol)
        {
            var type = _emitter.GetRuntimeType(typeSymbol);
            var local = AllocateLocal(type);
            _ilgen.Emit(OpCodes.Ldloca, local);
            _ilgen.Emit(OpCodes.Initobj, type);
            _ilgen.Emit(OpCodes.Ldloc, local);
            FreeLocal(local);
        }

        public override void EmitConvert(TypeSymbol sourceTypeSymbol, TypeSymbol targetTypeSymbol, bool isChecked)
        {
            var sourceType = _emitter.GetRuntimeType(sourceTypeSymbol);
            var targetType = _emitter.GetRuntimeType(targetTypeSymbol);

            if (sourceType == targetType)
            {
                // do nothing since same type
                return;
            }
            else if (targetType == typeof(void))
            {
                // target does not expect a type and expression type is not void.
                EmitPop();
                return;
            }
            else if (sourceType == typeof(void))
            {
                // source has no type (so no value was left on stack), but target expects a type (not void)
                EmitDefault(targetTypeSymbol);
                return;
            }
            else if (targetType == typeof(object))
            {
                if (sourceType.IsValueType)
                {
                    _ilgen.Emit(OpCodes.Box, sourceType);
                }
                return;
            }
            else if (sourceType == typeof(object))
            {
                if (targetType.IsValueType)
                {
                    _ilgen.Emit(OpCodes.Unbox_Any, targetType);
                }
                else
                {
                    _ilgen.Emit(OpCodes.Castclass, targetType);
                }
            }
            else if (sourceType.IsPrimitive && targetType.IsPrimitive)
            {
                // both are primitives, so try
                var success = TryEmitConvertToType(sourceType, targetType, isChecked);
                if (success)
                    return;
            }
            else if (!targetType.IsInterface && !targetType.IsValueType && sourceType.IsSubclassOf(targetType))
            {
                // do nothing since source is derived type from target
            }
            else if (!targetType.IsInterface && !targetType.IsValueType && targetType.IsSubclassOf(sourceType))
            {
                // target type is a derived type of source type, so try runtime cast
                _ilgen.Emit(OpCodes.Castclass, targetType);
            }
            else if (targetType.IsInterface && sourceType.IsAssignableTo(targetType))
            {
                if (sourceType.IsValueType)
                {
                    _ilgen.Emit(OpCodes.Box, sourceType);
                }

                return;
            }
            else
            {
                EmitPop();
                EmitDefault(targetType);
                EmitThrowAndReport(new Diagnostic($"Cannot convert from type '{sourceType.Name}' to '{targetType.Name}'"));
            }
        }

        /// <summary>
        /// Emits a conversion of a value on the stack (source type) to the target type if possible.
        /// </summary>
        private bool TryEmitConvertToType(Type sourceType, Type targetType, bool isChecked)
        {
            return TypeInfo.GetTypeCode(targetType) switch
            {
                TypeCode.SByte => TryEmitConvertToSByte(sourceType, isChecked),
                TypeCode.Byte => TryEmitConvertToByte(sourceType, isChecked),
                TypeCode.Int16 => TryEmitConvertToInt16(sourceType, isChecked),
                TypeCode.UInt16 => TryEmitConvertToUInt16(sourceType, isChecked),
                TypeCode.Int32 => TryEmitConvertToInt32(sourceType, isChecked),
                TypeCode.UInt32 => TryEmitConvertToUInt32(sourceType, isChecked),
                TypeCode.Int64 => TryEmitConvertToInt64(sourceType, isChecked),
                TypeCode.UInt64 => TryEmitConvertToUInt64(sourceType, isChecked),
                TypeCode.Single => TryEmitConvertToSingle(sourceType), // always checked
                TypeCode.Double => TryEmitConvertToDouble(sourceType), // always checked
                _ => false
            };
        }

        private bool TryEmitConvertToSByte(Type sourceType, bool isChecked)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.SByte:
                    break;

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I1_Un : OpCodes.Conv_I1);
                    break;

                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToByte(Type sourceType, bool isChecked)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.Byte:
                    break;

                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U1_Un : OpCodes.Conv_U1);
                    break;

                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToInt16(Type sourceType, bool isChecked)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                    _ilgen.Emit(OpCodes.Conv_I2);
                    break;

                case TypeCode.Int16:
                    break;

                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I2_Un : OpCodes.Conv_I2);
                    break;

                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToUInt16(Type sourceType, bool isChecked)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                    _ilgen.Emit(OpCodes.Conv_U2);
                    break;

                case TypeCode.UInt16:
                    break;

                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U2_Un : OpCodes.Conv_U2);
                    break;

                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToInt32(Type sourceType, bool isChecked)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    _ilgen.Emit(OpCodes.Conv_I4);
                    break;

                case TypeCode.Int32:
                    break;

                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I4_Un : OpCodes.Conv_I4);
                    break;

                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I4 : OpCodes.Conv_I4);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToUInt32(Type sourceType, bool isChecked)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U4 : OpCodes.Conv_U4);
                    break;

                case TypeCode.Byte:
                case TypeCode.UInt16:
                    _ilgen.Emit(OpCodes.Conv_U4);
                    break;

                case TypeCode.UInt32:
                    break;

                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U4_Un : OpCodes.Conv_U4);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToInt64(Type sourceType, bool isChecked)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    _ilgen.Emit(OpCodes.Conv_I8);
                    break;

                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I8_Un : OpCodes.Conv_I8);
                    break;

                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(OpCodes.Conv_Ovf_I8);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToUInt64(Type sourceType, bool isChecked)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U8 : OpCodes.Conv_U8);
                    break;

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                    _ilgen.Emit(OpCodes.Conv_U8);
                    break;

                case TypeCode.UInt64:
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToSingle(Type sourceType)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Double:
                    _ilgen.Emit(OpCodes.Conv_R4);
                    break;

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(OpCodes.Conv_R_Un);
                    break;

                case TypeCode.Single:
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToDouble(Type sourceType)
        {
            switch (TypeInfo.GetTypeCode(sourceType))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                    _ilgen.Emit(OpCodes.Conv_R8);
                    break;

                case TypeCode.Double:
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool IsUnsigned(Type type)
        {
            switch (TypeInfo.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Boolean:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsFloatingPoint(Type type)
        {
            switch (TypeInfo.GetTypeCode(type))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        public override void EmitAsType(TypeSymbol instanceTypeSymbol)
        {
            var instanceType = _emitter.GetRuntimeType(instanceTypeSymbol);
            _ilgen.Emit(OpCodes.Isinst, instanceType);
        }

        public override void EmitAdd(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? OpCodes.Add
                : IsUnsigned(operandType) ? OpCodes.Add_Ovf_Un
                : OpCodes.Add_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitSubtract(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? OpCodes.Sub
                : IsUnsigned(operandType) ? OpCodes.Sub_Ovf_Un
                : OpCodes.Sub_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitMultiply(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? OpCodes.Mul
                : IsUnsigned(operandType) ? OpCodes.Mul_Ovf_Un
                : OpCodes.Mul_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitDivide(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            var op = IsUnsigned(operandType) ? OpCodes.Div_Un : OpCodes.Div;
            _ilgen.Emit(op);
        }

        public override void EmitRemainder(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            var op = IsUnsigned(operandType) ? OpCodes.Rem_Un : OpCodes.Rem;
            _ilgen.Emit(op);
        }

        public override void EmitNegate(TypeSymbol operandTypeSymbol)
        {
            _ilgen.Emit(OpCodes.Neg);
        }

        public override void EmitIncrement(TypeSymbol operandType, bool isChecked)
        {
            EmitLoadInt32(1);
            EmitAdd(operandType, isChecked);
        }

        public override void EmitDecrement(TypeSymbol operandType, bool isChecked)
        {
            EmitLoadInt32(1);
            EmitSubtract(operandType, isChecked);
        }

        public override void EmitAnd()
        {
            _ilgen.Emit(OpCodes.And);
        }

        public override void EmitOr()
        {
            _ilgen.Emit(OpCodes.Or);
        }

        public override void EmitXor()
        {
            _ilgen.Emit(OpCodes.Xor);
        }

        public override void EmitNot()
        {
            _ilgen.Emit(OpCodes.Not);
        }

        public override void EmitShiftLeft(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            var mask = (operandType == typeof(long) || operandType == typeof(ulong)) ? 0x3F : 0x1F;
            EmitLoadInt32(mask);
            _ilgen.Emit(OpCodes.And);
            _ilgen.Emit(OpCodes.Shl);
        }

        public override void EmitShiftRight(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            var mask = (operandType == typeof(long) || operandType == typeof(ulong)) ? 0x3F : 0x1F;
            EmitLoadInt32(mask);
            _ilgen.Emit(OpCodes.And);
            _ilgen.Emit(IsUnsigned(operandType) ? OpCodes.Shr_Un : OpCodes.Shr);
        }

        public override void EmitEqual(TypeSymbol operandTypeSymbol)
        {
            _ilgen.Emit(OpCodes.Ceq);
        }

        public override void EmitNotEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            if (operandType == typeof(bool))
            {
                _ilgen.Emit(OpCodes.Xor);
            }
            else
            {
                // OMG
                _ilgen.Emit(OpCodes.Ceq);
                _ilgen.Emit(OpCodes.Ldc_I4_0);
                _ilgen.Emit(OpCodes.Ceq);
            }
        }

        public override void EmitLessThan(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) ? OpCodes.Clt_Un : OpCodes.Clt);
        }

        public override void EmitLessThanOrEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) || IsFloatingPoint(operandType) ? OpCodes.Cgt_Un : OpCodes.Cgt);
            _ilgen.Emit(OpCodes.Ldc_I4_0);
            _ilgen.Emit(OpCodes.Ceq);
        }

        public override void EmitGreaterThan(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) ? OpCodes.Cgt_Un : OpCodes.Cgt);
        }

        public override void EmitGreaterThanOrEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetRuntimeType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) || IsFloatingPoint(operandType) ? OpCodes.Clt_Un : OpCodes.Clt);
            _ilgen.Emit(OpCodes.Ldc_I4_0);
            _ilgen.Emit(OpCodes.Ceq);
        }

        public override void EmitThrow(string message)
        {
            EmitThrow(typeof(InvalidOperationException), message);
        }

        public override void EmitThrowAndReport(Diagnostic diagnostic)
        {
            EmitThrow(diagnostic.ToString());
            _emitter._diagnostics.Add(diagnostic);
        }

        public override void EmitThrow(TypeSymbol exceptionTypeSymbol, string message)
        {
            var exceptionType = _emitter.GetRuntimeType(exceptionTypeSymbol);
            EmitThrow(exceptionType, message);
        }

        private void EmitThrow(Type exceptionType, string message)
        {
            _ilgen.Emit(OpCodes.Ldstr, message);
            var ci = exceptionType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(string)]);
            _ilgen.Emit(OpCodes.Newobj, ci!);
            _ilgen.ThrowException(typeof(InvalidOperationException));
        }
    }
}