using System.Reflection;
using System.Reflection.Emit;

namespace Parkour.Reflection;

using Symbols;

public partial class ReflectionEmitter
{
    /// <summary>
    /// Emits into <see cref="System.Reflection.Emit.ILGenerator"/>
    /// </summary>
    private class ILEmitter : Semantics.ILEmitter
    {
        private readonly ReflectionEmitter _emitter;
        private readonly ILGenerator _ilgen;

        private readonly Dictionary<Type, Stack<LocalBuilder>> _localPool =
            new Dictionary<Type, Stack<LocalBuilder>>();

        public ILEmitter(ReflectionEmitter builder, ILGenerator ilgen)
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
                var variableType = _emitter.GetReflectionType(variable.Type);
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
            var type = _emitter.GetReflectionType(elementTypeSymbol);

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
            var type = _emitter.GetReflectionType(elementTypeSymbol);

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
            var fi = _emitter.GetReflectionInfo<FieldInfo>(field);
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
            var fi = _emitter.GetReflectionInfo<FieldInfo>(field);
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
            var fi = _emitter.GetReflectionInfo<FieldInfo>(field);
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
            var info = _emitter.GetReflectionInfo<MethodInfo>(methodSymbol);
            _ilgen.Emit(OpCodes.Ldftn, info);
        }

        public override void EmitLoadToken(MemberSymbol symbol)
        {
            var info = _emitter.GetReflectionInfo(symbol);
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
            var type = _emitter.GetReflectionType(typeSymbol);
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
            var method = _emitter.GetReflectionInfo<MethodInfo>(methodSymbol);

            var instanceIsValueType = (method.DeclaringType != null && method.DeclaringType.IsValueType);
            var op = method.IsStatic || instanceIsValueType
                ? OpCodes.Call
                : OpCodes.Callvirt;

            _ilgen.Emit(op, method);
        }

        public override void EmitCall(ConstructorSymbol constructorSymbol)
        {
            var info = _emitter.GetReflectionInfo<ConstructorInfo>(constructorSymbol);
            _ilgen.Emit(OpCodes.Call, info);
        }

        public override void EmitNew(ConstructorSymbol constructorSymbol)
        {
            var info = _emitter.GetReflectionInfo<ConstructorInfo>(constructorSymbol);
            _ilgen.Emit(OpCodes.Newobj, info);
        }

        public override void EmitNewSZArray(TypeSymbol elementTypeSymbol)
        {
            var info = _emitter.GetReflectionType(elementTypeSymbol);
            _ilgen.Emit(OpCodes.Newarr, info);
        }

        public override void EmitInit(TypeSymbol typeSymbol)
        {
            var type = _emitter.GetReflectionType(typeSymbol);
            var local = AllocateLocal(type);
            _ilgen.Emit(OpCodes.Ldloca, local);
            _ilgen.Emit(OpCodes.Initobj, type);
            _ilgen.Emit(OpCodes.Ldloc, local);
            FreeLocal(local);
        }

        public override void EmitConvert(TypeSymbol sourceTypeSymbol, TypeSymbol targetTypeSymbol, bool isChecked)
        {
            var sourceType = _emitter.GetReflectionType(sourceTypeSymbol);
            var targetType = _emitter.GetReflectionType(targetTypeSymbol);

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
            var instanceType = _emitter.GetReflectionType(instanceTypeSymbol);
            _ilgen.Emit(OpCodes.Isinst, instanceType);
        }

        public override void EmitAdd(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? OpCodes.Add
                : IsUnsigned(operandType) ? OpCodes.Add_Ovf_Un
                : OpCodes.Add_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitSubtract(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? OpCodes.Sub
                : IsUnsigned(operandType) ? OpCodes.Sub_Ovf_Un
                : OpCodes.Sub_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitMultiply(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? OpCodes.Mul
                : IsUnsigned(operandType) ? OpCodes.Mul_Ovf_Un
                : OpCodes.Mul_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitDivide(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
            var op = IsUnsigned(operandType) ? OpCodes.Div_Un : OpCodes.Div;
            _ilgen.Emit(op);
        }

        public override void EmitRemainder(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
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
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
            var mask = (operandType == typeof(long) || operandType == typeof(ulong)) ? 0x3F : 0x1F;
            EmitLoadInt32(mask);
            _ilgen.Emit(OpCodes.And);
            _ilgen.Emit(OpCodes.Shl);
        }

        public override void EmitShiftRight(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
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
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
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
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) ? OpCodes.Clt_Un : OpCodes.Clt);
        }

        public override void EmitLessThanOrEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) || IsFloatingPoint(operandType) ? OpCodes.Cgt_Un : OpCodes.Cgt);
            _ilgen.Emit(OpCodes.Ldc_I4_0);
            _ilgen.Emit(OpCodes.Ceq);
        }

        public override void EmitGreaterThan(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) ? OpCodes.Cgt_Un : OpCodes.Cgt);
        }

        public override void EmitGreaterThanOrEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetReflectionType(operandTypeSymbol);
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
            var exceptionType = _emitter.GetReflectionType(exceptionTypeSymbol);
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