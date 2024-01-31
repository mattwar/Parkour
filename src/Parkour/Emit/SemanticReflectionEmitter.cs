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
    public SemanticReflectionEmitter()
    {
    }

    public virtual AssemblyBuilder Emit(
        DeclarationBinding binding, 
        string assemblyName)
    {
        var builder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName(assemblyName),
            AssemblyBuilderAccess.RunAndCollect);

        var moduleName = "Module" + builder.GetModules().Length;
        EmitIntoAssembly(binding, builder, moduleName);

        return builder;
    }

    public virtual void EmitIntoAssembly(
        DeclarationBinding binding, 
        AssemblyBuilder builder, 
        string moduleName)
    {
        var moduleBuilder = builder.DefineDynamicModule(moduleName);

        if (RuntimeSymbols.TryGet(binding.ExternalSymbols, out var runtimeSymbols))
        {
            var context = new EmitContext(runtimeSymbols);
            DefineTypeBuilders(context, moduleBuilder, binding.DeclarationSymbols);
            DefineTypeBuilderMembers(context, binding.DeclarationSymbols);
            EmitExpressionBodies(context, binding, binding.DeclarationSymbols);
            ResolveBuilders(context, binding.DeclarationSymbols);
        }
    }

    protected virtual void DefineTypeBuilders(
        EmitContext context,
        ModuleBuilder moduleBuilder, 
        NamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.Members)
        {
            if (member is NamespaceSymbol ns)
            {
                DefineTypeBuilders(context, moduleBuilder, ns);
            }
            else if (member is TypeSymbol ts)
            {
                DefineTypeBuilders(context, moduleBuilder, ts);
            }
        }
    }

    protected virtual void DefineTypeBuilders(
        EmitContext context, 
        ModuleBuilder moduleBuilder, 
        TypeSymbol typeSymbol)
    {
        var name = typeSymbol.FullName;
        TypeBuilder typeBuilder = moduleBuilder.DefineType(name, GetTypeAttributes(typeSymbol));
        context.AddBuilder(typeSymbol, typeBuilder);

        // declare type parameters
        if (typeSymbol.TypeParameters.Count > 0)
        {
            var typeParamBuilders = typeBuilder.DefineGenericParameters(typeSymbol.TypeParameters.Select(tp => tp.Name).ToArray());
            for (int i = 0; i < typeParamBuilders.Length; i++)
            {
                context.AddBuilder(typeSymbol.TypeParameters[i], typeParamBuilders[i]);
            }
        }

        foreach (var nestedType in typeSymbol.Members.OfType<TypeSymbol>())
        {
            DefineNestedTypeBuilders(context, typeBuilder, nestedType);
        }
    }

    protected virtual void DefineNestedTypeBuilders(
        EmitContext context,
        TypeBuilder containerTypeBuilder, 
        TypeSymbol nestedTypeSymbol)
    {
        var name = nestedTypeSymbol.FullName;
        var nestedTypeBuilder = containerTypeBuilder.DefineNestedType(name, GetTypeAttributes(nestedTypeSymbol));
        context.AddBuilder(nestedTypeSymbol, containerTypeBuilder);

        // declare type parameters
        if (nestedTypeSymbol.TypeParameters.Count > 0)
        {
            var typeParamBuilders = nestedTypeBuilder.DefineGenericParameters(
                nestedTypeSymbol.TypeParameters.Select(tp => tp.Name).ToArray());

            for (int i = 0; i < typeParamBuilders.Length; i++)
            {
                context.AddBuilder(nestedTypeSymbol.TypeParameters[i], typeParamBuilders[i]);
            }
        }

        foreach (var nestedType in nestedTypeSymbol.Members.OfType<TypeSymbol>())
        {
            DefineNestedTypeBuilders(context, nestedTypeBuilder, nestedType);
        }
    }

    protected virtual void DefineTypeBuilderMembers(
        EmitContext context, 
        NamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.Members)
        {
            if (member is NamespaceSymbol nestedNamespaceSymbol)
            {
                DefineTypeBuilderMembers(context, nestedNamespaceSymbol);
            }
            else if (member is TypeSymbol typeSymbol)
            {
                DefineTypeMemberBuilders(context, typeSymbol);
            }
        }
    }

    protected virtual void DefineTypeMemberBuilders(
        EmitContext context,
        TypeSymbol typeSymbol)
    {
        if (context.TryGetBuilder<TypeBuilder>(typeSymbol, out var typeBuilder))
        {
            // set base type for type now
            var baseTypeSymbol = typeSymbol.BaseTypes.FirstOrDefault(t => !t.IsInterface);
            if (baseTypeSymbol != null)
            {
                typeBuilder.SetParent(context.GetRuntimeType(baseTypeSymbol));
            }

            // define members
            foreach (var member in typeSymbol.Members)
            {
                switch (member)
                {
                    case FieldSymbol fieldSymbol:
                        var fieldType = context.GetRuntimeType(fieldSymbol.FieldType);
                        var fieldAttrs = GetFieldAttributes(fieldSymbol);
                        var fieldBuilder = typeBuilder.DefineField(
                            fieldSymbol.Name, 
                            fieldType, 
                            fieldAttrs);
                        context.AddBuilder(fieldSymbol, fieldBuilder);
                        break;

                    case MethodSymbol methodSymbol:
                        var methodBuilder = typeBuilder.DefineMethod(
                            methodSymbol.Name,
                            GetMethodAttributes(methodSymbol));
                        context.AddBuilder(methodSymbol, methodBuilder);

                        // define type parameters
                        if (methodSymbol.TypeParameters.Count > 0)
                        {
                            var typeParameterBuilders = methodBuilder.DefineGenericParameters(
                                methodSymbol.TypeParameters.Select(tp => tp.Name).ToArray());

                            for (int i = 0; i < typeParameterBuilders.Length; i++)
                            {
                                context.AddBuilder(methodSymbol.TypeParameters[i], typeParameterBuilders[i]);
                            }
                        }

                        // set return type after defining type parameters
                        methodBuilder.SetReturnType(context.GetRuntimeType(methodSymbol.ReturnType));

                        // set parameter types after defining type parametres
                        methodBuilder.SetParameters(
                            methodSymbol.Parameters
                            .Select(p => context.GetRuntimeType(p.ParameterType))
                            .ToArray());

                        for (int i = 0; i < methodSymbol.Parameters.Count; i++)
                        {
                            var parameterSymbol = methodSymbol.Parameters[i];
                            var parameterBuilder = methodBuilder.DefineParameter(i, GetParameterAttributes(parameterSymbol), parameterSymbol.Name);
                            context.AddBuilder(parameterSymbol, parameterBuilder);
                        }
                        break;

                    case ConstructorSymbol constructor:
                        var constructorBuilder = typeBuilder.DefineConstructor(
                            GetMethodAttributes(constructor),
                            CallingConventions.Standard,
                            constructor.Parameters.Select(p => context.GetRuntimeType(p.ParameterType)).ToArray()
                            );
                        context.AddBuilder(constructor, constructorBuilder);
                        for (int i = 0; i < constructor.Parameters.Count; i++)
                        {
                            var parameterSymbol = constructor.Parameters[i];
                            var parameterBuilder = constructorBuilder.DefineParameter(i, GetParameterAttributes(parameterSymbol), parameterSymbol.Name);
                            context.AddBuilder(parameterSymbol, parameterBuilder);
                        }
                        break;
                }
            }
        }
    }

    protected virtual void ResolveBuilders(EmitContext context, Symbol symbol)
    {
        switch (symbol)
        {
            case NamespaceSymbol ns:
                foreach (var member in ns.Members)
                {
                    ResolveBuilders(context, member);
                }
                break;

            case TypeSymbol ts:
                if (context.TryGetBuilder<TypeBuilder>(ts, out var typeBuilder))
                {
                    _ = typeBuilder.CreateType();
                }

                foreach (var member in ts.Members)
                {
                    ResolveBuilders(context, member);
                }
                break;
        }
    }

    protected virtual void EmitExpressionBodies(
        EmitContext context, 
        DeclarationBinding binding, 
        Symbol symbol)
    {
        switch (symbol)
        {
            case NamespaceSymbol ns:
                foreach (var member in ns.Members)
                {
                    EmitExpressionBodies(context, binding, member);
                }
                break;

            case TypeSymbol ts:
                foreach (var member in ts.Members)
                {
                    EmitExpressionBodies(context, binding, member);
                }
                break;

            case ConstructorSymbol constructor:
                EmitConstructorBody(context, binding, constructor);
                break;

            case MethodSymbol method:
                EmitMethodBody(context, binding, method);
                break;
        }
    }

    protected virtual void EmitConstructorBody(
        EmitContext context,
        DeclarationBinding binding, 
        ConstructorSymbol symbol)
    {
        if (context.TryGetBuilder<ConstructorBuilder>(symbol, out var builder))
        {
            var decl = binding.GetBoundSymbolDeclarations(symbol)
                .OfType<ConstructorDeclaration>()
                .FirstOrDefault();
            if (decl != null)
            {
                var generator = builder.GetILGenerator();
                EmitExpression(context, generator, decl.Body);
            }
        }
    }

    protected virtual void EmitMethodBody(
        EmitContext context,
        DeclarationBinding binding, 
        MethodSymbol symbol)
    {
        if (context.TryGetBuilder<MethodBuilder>(symbol, out var builder))
        {
            var decl = binding.GetBoundSymbolDeclarations(symbol)
                .OfType<MethodDeclaration>()
                .FirstOrDefault();
            if (decl != null)
            {
                var generator = builder.GetILGenerator();
                EmitExpression(context, generator, decl.Body);
            }
        }
    }

    protected virtual void EmitExpression(EmitContext context, ILGenerator gen, Expression expression)
    {
        throw new NotImplementedException();
    }

    protected virtual TypeAttributes GetTypeAttributes(TypeSymbol ts)
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
       
    protected virtual FieldAttributes GetFieldAttributes(FieldSymbol field)
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

    protected virtual MethodAttributes GetMethodAttributes(MemberSymbol method)
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

    protected virtual ParameterAttributes GetParameterAttributes(ParameterSymbol parameter)
    {
        return ParameterAttributes.None;
    }

    protected class EmitContext
    {
        private RuntimeSymbols _runtimeSymbols;
        private readonly Dictionary<Symbol, object> _symbolToBuilderMap =
            new Dictionary<Symbol, object>();

        public EmitContext(RuntimeSymbols runtimeSymbols)
        {
            _runtimeSymbols = runtimeSymbols;
        }

        public void AddBuilder(Symbol symbol, object builder)
        {
            _symbolToBuilderMap.Add(symbol, builder);
        }

        public bool TryGetBuilder(Symbol symbol, [NotNullWhen(true)] out object? builder)
        {
            return _symbolToBuilderMap.TryGetValue(symbol, out builder);
        }

        public bool TryGetBuilder<TBuilder>(Symbol symbol, [NotNullWhen(true)] out TBuilder? builder)
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

        public Type GetRuntimeType(TypeSymbol typeSymbol) =>
            GetRuntimeInfo<TypeInfo>(typeSymbol);

        public FieldInfo GetFieldInfo(FieldSymbol fieldSymbol) =>
            GetRuntimeInfo<FieldInfo>(fieldSymbol);

        public MethodInfo GetMethodInfo(MethodSymbol methodSymbol) =>
            GetRuntimeInfo<MethodInfo>(methodSymbol);

        public TInfo GetRuntimeInfo<TInfo>(Symbol symbol)
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

        public object? GetRuntimeInfo(Symbol symbol)
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
    }
}
