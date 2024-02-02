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
            EmitSymbolBodies(context, binding, binding.DeclarationSymbols);
            CreateTypes(context, binding.DeclarationSymbols);
        }
    }

    #region Define Builders
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
    #endregion

    #region Emit Bodies
    protected virtual void EmitSymbolBodies(
        EmitContext context, 
        DeclarationBinding binding, 
        Symbol symbol)
    {
        switch (symbol)
        {
            case ConstructorSymbol constructor:
                EmitConstructorBody(context, binding, constructor);
                break;

            case MethodSymbol method:
                EmitMethodBody(context, binding, method);
                break;

            case NamespaceSymbol ns:
                foreach (var member in ns.Members)
                {
                    EmitSymbolBodies(context, binding, member);
                }
                break;

            case TypeSymbol ts:
                foreach (var member in ts.Members)
                {
                    EmitSymbolBodies(context, binding, member);
                }
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
                EmitMethodBody(context, generator, decl.Body, typeof(void), decl.ReturnLabel);
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
            var decls = binding.GetBoundSymbolDeclarations(symbol);
            var decl = decls
                .OfType<MethodDeclaration>()
                .FirstOrDefault();

            if (decl != null)
            {
                var generator = builder.GetILGenerator();
                var returnType = context.GetRuntimeType(symbol.ReturnType);
                EmitMethodBody(context, generator, decl.Body, returnType, decl.ReturnLabel);
            }
        }
    }

    protected virtual void EmitMethodBody(
        EmitContext context, ILGenerator gen, Expression body, Type returnType, LabelSymbol? returnLabel)
    {
        Label retLabelBuilder = gen.DefineLabel();

        if (returnLabel != null)
        {
            context.AddBuilder(returnLabel, retLabelBuilder);
        }

        var bodyType = context.GetRuntimeType(body.ResultType);
        EmitExpressionAsType(context, gen, body, bodyType);

        gen.MarkLabel(retLabelBuilder);
        gen.Emit(OpCodes.Ret);
    }

    protected virtual void EmitExpression(EmitContext context, ILGenerator gen, Expression expression)
    {
        switch (expression)
        {
            case BlockExpression block:
                EmitBlock(context, gen, block);
                break;

            case BranchExpression branch:
                EmitBranch(context, gen, branch);
                break;

            case ConstantExpression constant:
                EmitConstant(context, gen, constant);
                break;

            case DefaultExpression @default:
                EmitDefault(context, gen, @default);
                break;

            case LabelExpression label:
                EmitLabel(context, gen, label);
                break;

            case VariableExpression variable:
                EmitVariable(context, gen, variable);
                break;
        }
    }

    protected virtual void EmitExpressionAsType(EmitContext context, ILGenerator gen, Expression expression, Type targetType)
    {
        EmitExpression(context, gen, expression);

        var sourceType = context.GetRuntimeType(expression.ResultType);
        if (sourceType == targetType)
        {
            // do nothing
            return;
        }
        else if (targetType == typeof(void))
        {
            gen.Emit(OpCodes.Pop);
            return;
        }
        else if (sourceType == typeof(void))
        {
            EmitDefault(context, gen, targetType);
            return;
        }
        else if (sourceType.IsPrimitive && targetType.IsPrimitive)
        {
            var success = TryEmitConvertToType(gen, sourceType, targetType);
            if (success)
                return;
        }
        else if (targetType == typeof(object))
        {
            if (sourceType.IsValueType)
            {
                gen.Emit(OpCodes.Box);
            }

            return;
        }
        else if (targetType.IsInterface && sourceType.IsAssignableTo(targetType))
        {
            if (sourceType.IsValueType)
            {
                gen.Emit(OpCodes.Box);
            }

            return;
        }
        else if (!targetType.IsInterface && !targetType.IsValueType && sourceType.IsSubclassOf(targetType))
        {
            // do nothing
        }

        throw new InvalidOperationException($"Cannot convert from type '{sourceType.Name}' to '{targetType.Name}'");
    }

    #region TryEmitConvertToType
    protected virtual bool TryEmitConvertToType(ILGenerator gen, Type sourceType, Type targetType)
    {
        return TypeInfo.GetTypeCode(targetType) switch
        {
            TypeCode.SByte => TryEmitConvertToSByte(gen, sourceType),
            TypeCode.Byte => TryEmitConvertToByte(gen, sourceType),
            TypeCode.Int16 => TryEmitConvertToInt16(gen, sourceType),
            TypeCode.UInt16 => TryEmitConvertToUInt16(gen, sourceType),
            TypeCode.Int32 => TryEmitConvertToInt32(gen, sourceType),
            TypeCode.UInt32 => TryEmitConvertToUInt32(gen, sourceType),
            TypeCode.Int64 => TryEmitConvertToInt64(gen, sourceType),
            TypeCode.UInt64 => TryEmitConvertToUInt64(gen, sourceType),
            TypeCode.Single => TryEmitConvertToSingle(gen, sourceType),
            TypeCode.Double => TryEmitConvertToDouble(gen, sourceType),
            _ => false
        };
    }

    private bool TryEmitConvertToSByte(ILGenerator gen, Type sourceType)
    {
        switch (TypeInfo.GetTypeCode(sourceType))
        {
            case TypeCode.SByte:
                break;

            case TypeCode.Byte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                gen.Emit(OpCodes.Conv_Ovf_I1_Un);
                break;

            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                gen.Emit(OpCodes.Conv_Ovf_I1);
                break;

            default:
                return false;
        }

        return true;
    }

    private bool TryEmitConvertToByte(ILGenerator gen, Type sourceType)
    {
        switch (TypeInfo.GetTypeCode(sourceType))
        {
            case TypeCode.Byte:
                break;

            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                gen.Emit(OpCodes.Conv_Ovf_U1_Un);
                break;

            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                gen.Emit(OpCodes.Conv_Ovf_U1);
                break;

            default:
                return false;
        }

        return true;
    }

    private bool TryEmitConvertToInt16(ILGenerator gen, Type sourceType)
    {
        switch (TypeInfo.GetTypeCode(sourceType))
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
                gen.Emit(OpCodes.Conv_I2);
                break;

            case TypeCode.Int16:
                break;

            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                gen.Emit(OpCodes.Conv_Ovf_I2_Un);
                break;

            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                gen.Emit(OpCodes.Conv_Ovf_I2);
                break;

            default:
                return false;
        }

        return true;
    }

    private bool TryEmitConvertToUInt16(ILGenerator gen, Type sourceType)
    {
        switch (TypeInfo.GetTypeCode(sourceType))
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
                gen.Emit(OpCodes.Conv_U2);
                break;

            case TypeCode.UInt16:
                break;

            case TypeCode.UInt32:
            case TypeCode.UInt64:
                gen.Emit(OpCodes.Conv_Ovf_U2_Un);
                break;

            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                gen.Emit(OpCodes.Conv_Ovf_U2);
                break;

            default:
                return false;
        }

        return true;
    }

    private bool TryEmitConvertToInt32(ILGenerator gen, Type sourceType)
    {
        switch (TypeInfo.GetTypeCode(sourceType))
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
                gen.Emit(OpCodes.Conv_I4);
                break;

            case TypeCode.Int32:
                break;

            case TypeCode.UInt32:
            case TypeCode.UInt64:
                gen.Emit(OpCodes.Conv_Ovf_I4_Un);
                break;

            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                gen.Emit(OpCodes.Conv_Ovf_I4);
                break;

            default:
                return false;
        }

        return true;
    }

    private bool TryEmitConvertToUInt32(ILGenerator gen, Type sourceType)
    {
        switch (TypeInfo.GetTypeCode(sourceType))
        {
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                gen.Emit(OpCodes.Conv_Ovf_U4);
                break;

            case TypeCode.Byte:
            case TypeCode.UInt16:
                gen.Emit(OpCodes.Conv_U4);
                break;

            case TypeCode.UInt32:
                break;

            case TypeCode.UInt64:
                gen.Emit(OpCodes.Conv_Ovf_U4_Un);
                break;

            default:
                return false;
        }

        return true;
    }

    private bool TryEmitConvertToInt64(ILGenerator gen, Type sourceType)
    {
        switch (TypeInfo.GetTypeCode(sourceType))
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
                gen.Emit(OpCodes.Conv_I8);
                break;

            case TypeCode.UInt64:
                gen.Emit(OpCodes.Conv_Ovf_I8_Un);
                break;

            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                gen.Emit(OpCodes.Conv_Ovf_I8);
                break;

            default:
                return false;
        }

        return true;
    }

    private bool TryEmitConvertToUInt64(ILGenerator gen, Type sourceType)
    {
        switch (TypeInfo.GetTypeCode(sourceType))
        {
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
                gen.Emit(OpCodes.Conv_Ovf_U8);
                break;

            case TypeCode.Byte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
                gen.Emit(OpCodes.Conv_U4);
                break;

            case TypeCode.UInt64:
                break;

            default:
                return false;
        }

        return true;
    }

    private bool TryEmitConvertToSingle(ILGenerator gen, Type sourceType)
    {
        switch (TypeInfo.GetTypeCode(sourceType))
        {
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Double:
                gen.Emit(OpCodes.Conv_R4);
                break;

            case TypeCode.Byte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                gen.Emit(OpCodes.Conv_R_Un);
                break;

            case TypeCode.Single:
                break;

            default:
                return false;
        }

        return true;
    }

    private bool TryEmitConvertToDouble(ILGenerator gen, Type sourceType)
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
                gen.Emit(OpCodes.Conv_R8);
                break;

            case TypeCode.Double:
                break;

            default:
                return false;
        }

        return true;
    }

    #endregion

    #region Block Emit
    protected virtual void EmitBlock(EmitContext context, ILGenerator gen, BlockExpression block)
    {
        // pre-declare variables
        foreach (var variableExprs in block.Expressions.OfType<VariableExpression>())
        {
            if (variableExprs.Variable != null)
            {
                var variableType = context.GetRuntimeType(variableExprs.ResultType);
                var localBuilder = gen.DeclareLocal(variableType);
                context.AddBuilder(variableExprs.Variable, localBuilder);
            }
        }

        // pre-define labels
        foreach (var labelExpr in block.Expressions.OfType<LabelExpression>())
        {
            if (labelExpr.LabelSymbol != null)
            {
                var label = gen.DefineLabel();
                context.AddBuilder(labelExpr.LabelSymbol, label);
            }
        }

        for (int i = 0; i < block.Expressions.Count; i++)
        {
            if (i < block.Expressions.Count - 1)
            {
                EmitExpressionAsType(context, gen, block.Expressions[i], typeof(void));
            }
            else
            {
                var blockResultType = context.GetRuntimeType(block.ResultType);
                EmitExpressionAsType(context, gen, block.Expressions[i], blockResultType);
            }
        }
    }
    #endregion

    #region Branch Emit
    protected virtual void EmitBranch(EmitContext context, ILGenerator gen, BranchExpression branch)
    {
        if (branch.LabelSymbol != null
            && context.TryGetBuilder<Label>(branch.LabelSymbol, out var label))
        {
            var recievingType = context.GetRuntimeType(branch.LabelSymbol.Type);

            if (branch.Expression != null)
            {
                EmitExpressionAsType(context, gen, branch.Expression, recievingType);
            }

            gen.Emit(OpCodes.Br, label);
        }
    }
    #endregion

    #region Call Emit
    protected virtual void EmitCall(EmitContext context, ILGenerator gen, CallExpression call)
    {
        var calledSymbol = call.CalledSymbol as MethodSymbol;

        if (calledSymbol != null
            && context.TryGetBuilder<MethodInfo>(calledSymbol, out var method))
        {
            var instance = GetCallInstance(call.Expression);
            if (instance != null)
            {
                EmitExpression(context, gen, instance);
            }

            EmitArguments(context, gen, calledSymbol.Parameters, call.Arguments);

            gen.EmitCall(OpCodes.Call, method, null);
        }
    }

    protected virtual void EmitArguments(EmitContext context, ILGenerator gen, ImmutableList<ParameterSymbol> parameters, ImmutableList<Expression> arguments)
    {
        for (int i = 0; i < arguments.Count; i++)
        {
            var ptype = context.GetRuntimeType(parameters[i].ParameterType);
            EmitExpressionAsType(context, gen, arguments[i], ptype);
        }
    }

    /// <summary>
    /// Gets the instance of the call from the called expression.
    /// </summary>
    protected virtual Expression? GetCallInstance(Expression expression)
    {
        switch (expression)
        {
            case MemberExpression member:
                return member.Expression;
            case AdjustedReferenceExpression filter:
                return GetCallInstance(filter.Expression);
            default:
                return null;
        }
    }
    #endregion

    #region Constant Emit
    protected virtual void EmitConstant(EmitContext context, ILGenerator gen, ConstantExpression constant)
    {
        EmitValue(gen, constant.Value);
    }

    private static ConstructorInfo DateTime_Constructor =
        typeof(DateTime).GetConstructor([typeof(int)])!;

    private static ConstructorInfo Decimal_Constructor =
       typeof(decimal).GetConstructor([typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte)])!;
        
    private void EmitValue(ILGenerator gen, object? value)
    {
        if (value == null)
        {
            gen.Emit(OpCodes.Ldnull);
            return;
        }
        else
        {
            var type = value.GetType();
            switch (TypeInfo.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    if ((bool)value)
                        gen.Emit(OpCodes.Ldc_I4_1);
                    else
                        gen.Emit(OpCodes.Ldc_I4_0);
                    break;
                case TypeCode.Byte:
                    gen.Emit(OpCodes.Ldc_I4, (byte)value);
                    break;
                case TypeCode.SByte:
                    gen.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    break;
                case TypeCode.Int16:
                    gen.Emit(OpCodes.Ldc_I4, (int)(short)value);
                    break;
                case TypeCode.UInt16:
                    gen.Emit(OpCodes.Ldc_I4, unchecked((int)(uint)(ushort)value));
                    break;
                case TypeCode.Int32:
                    gen.Emit(OpCodes.Ldc_I4, (int)value);
                    break;
                case TypeCode.UInt32:
                    gen.Emit(OpCodes.Ldc_I4, unchecked((int)(uint)value));
                    break;
                case TypeCode.Int64:
                    gen.Emit(OpCodes.Ldc_I8, (long)value);
                    break;
                case TypeCode.UInt64:
                    gen.Emit(OpCodes.Ldc_I8, unchecked((long)(ulong)value));
                    break;
                case TypeCode.Single:
                    gen.Emit(OpCodes.Ldc_R4, (float)value);
                    break;
                case TypeCode.Double:
                    gen.Emit(OpCodes.Ldc_R8, (double)value);
                    break;
                case TypeCode.Char:
                    gen.Emit(OpCodes.Ldc_I4, (int)(char)value);
                    break;
                case TypeCode.String:
                    gen.Emit(OpCodes.Ldstr, (string)value);
                    break;
                case TypeCode.DateTime:
                    {
                        gen.Emit(OpCodes.Ldc_I8, ((DateTime)value).Ticks);
                        gen.Emit(OpCodes.Call, DateTime_Constructor);
                        break;
                    }
                case TypeCode.Decimal:
                    {
                        var dec = (decimal)value;
                        Span<int> bits = stackalloc int[4];
                        decimal.GetBits(dec, bits);
                        var scale = (bits[3] & int.MaxValue) >> 16;
                        gen.Emit(OpCodes.Ldc_I4, bits[0]);
                        gen.Emit(OpCodes.Ldc_I4, bits[1]);
                        gen.Emit(OpCodes.Ldc_I4, bits[2]);
                        gen.Emit((bits[3] & 0x80000000) != 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        gen.Emit(OpCodes.Ldc_I4, scale);
                        gen.Emit(OpCodes.Call, Decimal_Constructor);
                        break;
                    }
                default:
                    // some other object literal?
                    throw new InvalidOperationException($"Unhandled constant value type '{value.GetType().Name}' in EmitValue");
            }
        }
    }
    #endregion

    #region Default Emit
    protected virtual void EmitDefault(EmitContext context, ILGenerator gen, DefaultExpression @default)
    {
        var type = context.GetRuntimeType(@default.ResultType);
        EmitDefault(context, gen, type);
    }

    private static FieldInfo DateTime_Default =
        typeof(DateTime).GetField("MinValue", BindingFlags.Static | BindingFlags.Public)!;

    private static FieldInfo Decimal_Default =
        typeof(decimal).GetField("Zero", BindingFlags.Static | BindingFlags.Public)!;

    private void EmitDefault(EmitContext context, ILGenerator gen, Type type)
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
                gen.Emit(OpCodes.Ldc_I4_0);
                break;

            case TypeCode.Int64:
            case TypeCode.UInt64:
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Conv_I8);
                break;

            case TypeCode.Single:
                gen.Emit(OpCodes.Ldc_R4, 0.0f);
                break;

            case TypeCode.Double:
                gen.Emit(OpCodes.Ldc_R8, 0.0);
                break;

            case TypeCode.Decimal:
                gen.Emit(OpCodes.Ldsfld, Decimal_Default);
                break;

            case TypeCode.DateTime:
                gen.Emit(OpCodes.Ldsfld, DateTime_Default);
                break;

            default:
                if (type.IsValueType)
                {
                    var local = context.AllocateLocal(type, gen);
                    gen.Emit(OpCodes.Ldloca, local);
                    gen.Emit(OpCodes.Initobj, type);
                    gen.Emit(OpCodes.Ldloc, local);
                    context.FreeLocal(local);
                }
                else
                {
                    gen.Emit(OpCodes.Ldnull);
                }
                break;
        }
    }
    #endregion

    #region Label Emit
    protected virtual void EmitLabel(EmitContext context, ILGenerator gen, LabelExpression label)
    {
        if (label.LabelSymbol != null
            && context.TryGetBuilder<Label>(label.LabelSymbol, out var lab))
        {
            // if label is expecting a type, always preceed it with that type's default value.
            var recievingType = context.GetRuntimeType(label.LabelSymbol.Type);
            if (recievingType != typeof(void))
                EmitDefault(context, gen, recievingType);

            gen.MarkLabel(lab);
        }
    }
    #endregion

    #region Variable Emit
    protected virtual void EmitVariable(EmitContext context, ILGenerator gen, VariableExpression variable)
    {
        if (variable.Variable != null
            && variable.Initializer != null
            && context.TryGetBuilder<LocalBuilder>(variable.Variable, out var local))
        {
            EmitExpressionAsType(context, gen, variable.Initializer, local.LocalType);

            gen.Emit(OpCodes.Dup); // variable expression returns the value too
            gen.Emit(OpCodes.Stloc, local);
        }
    }
    #endregion

    #endregion

    #region Create Types
    protected virtual void CreateTypes(EmitContext context, Symbol symbol)
    {
        switch (symbol)
        {
            case NamespaceSymbol ns:
                foreach (var member in ns.Members)
                {
                    CreateTypes(context, member);
                }
                break;

            case TypeSymbol ts:
                if (context.TryGetBuilder<TypeBuilder>(ts, out var typeBuilder))
                {
                    _ = typeBuilder.CreateType();
                }

                foreach (var member in ts.Members)
                {
                    CreateTypes(context, member);
                }
                break;
        }
    }
    #endregion

    #region EmitContext
    protected class EmitContext
    {
        private RuntimeSymbols _runtimeSymbols;
        
        private readonly Dictionary<Symbol, object> _symbolToBuilderMap =
            new Dictionary<Symbol, object>();

        private readonly Dictionary<Type, List<LocalBuilder>> _localPool =
            new Dictionary<Type, List<LocalBuilder>>();

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

        public LocalBuilder AllocateLocal(Type type, ILGenerator gen)
        {
            if (_localPool.TryGetValue(type, out var freedLocals)
                && freedLocals.Count > 0)
            {
                var local = freedLocals[freedLocals.Count - 1];
                freedLocals.RemoveAt(freedLocals.Count - 1);
                return local;
            }
            else
            {
                return gen.DeclareLocal(type);
            }
        }

        public void FreeLocal(LocalBuilder local)
        {
            if (!_localPool.TryGetValue(local.LocalType, out var freedLocals))
            {
                freedLocals = new List<LocalBuilder>();
                _localPool.Add(local.LocalType, freedLocals);
            }

            freedLocals.Add(local);
        }
    }
    #endregion
}
