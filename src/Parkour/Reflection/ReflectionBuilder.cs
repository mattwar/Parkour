using System.Reflection;
using SRE=System.Reflection.Emit;

namespace Parkour.Reflection;
using Symbols;

/// <summary>
/// A <see cref="Emitting.ModuleBuilder"/> that builds a <see cref="System.Reflection.Emit.ModuleBuilder"/>
/// </summary>
public class ReflectionBuilder : Parkour.Emitting.ModuleBuilder
{
    private readonly SRE.AssemblyBuilder _assemblyBuilder;
    private readonly SRE.ModuleBuilder _moduleBuilder;
    private readonly ReflectionSymbols _runtimeSymbols;
    private readonly List<Diagnostic> _diagnostics;

    private Dictionary<Symbol, object> _symbolToBuilder =
        new Dictionary<Symbol, object>();

    public SRE.AssemblyBuilder Assembly => _assemblyBuilder;
    public SRE.ModuleBuilder Module => _moduleBuilder;

    public ReflectionBuilder(
        ReflectionSymbols reflectionSymbols,
        SRE.AssemblyBuilder assemblyBuilder,
        SRE.ModuleBuilder moduleBuilder)
    {
        _runtimeSymbols = reflectionSymbols;
        _assemblyBuilder = assemblyBuilder;
        _moduleBuilder = moduleBuilder;
        _diagnostics = new List<Diagnostic>();
    }

    public ReflectionBuilder(
        ReflectionSymbols reflectionSymbols,
        SRE.AssemblyBuilder assemblyBuilder,
        string? moduleName = null)
        : this(
              reflectionSymbols,
              assemblyBuilder,
              assemblyBuilder.DefineDynamicModule(
                  moduleName ?? $"Module{assemblyBuilder.GetModules().Length}")
              )
    {
    }

