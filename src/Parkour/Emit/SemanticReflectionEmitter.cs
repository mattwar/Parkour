using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Parkour.Emit;
using Binding;
using Semantics;
using Symbols;

public class SemanticReflectionEmitter
{
    private readonly RuntimeSymbols _runtimeSymbols;

    public SemanticReflectionEmitter(
        RuntimeSymbols runtimeSymbols)
    {
        _runtimeSymbols = runtimeSymbols;
    }

    public AssemblyBuilder EmitAssembly(DeclarationBinding binding, string assemblyName)
    {
        var builder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName(assemblyName),
            AssemblyBuilderAccess.RunAndCollect);

        EmitIntoAssembly(binding, builder);

        return builder;
    }

    public void EmitIntoAssembly(DeclarationBinding binding, AssemblyBuilder builder)
    {
        var moduleName = "Module_" + builder.GetModules().Length;
        var moduleBuilder = builder.DefineDynamicModule(moduleName);
        DefineTypeBuilders(moduleBuilder, binding.DeclarationSymbols);
        DefineTypeBuilderMembers(binding.DeclarationSymbols);
        EmitExpressionBodies(binding, binding.DeclarationSymbols);
        ResolveBuilders(binding.DeclarationSymbols);
    }

    private void DefineTypeBuilders(ModuleBuilder moduleBuilder, NamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.Members)
        {
            if (member is NamespaceSymbol ns)
            {
                DefineTypeBuilders(moduleBuilder, ns);
            }
            else if (member is TypeSymbol ts)
            {
                DefineTypeBuilders(moduleBuilder, ts);
            }
        }
    }

    private void DefineTypeBuilders(ModuleBuilder moduleBuilder, TypeSymbol typeSymbol)
    {
        var name = typeSymbol.FullName;
        TypeBuilder typeBuilder = moduleBuilder.DefineType(name, GetTypeAttributes(typeSymbol));
        AddBuilder(typeSymbol, typeBuilder);

        // declare type parameters
        if (typeSymbol.TypeParameters.Count > 0)
        {
            var typeParamBuilders = typeBuilder.DefineGenericParameters(typeSymbol.TypeParameters.Select(tp => tp.Name).ToArray());
            for (int i = 0; i < typeParamBuilders.Length; i++)
            {
                AddBuilder(typeSymbol.TypeParameters[i], typeParamBuilders[i]);
            }
        }

        foreach (var nestedType in typeSymbol.Members.OfType<TypeSymbol>())
        {
            DefineNestedTypeBuilders(typeBuilder, nestedType);
        }
    }

    private void DefineNestedTypeBuilders(TypeBuilder containerTypeBuilder, TypeSymbol nestedTypeSymbol)
    {
        var name = nestedTypeSymbol.FullName;
        var nestedTypeBuilder = containerTypeBuilder.DefineNestedType(name, GetTypeAttributes(nestedTypeSymbol));
        AddBuilder(nestedTypeSymbol, containerTypeBuilder);

        // declare type parameters
        if (nestedTypeSymbol.TypeParameters.Count > 0)
        {
            var typeParamBuilders = nestedTypeBuilder.DefineGenericParameters(
                nestedTypeSymbol.TypeParameters.Select(tp => tp.Name).ToArray());

            for (int i = 0; i < typeParamBuilders.Length; i++)
            {
                AddBuilder(nestedTypeSymbol.TypeParameters[i], typeParamBuilders[i]);
            }
        }

        foreach (var nestedType in nestedTypeSymbol.Members.OfType<TypeSymbol>())
        {
            DefineNestedTypeBuilders(nestedTypeBuilder, nestedType);
        }
    }

    private void DefineTypeBuilderMembers(NamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.Members)
        {
            if (member is NamespaceSymbol nestedNamespaceSymbol)
            {
                DefineTypeBuilderMembers(nestedNamespaceSymbol);
            }
            else if (member is TypeSymbol typeSymbol)
            {
                DefineTypeMemberBuilders(typeSymbol);
            }
        }
    }

    private void DefineTypeMemberBuilders(TypeSymbol typeSymbol)
    {
        if (TryGetBuilder<TypeBuilder>(typeSymbol, out var typeBuilder))
        {
            // set base type for type now
            var baseTypeSymbol = typeSymbol.BaseTypes.FirstOrDefault(t => !t.IsInterface);
            if (baseTypeSymbol != null)
            {
                typeBuilder.SetParent(GetRuntimeType(baseTypeSymbol));
            }

            // define members
            foreach (var member in typeSymbol.Members)
            {
                switch (member)
                {
                    case FieldSymbol fieldSymbol:
                        var fieldType = GetRuntimeType(fieldSymbol.FieldType);
                        var fieldAttrs = GetFieldAttributes(fieldSymbol);
                        var fieldBuilder = typeBuilder.DefineField(
                            fieldSymbol.Name, 
                            fieldType, 
                            fieldAttrs);

                        AddBuilder(fieldSymbol, fieldBuilder);
                        break;

                    case MethodSymbol methodSymbol:
                        var methodBuilder = typeBuilder.DefineMethod(
                            methodSymbol.Name,
                            GetMethodAttributes(methodSymbol));
                        AddBuilder(methodSymbol, methodBuilder);

                        // define type parameters
                        if (methodSymbol.TypeParameters.Count > 0)
                        {
                            var typeParameterBuilders = methodBuilder.DefineGenericParameters(
                                methodSymbol.TypeParameters.Select(tp => tp.Name).ToArray());

                            for (int i = 0; i < typeParameterBuilders.Length; i++)
                            {
                                AddBuilder(methodSymbol.TypeParameters[i], typeParameterBuilders[i]);
                            }
                        }

                        // set return type after defining type parameters
                        methodBuilder.SetReturnType(GetRuntimeType(methodSymbol.ReturnType));

                        // set parameter types after defining type parametres
                        methodBuilder.SetParameters(methodSymbol.Parameters.Select(p => GetRuntimeType(p.ParameterType)).ToArray());

                        for (int i = 0; i < methodSymbol.Parameters.Count; i++)
                        {
                            var parameterSymbol = methodSymbol.Parameters[i];
                            var parameterBuilder = methodBuilder.DefineParameter(i, GetParameterAttributes(parameterSymbol), parameterSymbol.Name);
                            AddBuilder(parameterSymbol, parameterBuilder);
                        }
                        break;

                    case ConstructorSymbol constructor:
                        var constructorBuilder = typeBuilder.DefineConstructor(
                            GetMethodAttributes(constructor),
                            CallingConventions.Standard,
                            constructor.Parameters.Select(p => GetRuntimeType(p.ParameterType)).ToArray()
                            );
                        AddBuilder(constructor, constructorBuilder);
                        for (int i = 0; i < constructor.Parameters.Count; i++)
                        {
                            var parameterSymbol = constructor.Parameters[i];
                            var parameterBuilder = constructorBuilder.DefineParameter(i, GetParameterAttributes(parameterSymbol), parameterSymbol.Name);
                            AddBuilder(parameterSymbol, parameterBuilder);
                        }
                        break;
                }
            }
        }
    }

    private void ResolveBuilders(Symbol symbol)
    {
        switch (symbol)
        {
            case NamespaceSymbol ns:
                foreach (var member in ns.Members)
                {
                    ResolveBuilders(member);
                }
                break;

            case TypeSymbol ts:
                if (TryGetBuilder<TypeBuilder>(ts, out var typeBuilder))
                {
                    _ = typeBuilder.CreateType();
                }

                foreach (var member in ts.Members)
                {
                    ResolveBuilders(member);
                }
                break;
        }
    }

    private void EmitExpressionBodies(DeclarationBinding binding, Symbol symbol)
    {
        switch (symbol)
        {
            case NamespaceSymbol ns:
                foreach (var member in ns.Members)
                {
                    EmitExpressionBodies(binding, member);
                }
                break;

            case TypeSymbol ts:
                foreach (var member in ts.Members)
                {
                    EmitExpressionBodies(binding, member);
                }
                break;

            case ConstructorSymbol constructor:
                EmitConstructorBody(binding, constructor);
                break;

            case MethodSymbol method:
                EmitMethodBody(binding, method);
                break;
        }
    }

    private void EmitConstructorBody(DeclarationBinding binding, ConstructorSymbol symbol)
    {
        if (TryGetBuilder<ConstructorBuilder>(symbol, out var builder))
        {
            var decl = binding.GetBoundSymbolDeclarations(symbol)
                .OfType<ConstructorDeclaration>()
                .FirstOrDefault();
            if (decl != null)
            {
                var generator = builder.GetILGenerator();
                EmitExpression(generator, decl.Body);
            }
        }
    }

    private void EmitMethodBody(DeclarationBinding binding, MethodSymbol symbol)
    {
        if (TryGetBuilder<MethodBuilder>(symbol, out var builder))
        {
            var decl = binding.GetBoundSymbolDeclarations(symbol)
                .OfType<MethodDeclaration>()
                .FirstOrDefault();
            if (decl != null)
            {
                var generator = builder.GetILGenerator();
                EmitExpression(generator, decl.Body);
            }
        }
    }

    private void EmitExpression(ILGenerator gen, Expression expression)
    {
        throw new NotImplementedException();
    }

    private Type GetRuntimeType(TypeSymbol typeSymbol) =>
        GetRuntimeInfo<TypeInfo>(typeSymbol);

    private FieldInfo GetFieldInfo(FieldSymbol fieldSymbol) =>
        GetRuntimeInfo<FieldInfo>(fieldSymbol);

    private MethodInfo GetMethodInfo(MethodSymbol methodSymbol) =>
        GetRuntimeInfo<MethodInfo>(methodSymbol);

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
        _runtimeSymbols.TryGetRuntimeInfo(symbol, out var info, GetFromBuilders);
        return info;

        object? GetFromBuilders(Symbol symbol)
        {
            return TryGetBuilder(symbol, out var builder)
                && builder is MemberInfo info
                ? info
                : null;
        }
    }

    private readonly Dictionary<Symbol, object> _symbolToBuilderMap =
        new Dictionary<Symbol, object>();

    private void AddBuilder(Symbol symbol, object builder)
    {
        _symbolToBuilderMap.Add(symbol, builder);
    }

    private bool TryGetBuilder(Symbol symbol, [NotNullWhen(true)] out object? builder)
    {
        return _symbolToBuilderMap.TryGetValue(symbol, out builder);
    }

    private bool TryGetBuilder<TBuilder>(Symbol symbol, [NotNullWhen(true)] out TBuilder? builder)
    {
        if (TryGetBuilder(symbol, out var obuilder) 
            && obuilder is TBuilder tbuilder)
        {
            builder = tbuilder;
            return true;
        }
        builder = default!;
        return false;
    }

    private static TypeAttributes GetTypeAttributes(TypeSymbol ts)
    {
        TypeAttributes attrs = default;

        if ((ts.Modifiers & SymbolModifier.Abstract) != 0)
            attrs |= TypeAttributes.Abstract;
        else if ((ts.Modifiers & SymbolModifier.Sealed) != 0)
            attrs |= TypeAttributes.Sealed;
        else if ((ts.Modifiers & SymbolModifier.Special) != 0)
            attrs |= TypeAttributes.SpecialName;

        var isNested = ts.DeclaringSymbol is TypeSymbol;
        switch (ts.Access)
        {
            case SymbolAccess.Private:
                attrs |= isNested ? TypeAttributes.NestedPrivate : TypeAttributes.NotPublic;
                break;
            case SymbolAccess.Public:
                attrs |= isNested ? TypeAttributes.NestedPublic : TypeAttributes.NotPublic;
                break;
            case SymbolAccess.Internal:
                attrs |= isNested ? TypeAttributes.NestedAssembly : TypeAttributes.NotPublic;
                break;
            case SymbolAccess.Protected:
                attrs |= isNested ? TypeAttributes.NestedFamily : TypeAttributes.NotPublic;
                break;
            case SymbolAccess.ProtectedOrInternal:
                attrs |= isNested ? TypeAttributes.NestedFamORAssem : TypeAttributes.NotPublic;
                break;
            case SymbolAccess.ProtectedAndInternal:
                attrs |= isNested ? TypeAttributes.NestedFamANDAssem : TypeAttributes.NotPublic;
                break;
        }

        return attrs;
    }
       
    private FieldAttributes GetFieldAttributes(FieldSymbol field)
    {
        FieldAttributes attrs = default;

        if ((field.Modifiers & SymbolModifier.Static) != 0)
            attrs |= FieldAttributes.Static;

        if ((field.Modifiers & SymbolModifier.Special) != 0)
            attrs |= FieldAttributes.SpecialName;

        switch (field.Access)
        {
            case SymbolAccess.Private:
                attrs |= FieldAttributes.Private;
                break;
            case SymbolAccess.Public:
                attrs |= FieldAttributes.Public;
                break;
            case SymbolAccess.Protected:
                attrs |= FieldAttributes.Family;
                break;
            case SymbolAccess.Internal:
                attrs |= FieldAttributes.Assembly;
                break;
            case SymbolAccess.ProtectedOrInternal:
                attrs |= FieldAttributes.FamORAssem;
                break;
            case SymbolAccess.ProtectedAndInternal:
                attrs |= FieldAttributes.FamANDAssem;
                break;
        }

        return attrs;
    }

    private MethodAttributes GetMethodAttributes(MemberSymbol method)
    {
        MethodAttributes attrs = default;

        if ((method.Modifiers & SymbolModifier.Static) != 0)
            attrs |= MethodAttributes.Static;

        if ((method.Modifiers & SymbolModifier.Special) != 0)
            attrs |= MethodAttributes.SpecialName;

        switch (method.Access)
        {
            case SymbolAccess.Private:
                attrs |= MethodAttributes.Private;
                break;
            case SymbolAccess.Public:
                attrs |= MethodAttributes.Public;
                break;
            case SymbolAccess.Protected:
                attrs |= MethodAttributes.Family;
                break;
            case SymbolAccess.Internal:
                attrs |= MethodAttributes.Assembly;
                break;
            case SymbolAccess.ProtectedOrInternal:
                attrs |= MethodAttributes.FamORAssem;
                break;
            case SymbolAccess.ProtectedAndInternal:
                attrs |= MethodAttributes.FamANDAssem;
                break;
        }

        return attrs;
    }

    private ParameterAttributes GetParameterAttributes(ParameterSymbol parameter)
    {
        return ParameterAttributes.None;
    }
}