    public ReflectionBuilder(
        ReflectionSymbols reflectionSymbols,
        string assemblyName)
        : this(
              reflectionSymbols,
              SRE.AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(assemblyName),
                SRE.AssemblyBuilderAccess.RunAndCollect))
    {
    }

    public override BuildResult Build()
    {
        // TODO: do these need to be in topographical order?
        var typeBuilders = _symbolToBuilder
            .Where(kvp => kvp.Key is TypeSymbol)
            .Select(kvp => kvp.Value)
            .OfType<SRE.TypeBuilder>()
            .ToList();

        foreach (var typeBuilder in typeBuilders)
        {
            typeBuilder.CreateType();
        }

        _moduleBuilder.CreateGlobalFunctions();

        return new BuildResult(_diagnostics.ToImmutableList());
    }


    public override void DeclareClass(ClassSymbol classSymbol)
    {
        DeclareType(classSymbol);
    }

    public override void DeclareStruct(StructSymbol structSymbol)
    {
        DeclareType(structSymbol);
    }

    public override void DeclareInterface(InterfaceSymbol interfaceSymbol)
    {
        DeclareType(interfaceSymbol);
    }

    private void DeclareType(TypeSymbol typeSymbol)
    {
        var name = typeSymbol.FullName;
        SRE.TypeBuilder typeBuilder;

        if (typeSymbol.DeclaringSymbol is TypeSymbol pts)
        {
            if (_symbolToBuilder.TryGetValue(typeSymbol.DeclaringSymbol, out var pb)
            && pb is SRE.TypeBuilder parentBuilder)
            {
                typeBuilder = parentBuilder.DefineNestedType(name, GetTypeAttributes(typeSymbol));
            }
            else
            {
                _diagnostics.Add(new Diagnostic($"Nested type '{typeSymbol.FullName}' parent '{pts.Name}' not yet declared."));
                return;
            }
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

    public override void DeclareClassBaseType(ClassSymbol classSymbol)
    {
        DeclareTypeBaseType(classSymbol);
    }

    public override void DeclareStructBaseType(StructSymbol structSymbol)
    {
        DeclareTypeBaseType(structSymbol);
    }

    public override void DeclareInterfaceBaseType(InterfaceSymbol interfaceSymbol)
    {
        DeclareTypeBaseType(interfaceSymbol);
    }

    private void DeclareTypeBaseType(TypeSymbol typeSymbol)
    {
        if (_symbolToBuilder.TryGetValue(typeSymbol, out var builder)
            && builder is SRE.TypeBuilder typeBuilder)
        {
            // set base type for type now
            var baseTypeSymbol = typeSymbol.BaseTypes.FirstOrDefault(t => !t.IsInterface);
            if (baseTypeSymbol != null)
            {
                typeBuilder.SetParent(GetRuntimeType(baseTypeSymbol));
            }
        }
    }

    public override void DeclareField(FieldSymbol fieldSymbol)
    {
        if (TryGetDeclaringTypeBuilder(fieldSymbol, out var typeBuilder))
        {
            var fieldType = GetRuntimeType(fieldSymbol.FieldType);
            var fieldAttrs = GetFieldAttributes(fieldSymbol);
            var fieldBuilder = typeBuilder.DefineField(
                fieldSymbol.Name,
                fieldType,
                fieldAttrs);
            _symbolToBuilder.Add(fieldSymbol, fieldBuilder);
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot emit field '{fieldSymbol.FullName}'"));
        }
    }

    public override void DeclareMethod(MethodSymbol methodSymbol)
    {
        if (methodSymbol.DeclaringSymbol is GlobalNamespaceSymbol ns)
        {
            var methodBuilder = _moduleBuilder.DefineGlobalMethod(
                methodSymbol.Name,
                GetMethodAttributes(methodSymbol),
                null,
                null);

            _symbolToBuilder.Add(methodSymbol, methodBuilder);

            // define type parameters
            if (methodSymbol.TypeParameters.Count > 0)
            {
                var typeParameterBuilders = methodBuilder.DefineGenericParameters(
                    methodSymbol.TypeParameters.Select(tp => tp.Name).ToArray());

                for (int i = 0; i < typeParameterBuilders.Length; i++)
                {
                    _symbolToBuilder.Add(methodSymbol.TypeParameters[i], typeParameterBuilders[i]);
                }
            }

            // set return type after defining type parameters
            methodBuilder.SetReturnType(GetRuntimeType(methodSymbol.ReturnType));

            // set parameter types after defining type parametres
            methodBuilder.SetParameters(
                methodSymbol.Parameters
                .Select(p => GetRuntimeType(p.ParameterType))
                .ToArray());

            for (int i = 0; i < methodSymbol.Parameters.Count; i++)
            {
                var parameterSymbol = methodSymbol.Parameters[i];
                var parameterBuilder = methodBuilder.DefineParameter(i, GetParameterAttributes(parameterSymbol), parameterSymbol.Name);
                _symbolToBuilder.Add(parameterSymbol, parameterBuilder);
            }
        }
        else if (TryGetDeclaringTypeBuilder(methodSymbol, out var typeBuilder))
        {
            var methodBuilder = typeBuilder.DefineMethod(
                methodSymbol.Name,
                GetMethodAttributes(methodSymbol));
            _symbolToBuilder.Add(methodSymbol, methodBuilder);

            // define type parameters
            if (methodSymbol.TypeParameters.Count > 0)
            {
                var typeParameterBuilders = methodBuilder.DefineGenericParameters(
                    methodSymbol.TypeParameters.Select(tp => tp.Name).ToArray());

                for (int i = 0; i < typeParameterBuilders.Length; i++)
                {
                    _symbolToBuilder.Add(methodSymbol.TypeParameters[i], typeParameterBuilders[i]);
                }
            }

            // set return type after defining type parameters
            methodBuilder.SetReturnType(GetRuntimeType(methodSymbol.ReturnType));

            // set parameter types after defining type parametres
            methodBuilder.SetParameters(
                methodSymbol.Parameters
                .Select(p => GetRuntimeType(p.ParameterType))
                .ToArray());

            for (int i = 0; i < methodSymbol.Parameters.Count; i++)
            {
                var parameterSymbol = methodSymbol.Parameters[i];
                var parameterBuilder = methodBuilder.DefineParameter(i, GetParameterAttributes(parameterSymbol), parameterSymbol.Name);
                _symbolToBuilder.Add(parameterSymbol, parameterBuilder);
            }
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot emit method '{methodSymbol.FullName}'"));
        }
    }

    public override void DeclareConstructor(ConstructorSymbol constructorSymbol)
    {
        if (TryGetDeclaringTypeBuilder(constructorSymbol, out var typeBuilder))
        {
            var constructorBuilder = typeBuilder.DefineConstructor(
                GetMethodAttributes(constructorSymbol),
                CallingConventions.Standard,
                constructorSymbol.Parameters.Select(p => GetRuntimeType(p.ParameterType)).ToArray()
                );
            _symbolToBuilder.Add(constructorSymbol, constructorBuilder);
            for (int i = 0; i < constructorSymbol.Parameters.Count; i++)
            {
                var parameterSymbol = constructorSymbol.Parameters[i];
                var parameterBuilder = constructorBuilder.DefineParameter(i, GetParameterAttributes(parameterSymbol), parameterSymbol.Name);
                _symbolToBuilder.Add(parameterSymbol, parameterBuilder);
            }
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot emit constructor '{constructorSymbol.FullName}'"));
        }
    }

    public override void DeclareProperty(PropertySymbol propertySymbol)
    {
        if (TryGetDeclaringTypeBuilder(propertySymbol, out var typeBuilder))
        {
            var propertyBuilder = typeBuilder.DefineProperty(
                propertySymbol.Name,
                GetPropertyAttributes(propertySymbol),
                GetRuntimeType(propertySymbol.PropertyType),
                []);
            _symbolToBuilder.Add(propertySymbol, propertyBuilder);
            if (propertySymbol.GetMethod != null
                && _symbolToBuilder.TryGetValue(propertySymbol.GetMethod, out var getMethodBuilder))
            {
                propertyBuilder.SetGetMethod((SRE.MethodBuilder)getMethodBuilder);
            }
            if (propertySymbol.SetMethod != null
                && _symbolToBuilder.TryGetValue(propertySymbol.SetMethod, out var setMethodBuilder))
            {
                propertyBuilder.SetSetMethod((SRE.MethodBuilder)setMethodBuilder);
            }
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot emit property '{propertySymbol.FullName}'"));
        }
    }

    public override void DeclareIndexer(IndexerSymbol indexerSymbol)
    {
        throw new NotImplementedException();
    }

    private bool TryGetDeclaringTypeBuilder(MemberSymbol memberSymbol, out SRE.TypeBuilder typeBuilder)
    {
        if (memberSymbol.DeclaringSymbol is TypeSymbol declaringTypeSymbol
            && _symbolToBuilder.TryGetValue(declaringTypeSymbol, out var builder)
            && builder is SRE.TypeBuilder tb)
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

    public override void BuildMethodBody(MethodSymbol methodSymbol, Action<MethodSymbol, Emitting.BodyBuilder> fnEmitBody)
    {
        if (_symbolToBuilder.TryGetValue(methodSymbol, out var builder)
            && builder is SRE.MethodBuilder methodBuilder)
        {
            fnEmitBody(methodSymbol, new ReflectionILBuilder(this, methodBuilder.GetILGenerator()));
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot emit body for method '{methodSymbol.FullName}'"));
        }
    }

    public override void BuildConstructorBody(ConstructorSymbol constructorSymbol, Action<ConstructorSymbol, Emitting.BodyBuilder> fnEmitBody)
    {
        if (_symbolToBuilder.TryGetValue(constructorSymbol, out var builder)
            && builder is SRE.ConstructorBuilder constructorBuilder)
        {
            fnEmitBody(constructorSymbol, new ReflectionILBuilder(this, constructorBuilder.GetILGenerator()));
        }
        else
        {
            _diagnostics.Add(new Diagnostic($"Cannot emit body for constructor '{constructorSymbol.FullName}'"));
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
        _runtimeSymbols.TryGetRuntimeInfo(symbol, out var info, GetFromBuilders);
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

    private static FieldAttributes GetFieldAttributes(FieldSymbol field)
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

    private static MethodAttributes GetMethodAttributes(MemberSymbol method)
    {
        MethodAttributes attrs = default;

        if ((method.Modifiers & SymbolModifier.Static) != 0)
            attrs |= MethodAttributes.Static;

        if (method.DeclaringSymbol == null
            || method.DeclaringSymbol is NamespaceSymbol)
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

    private static ParameterAttributes GetParameterAttributes(ParameterSymbol parameter)
    {
        return ParameterAttributes.None;
    }

    private static PropertyAttributes GetPropertyAttributes(PropertySymbol property)
    {
        return PropertyAttributes.None;
    }

    /// <summary>
    /// Emits into <see cref="System.Reflection.Emit.ILGenerator"/>
    /// </summary>
    private class ReflectionILBuilder : Emitting.BodyBuilder
    {
        private readonly ReflectionBuilder _builder;
        private readonly SRE.ILGenerator _ilgen;

        private readonly Dictionary<Type, Stack<SRE.LocalBuilder>> _localPool =
            new Dictionary<Type, Stack<SRE.LocalBuilder>>();

        public ReflectionILBuilder(ReflectionBuilder builder, SRE.ILGenerator ilgen)
        {
            _builder = builder;
            _ilgen = ilgen;
        }

        private readonly Dictionary<LabelSymbol, SRE.Label> _labelSymbolToLabelMap =
            new Dictionary<LabelSymbol, SRE.Label>();

        private SRE.Label GetLabel(LabelSymbol labelSymbol)
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

        private readonly Dictionary<VariableSymbol, SRE.LocalBuilder> _variableToLocalMap =
            new Dictionary<VariableSymbol, SRE.LocalBuilder>();

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

        private SRE.LocalBuilder GetLocal(VariableSymbol variable)
        {
            if (!_variableToLocalMap.TryGetValue(variable, out var local))
            {
                var variableType = _builder.GetRuntimeType(variable.VariableType);
                local = AllocateLocal(variableType);
                _variableToLocalMap.Add(variable, local);
            }

            return local;
        }

        private SRE.LocalBuilder AllocateLocal(Type type)
        {
            if (_localPool.TryGetValue(type, out var localStack)
                && localStack.Count > 0)
            {
                return localStack.Pop();

            }

            return _ilgen.DeclareLocal(type);
        }

        private void FreeLocal(SRE.LocalBuilder local)
        {
            if (!_localPool.TryGetValue(local.LocalType, out var localStack))
            {
                localStack = new Stack<SRE.LocalBuilder>();
                _localPool.Add(local.LocalType, localStack);
            }

            localStack.Push(local);
        }

        public override void EmitDup()
        {
            _ilgen.Emit(SRE.OpCodes.Dup);
        }

        public override void EmitPop()
        {
            _ilgen.Emit(SRE.OpCodes.Pop);
        }

        public override void EmitReturn()
        {
            _ilgen.Emit(SRE.OpCodes.Ret);
        }

        public override void EmitBranch(LabelSymbol labelSymbol)
        {
            _ilgen.Emit(SRE.OpCodes.Br, GetLabel(labelSymbol));
        }

        public override void EmitBranchTrue(LabelSymbol labelSymbol)
        {
            _ilgen.Emit(SRE.OpCodes.Brtrue, GetLabel(labelSymbol));
        }

        public override void EmitBranchFalse(LabelSymbol labelSymbol)
        {
            _ilgen.Emit(SRE.OpCodes.Brfalse, GetLabel(labelSymbol));
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
                    _ilgen.Emit(SRE.OpCodes.Ldarg_0);
                    break;
                case 1:
                    _ilgen.Emit(SRE.OpCodes.Ldarg_1);
                    break;
                default:
                    _ilgen.Emit(SRE.OpCodes.Ldarg, n);
                    break;
            }
        }

        private void EmitLoadArgAddress(int n)
        {
            if (n >= 0 && n < 256)
            {
                _ilgen.Emit(SRE.OpCodes.Ldarga_S, (byte)n);
            }
            else
            {
                _ilgen.Emit(SRE.OpCodes.Ldarga, n);
            }
        }

        private void EmitStoreArg(int n)
        {
            if (n >= 0 && n < 256)
            {
                _ilgen.Emit(SRE.OpCodes.Starg_S, (byte)n);
            }
            else
            {
                _ilgen.Emit(SRE.OpCodes.Starg, n);
            }
        }

        public override void EmitLoadArrayElement(TypeSymbol elementTypeSymbol)
        {
            var type = _builder.GetRuntimeType(elementTypeSymbol);

            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.SByte:
                    _ilgen.Emit(SRE.OpCodes.Ldelem_I1);
                    break;
                case TypeCode.Byte:
                    _ilgen.Emit(SRE.OpCodes.Ldelem_U1);
                    break;
                case TypeCode.Int16:
                    _ilgen.Emit(SRE.OpCodes.Ldelem_I2);
                    break;
                case TypeCode.UInt16:
                    _ilgen.Emit(SRE.OpCodes.Ldelem_U2);
                    break;
                case TypeCode.Int32:
                    _ilgen.Emit(SRE.OpCodes.Ldelem_I4);
                    break;
                case TypeCode.UInt32:
                    _ilgen.Emit(SRE.OpCodes.Ldelem_U4);
                    break;
                case TypeCode.Int64:
                    _ilgen.Emit(SRE.OpCodes.Ldelem_I8);
                    break;
                case TypeCode.Single:
                    _ilgen.Emit(SRE.OpCodes.Ldelem_R4);
                    break;
                case TypeCode.Double:
                    _ilgen.Emit(SRE.OpCodes.Ldelem_R8);
                    break;
                default:
                    if (type == typeof(nint))
                    {
                        _ilgen.Emit(SRE.OpCodes.Ldelem_I);
                    }
                    else
                    {
                        _ilgen.Emit(SRE.OpCodes.Ldelem, type);
                    }
                    break;
            }

            // OpCodes.Ldelem_Ref  == typeof(object)?
        }

        public override void EmitLoadArrayElementAddress(TypeSymbol elementTypeSymbol)
        {
            _ilgen.Emit(SRE.OpCodes.Ldelema);
        }

        public override void EmitStoreArrayElement(TypeSymbol elementTypeSymbol)
        {
            var type = _builder.GetRuntimeType(elementTypeSymbol);

            var typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.SByte:
                    _ilgen.Emit(SRE.OpCodes.Stelem_I1);
                    break;
                case TypeCode.Int16:
                    _ilgen.Emit(SRE.OpCodes.Stelem_I2);
                    break;
                case TypeCode.Int32:
                    _ilgen.Emit(SRE.OpCodes.Stelem_I4);
                    break;
                case TypeCode.Int64:
                    _ilgen.Emit(SRE.OpCodes.Stelem_I8);
                    break;
                case TypeCode.Single:
                    _ilgen.Emit(SRE.OpCodes.Stelem_R4);
                    break;
                case TypeCode.Double:
                    _ilgen.Emit(SRE.OpCodes.Stelem_R8);
                    break;
                default:
                    if (type == typeof(nint))
                    {
                        _ilgen.Emit(SRE.OpCodes.Stelem_I);
                    }
                    else
                    {
                        _ilgen.Emit(SRE.OpCodes.Stelem, type);
                    }
                    break;
            }

            // OpCodes.Stelem_Ref
        }

        public override void EmitLoadField(FieldSymbol field)
        {
            var fi = _builder.GetRuntimeInfo<FieldInfo>(field);
            _ilgen.Emit(SRE.OpCodes.Ldfld, fi);
        }

        public override void EmitLoadFieldAddress(FieldSymbol field)
        {
            var fi = _builder.GetRuntimeInfo<FieldInfo>(field);
            _ilgen.Emit(SRE.OpCodes.Ldflda, fi);
        }

        public override void EmitStoreField(FieldSymbol field)
        {
            var fi = _builder.GetRuntimeInfo<FieldInfo>(field);
            _ilgen.Emit(SRE.OpCodes.Stfld, fi);
        }

        public override void EmitLoadVariable(VariableSymbol variable)
        {
            var loc = GetLocal(variable);
            _ilgen.Emit(SRE.OpCodes.Ldloc, loc);
        }

        public override void EmitLoadVariableAddress(VariableSymbol variable)
        {
            var loc = GetLocal(variable);
            _ilgen.Emit(SRE.OpCodes.Ldloca, loc);
        }

        public override void EmitStoreVariable(VariableSymbol variable)
        {
            var loc = GetLocal(variable);
            _ilgen.Emit(SRE.OpCodes.Stloc, loc);
        }

        public override void EmitLoadNull()
        {
            _ilgen.Emit(SRE.OpCodes.Ldnull);
        }

        public override void EmitLoadBool(bool value)
        {
            _ilgen.Emit(value ? SRE.OpCodes.Ldc_I4_1 : SRE.OpCodes.Ldc_I4_0);
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
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_8);
                    break;
                case -1:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_M1);
                    break;
                default:
                    if (value >= 0 && value < 256)
                    {
                        _ilgen.Emit(SRE.OpCodes.Ldc_I4_S, (byte)value);
                    }
                    else
                    {
                        _ilgen.Emit(SRE.OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }

        public override void EmitLoadInt64(long value)
        {
            _ilgen.Emit(SRE.OpCodes.Ldc_I8, value);
        }

        public override void EmitLoadUInt64(ulong value)
        {
            EmitLoadInt64(unchecked((long)value));
        }

        public override void EmitLoadSingle(float value)
        {
            _ilgen.Emit(SRE.OpCodes.Ldc_R4, value);
        }

        public override void EmitLoadDouble(double value)
        {
            _ilgen.Emit(SRE.OpCodes.Ldc_R8, value);
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
            _ilgen.Emit(SRE.OpCodes.Call, Decimal_Constructor);
        }

        private static ConstructorInfo Decimal_Constructor =
           typeof(decimal).GetConstructor([typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte)])!;

        public override void EmitLoadString(string value)
        {
            _ilgen.Emit(SRE.OpCodes.Ldstr, value);
        }

        public override void EmitLoadChar(char value)
        {
            EmitLoadUInt16(value);
        }

        public override void EmitLoadMethod(MethodSymbol methodSymbol)
        {
            var info = _builder.GetRuntimeInfo<MethodInfo>(methodSymbol);
            _ilgen.Emit(SRE.OpCodes.Ldftn, info);
        }

        public override void EmitLoadToken(MemberSymbol symbol)
        {
            var info = _builder.GetRuntimeInfo(symbol);
            switch (info)
            {
                case MethodInfo mi:
                    _ilgen.Emit(SRE.OpCodes.Ldtoken, mi);
                    break;
                case FieldInfo fi:
                    _ilgen.Emit(SRE.OpCodes.Ldtoken, fi);
                    break;
                case Type type:
                    _ilgen.Emit(SRE.OpCodes.Ldtoken, type);
                    break;
            }
        }

        private static FieldInfo DateTime_Default =
            typeof(DateTime).GetField("MinValue", BindingFlags.Static | BindingFlags.Public)!;

        private static FieldInfo Decimal_Default =
            typeof(decimal).GetField("Zero", BindingFlags.Static | BindingFlags.Public)!;

        public override void EmitDefault(TypeSymbol typeSymbol)
        {
            var type = _builder.GetRuntimeType(typeSymbol);
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
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_0);
                    break;

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    _ilgen.Emit(SRE.OpCodes.Ldc_I4_0);
                    _ilgen.Emit(SRE.OpCodes.Conv_I8);
                    break;

                case TypeCode.Single:
                    _ilgen.Emit(SRE.OpCodes.Ldc_R4, 0.0f);
                    break;

                case TypeCode.Double:
                    _ilgen.Emit(SRE.OpCodes.Ldc_R8, 0.0);
                    break;

                case TypeCode.Decimal:
                    _ilgen.Emit(SRE.OpCodes.Ldsfld, Decimal_Default);
                    break;

                case TypeCode.DateTime:
                    _ilgen.Emit(SRE.OpCodes.Ldsfld, DateTime_Default);
                    break;

                default:
                    if (type.IsValueType)
                    {
                        var local = AllocateLocal(type);
                        _ilgen.Emit(SRE.OpCodes.Ldloca, local);
                        _ilgen.Emit(SRE.OpCodes.Initobj, type);
                        _ilgen.Emit(SRE.OpCodes.Ldloc, local);
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
            var method = _builder.GetRuntimeInfo<MethodInfo>(methodSymbol);

            var instanceIsValueType = (method.DeclaringType != null && method.DeclaringType.IsValueType);
            var op = method.IsStatic || instanceIsValueType
                ? SRE.OpCodes.Call
                : SRE.OpCodes.Callvirt;

            _ilgen.Emit(op, method);
        }

        public override void EmitCall(ConstructorSymbol constructorSymbol)
        {
            var info = _builder.GetRuntimeInfo<ConstructorInfo>(constructorSymbol);
            _ilgen.Emit(SRE.OpCodes.Call, info);
        }

        public override void EmitNew(ConstructorSymbol constructorSymbol)
        {
            var info = _builder.GetRuntimeInfo<ConstructorInfo>(constructorSymbol);
            _ilgen.Emit(SRE.OpCodes.Newobj, info);
        }

        public override void EmitNewArray(TypeSymbol elementTypeSymbol)
        {
            var info = _builder.GetRuntimeType(elementTypeSymbol);
            _ilgen.Emit(SRE.OpCodes.Newarr, info);
        }

        public override void EmitInit(TypeSymbol typeSymbol)
        {
            var type = _builder.GetRuntimeType(typeSymbol);
            var local = AllocateLocal(type);
            _ilgen.Emit(SRE.OpCodes.Ldloca, local);
            _ilgen.Emit(SRE.OpCodes.Initobj, type);
            _ilgen.Emit(SRE.OpCodes.Ldloc, local);
            FreeLocal(local);
        }

        public override void EmitConvert(TypeSymbol sourceTypeSymbol, TypeSymbol targetTypeSymbol, bool isChecked)
        {
            var sourceType = _builder.GetRuntimeType(sourceTypeSymbol);
            var targetType = _builder.GetRuntimeType(targetTypeSymbol);

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
                    _ilgen.Emit(SRE.OpCodes.Box, sourceType);
                }
                return;
            }
            else if (sourceType == typeof(object))
            {
                if (targetType.IsValueType)
                {
                    _ilgen.Emit(SRE.OpCodes.Unbox_Any, targetType);
                }
                else
                {
                    _ilgen.Emit(SRE.OpCodes.Castclass, targetType);
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
                _ilgen.Emit(SRE.OpCodes.Castclass, targetType);
            }
            else if (targetType.IsInterface && sourceType.IsAssignableTo(targetType))
            {
                if (sourceType.IsValueType)
                {
                    _ilgen.Emit(SRE.OpCodes.Box, sourceType);
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
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_I1_Un : SRE.OpCodes.Conv_I1);
                    break;

                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_I1 : SRE.OpCodes.Conv_I1);
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
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_U1_Un : SRE.OpCodes.Conv_U1);
                    break;

                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_U1 : SRE.OpCodes.Conv_U1);
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
                    _ilgen.Emit(SRE.OpCodes.Conv_I2);
                    break;

                case TypeCode.Int16:
                    break;

                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_I2_Un : SRE.OpCodes.Conv_I2);
                    break;

                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_I2 : SRE.OpCodes.Conv_I2);
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
                    _ilgen.Emit(SRE.OpCodes.Conv_U2);
                    break;

                case TypeCode.UInt16:
                    break;

                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_U2_Un : SRE.OpCodes.Conv_U2);
                    break;

                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_U2 : SRE.OpCodes.Conv_U2);
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
                    _ilgen.Emit(SRE.OpCodes.Conv_I4);
                    break;

                case TypeCode.Int32:
                    break;

                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_I4_Un : SRE.OpCodes.Conv_I4);
                    break;

                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_I4 : SRE.OpCodes.Conv_I4);
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
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_U4 : SRE.OpCodes.Conv_U4);
                    break;

                case TypeCode.Byte:
                case TypeCode.UInt16:
                    _ilgen.Emit(SRE.OpCodes.Conv_U4);
                    break;

                case TypeCode.UInt32:
                    break;

                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_U4_Un : SRE.OpCodes.Conv_U4);
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
                    _ilgen.Emit(SRE.OpCodes.Conv_I8);
                    break;

                case TypeCode.UInt64:
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_I8_Un : SRE.OpCodes.Conv_I8);
                    break;

                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    _ilgen.Emit(SRE.OpCodes.Conv_Ovf_I8);
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
                    _ilgen.Emit(isChecked ? SRE.OpCodes.Conv_Ovf_U8 : SRE.OpCodes.Conv_U8);
                    break;

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                    _ilgen.Emit(SRE.OpCodes.Conv_U8);
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
                    _ilgen.Emit(SRE.OpCodes.Conv_R4);
                    break;

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    _ilgen.Emit(SRE.OpCodes.Conv_R_Un);
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
                    _ilgen.Emit(SRE.OpCodes.Conv_R8);
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

        public override void EmitAdd(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? SRE.OpCodes.Add
                : IsUnsigned(operandType) ? SRE.OpCodes.Add_Ovf_Un
                : SRE.OpCodes.Add_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitSubtract(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? SRE.OpCodes.Sub
                : IsUnsigned(operandType) ? SRE.OpCodes.Sub_Ovf_Un
                : SRE.OpCodes.Sub_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitMultiply(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? SRE.OpCodes.Mul
                : IsUnsigned(operandType) ? SRE.OpCodes.Mul_Ovf_Un
                : SRE.OpCodes.Mul_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitDivide(TypeSymbol operandTypeSymbol)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            var op = IsUnsigned(operandType) ? SRE.OpCodes.Div_Un : SRE.OpCodes.Div;
            _ilgen.Emit(op);
        }

        public override void EmitRemainder(TypeSymbol operandTypeSymbol)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            var op = IsUnsigned(operandType) ? SRE.OpCodes.Rem_Un : SRE.OpCodes.Rem;
            _ilgen.Emit(op);
        }

        public override void EmitNegate(TypeSymbol operandTypeSymbol)
        {
            _ilgen.Emit(SRE.OpCodes.Neg);
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
            _ilgen.Emit(SRE.OpCodes.And);
        }

        public override void EmitOr()
        {
            _ilgen.Emit(SRE.OpCodes.Or);
        }

        public override void EmitXor()
        {
            _ilgen.Emit(SRE.OpCodes.Xor);
        }

        public override void EmitNot()
        {
            _ilgen.Emit(SRE.OpCodes.Not);
        }

        public override void EmitShiftLeft(TypeSymbol operandTypeSymbol)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            var mask = (operandType == typeof(long) || operandType == typeof(ulong)) ? 0x3F : 0x1F;
            EmitLoadInt32(mask);
            _ilgen.Emit(SRE.OpCodes.And);
            _ilgen.Emit(SRE.OpCodes.Shl);
        }

        public override void EmitShiftRight(TypeSymbol operandTypeSymbol)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            var mask = (operandType == typeof(long) || operandType == typeof(ulong)) ? 0x3F : 0x1F;
            EmitLoadInt32(mask);
            _ilgen.Emit(SRE.OpCodes.And);
            _ilgen.Emit(IsUnsigned(operandType) ? SRE.OpCodes.Shr_Un : SRE.OpCodes.Shr);
        }

        public override void EmitEqual(TypeSymbol operandTypeSymbol)
        {
            _ilgen.Emit(SRE.OpCodes.Ceq);
        }

        public override void EmitNotEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            if (operandType == typeof(bool))
            {
                _ilgen.Emit(SRE.OpCodes.Xor);
            }
            else
            {
                // OMG
                _ilgen.Emit(SRE.OpCodes.Ceq);
                _ilgen.Emit(SRE.OpCodes.Ldc_I4_0);
                _ilgen.Emit(SRE.OpCodes.Ceq);
            }
        }

        public override void EmitLessThan(TypeSymbol operandTypeSymbol)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) ? SRE.OpCodes.Clt_Un : SRE.OpCodes.Clt);
        }

        public override void EmitLessThanOrEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) || IsFloatingPoint(operandType) ? SRE.OpCodes.Cgt_Un : SRE.OpCodes.Cgt);
            _ilgen.Emit(SRE.OpCodes.Ldc_I4_0);
            _ilgen.Emit(SRE.OpCodes.Ceq);
        }

        public override void EmitGreaterThan(TypeSymbol operandTypeSymbol)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) ? SRE.OpCodes.Cgt_Un : SRE.OpCodes.Cgt);
        }

        public override void EmitGreaterThanOrEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _builder.GetRuntimeType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) || IsFloatingPoint(operandType) ? SRE.OpCodes.Clt_Un : SRE.OpCodes.Clt);
            _ilgen.Emit(SRE.OpCodes.Ldc_I4_0);
            _ilgen.Emit(SRE.OpCodes.Ceq);
        }

        public override void EmitThrow(string message)
        {
            EmitThrow(typeof(InvalidOperationException), message);
        }

        public override void EmitThrowAndReport(Diagnostic diagnostic)
        {
            EmitThrow(diagnostic.ToString());
            _builder._diagnostics.Add(diagnostic);
        }

        public override void EmitThrow(TypeSymbol exceptionTypeSymbol, string message)
        {
            var exceptionType = _builder.GetRuntimeType(exceptionTypeSymbol);
            EmitThrow(exceptionType, message);
        }

        private void EmitThrow(Type exceptionType, string message)
        {
            _ilgen.Emit(SRE.OpCodes.Ldstr, message);
            var ci = exceptionType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(string)]);
            _ilgen.Emit(SRE.OpCodes.Newobj, ci!);
            _ilgen.ThrowException(typeof(InvalidOperationException));
        }
    }
}