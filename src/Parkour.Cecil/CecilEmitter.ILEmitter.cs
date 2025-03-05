using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Parkour.Cecil;

using Symbols;

public partial class CecilEmitter
{
    /// <summary>
    /// Emits into <see cref="Mono.Cecil.Cil.ILProcessor"/>
    /// </summary>
    private class ILEmitter : Semantics.ILEmitter
    {
        private readonly CecilEmitter _emitter;
        private readonly CecilSymbols _externalSymbols;
        private readonly MethodBody _body;
        private readonly List<Diagnostic> _diagnostics;
        private readonly ILProcessor _ilgen;
        private readonly Dictionary<TypeReference, Stack<VariableDefinition>> _variablePool;

        public ILEmitter(
            CecilEmitter emitter, 
            CecilSymbols externalSymbols, 
            MethodBody body,
            List<Diagnostic> diagnostics)
        {
            _emitter = emitter;
            _externalSymbols = externalSymbols;
            _body = body;
            _diagnostics = diagnostics;
            _ilgen = body.GetILProcessor();
            _variablePool = new Dictionary<TypeReference, Stack<VariableDefinition>>(externalSymbols.TypeReferenceComparer);
        }

        public override SymbolTable ExternalSymbols =>
            _externalSymbols;

        private readonly Dictionary<LabelSymbol, Instruction> _labelSymbolToInstructionMap =
            new Dictionary<LabelSymbol, Instruction>();

        private Instruction GetLabel(LabelSymbol labelSymbol)
        {
            if (!_labelSymbolToInstructionMap.TryGetValue(labelSymbol, out var label))
            {
                label = _ilgen.Create(OpCodes.Nop);
                _labelSymbolToInstructionMap.Add(labelSymbol, label);
            }

            return label;
        }

        public override void MarkLabel(LabelSymbol labelSymbol)
        {
            var instruction = GetLabel(labelSymbol);
            _ilgen.Append(instruction);
        }

        private readonly Dictionary<VariableSymbol, VariableDefinition> _variableToLocalMap =
            new Dictionary<VariableSymbol, VariableDefinition>();

        public override void DeclareVariableStart(VariableSymbol variable)
        {
            _ = GetLocal(variable);
        }

        public override void DeclareVariableEnd(VariableSymbol variable)
        {
            if (_variableToLocalMap.TryGetValue(variable, out var vd))
            {
                _variableToLocalMap.Remove(variable);
                FreeVariable(vd);
            }
        }

        private VariableDefinition GetLocal(VariableSymbol variable)
        {
            if (!_variableToLocalMap.TryGetValue(variable, out var local))
            {
                var variableType = _emitter.GetCecilType(variable.Type);
                local = AllocateVariable(variableType);
                _variableToLocalMap.Add(variable, local);
            }

            return local;
        }

        private VariableDefinition AllocateVariable(TypeReference type)
        {
            if (_variablePool.TryGetValue(type, out var variableStack)
                && variableStack.Count > 0)
            {
                return variableStack.Pop();
            }

            var variable = new VariableDefinition(type);
            _body.Variables.Add(variable);
            return variable;
        }

        private void FreeVariable(VariableDefinition variable)
        {
            if (!_variablePool.TryGetValue(variable.VariableType, out var variableStack))
            {
                variableStack = new Stack<VariableDefinition>();
                _variablePool.Add(variable.VariableType, variableStack);
            }

            variableStack.Push(variable);
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
            var type = _emitter.GetCecilType(elementTypeSymbol);

            switch (type.MetadataType)
            {
                case MetadataType.SByte:
                    _ilgen.Emit(OpCodes.Ldelem_I1);
                    break;
                case MetadataType.Byte:
                    _ilgen.Emit(OpCodes.Ldelem_U1);
                    break;
                case MetadataType.Int16:
                    _ilgen.Emit(OpCodes.Ldelem_I2);
                    break;
                case MetadataType.UInt16:
                    _ilgen.Emit(OpCodes.Ldelem_U2);
                    break;
                case MetadataType.Int32:
                    _ilgen.Emit(OpCodes.Ldelem_I4);
                    break;
                case MetadataType.UInt32:
                    _ilgen.Emit(OpCodes.Ldelem_U4);
                    break;
                case MetadataType.Int64:
                    _ilgen.Emit(OpCodes.Ldelem_I8);
                    break;
                case MetadataType.Single:
                    _ilgen.Emit(OpCodes.Ldelem_R4);
                    break;
                case MetadataType.Double:
                    _ilgen.Emit(OpCodes.Ldelem_R8);
                    break;
                default:
                    if (!type.IsValueType)
                    {
                        _ilgen.Emit(OpCodes.Ldelem_Ref);
                    }
                    else if (type.FullName == typeof(nint).FullName)
                    {
                        _ilgen.Emit(OpCodes.Ldelem_I);
                    }
                    else
                    {
                        _ilgen.Emit(OpCodes.Ldelem_Any, type);
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
            var type = _emitter.GetCecilType(elementTypeSymbol);
            switch (type.MetadataType)
            {
                case MetadataType.SByte:
                case MetadataType.Byte:
                    _ilgen.Emit(OpCodes.Stelem_I1);
                    break;
                case MetadataType.Int16:
                case MetadataType.UInt16:
                    _ilgen.Emit(OpCodes.Stelem_I2);
                    break;
                case MetadataType.Int32:
                case MetadataType.UInt32:
                    _ilgen.Emit(OpCodes.Stelem_I4);
                    break;
                case MetadataType.Int64:
                case MetadataType.UInt64:
                    _ilgen.Emit(OpCodes.Stelem_I8);
                    break;
                case MetadataType.Single:
                    _ilgen.Emit(OpCodes.Stelem_R4);
                    break;
                case MetadataType.Double:
                    _ilgen.Emit(OpCodes.Stelem_R8);
                    break;
                default:
                    if (!type.IsValueType)
                    {
                        _ilgen.Emit(OpCodes.Stelem_Ref);
                    }
                    else if (type.FullName == typeof(nint).FullName)
                    {
                        _ilgen.Emit(OpCodes.Stelem_I);
                    }
                    else
                    {
                        _ilgen.Emit(OpCodes.Stelem_Any, type);
                    }
                    break;
            }
        }

        public override void EmitLoadField(FieldSymbol field)
        {
            var fi = _emitter.GetCecilReference<FieldReference>(field);
            if (field.IsStatic)
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
            var fi = _emitter.GetCecilReference<FieldReference>(field);
            if (field.IsStatic)
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
            var fi = _emitter.GetCecilReference<FieldReference>(field);
            if (field.IsStatic)
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
                    if (value >= -127 && value < 128)
                    {
                        _ilgen.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
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

        private MethodDefinition Decimal_Constructor =>
           _lazyDecimalConstructor ??= _externalSymbols.CecilDecimal
                .Methods.First(m => m.IsConstructor && m.Parameters.Count == 5);
        private MethodDefinition? _lazyDecimalConstructor;
        //typeof(decimal).GetConstructor([typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte)])!;

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
            var method = _emitter.GetCecilReference<MethodReference>(methodSymbol);
            _ilgen.Emit(OpCodes.Ldftn, method);
        }

        public override void EmitLoadToken(MemberSymbol symbol)
        {
            var member = _emitter.GetCecilReference(symbol);
            switch (member)
            {
                case MethodReference method:
                    _ilgen.Emit(OpCodes.Ldtoken, method);
                    break;
                case FieldReference field:
                    _ilgen.Emit(OpCodes.Ldtoken, field);
                    break;
                case TypeReference type:
                    _ilgen.Emit(OpCodes.Ldtoken, type);
                    break;
            }
        }

        private FieldReference DateTime_Default => 
            _externalSymbols.CecilDateTime.Fields.First(f => f.Name == "MinValue");

        private FieldReference Decimal_Default =>
            _externalSymbols.CecilDecimal.Fields.First(f => f.Name == "Zero");

        public override void EmitDefault(TypeSymbol typeSymbol)
        {
            var type = _emitter.GetCecilType(typeSymbol);
            EmitDefault(type);
        }

        private void EmitDefault(TypeReference type)
        {
            switch (type.MetadataType)
            {
                case MetadataType.Boolean:
                case MetadataType.Char:
                case MetadataType.SByte:
                case MetadataType.Byte:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                case MetadataType.Int32:
                case MetadataType.UInt32:
                    _ilgen.Emit(OpCodes.Ldc_I4_0);
                    break;

                case MetadataType.Int64:
                case MetadataType.UInt64:
                    _ilgen.Emit(OpCodes.Ldc_I4_0);
                    _ilgen.Emit(OpCodes.Conv_I8);
                    break;

                case MetadataType.Single:
                    _ilgen.Emit(OpCodes.Ldc_R4, 0.0f);
                    break;

                case MetadataType.Double:
                    _ilgen.Emit(OpCodes.Ldc_R8, 0.0);
                    break;

                default:
                    if (type.FullName == "System.Decimal")
                    {
                        _ilgen.Emit(OpCodes.Ldsfld, Decimal_Default);
                    }
                    else if (type.FullName == "System.DateTime")
                    {
                        _ilgen.Emit(OpCodes.Ldsfld, DateTime_Default);
                    }
                    if (type.IsValueType)
                    {
                        var variable = AllocateVariable(type);
                        _ilgen.Emit(OpCodes.Ldloca, variable);
                        _ilgen.Emit(OpCodes.Initobj, type);
                        _ilgen.Emit(OpCodes.Ldloc, variable);
                        FreeVariable(variable);
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
            var method = _emitter.GetCecilReference<MethodReference>(methodSymbol);

            var instanceIsValueType = (method.DeclaringType != null && method.DeclaringType.IsValueType);
            var op = methodSymbol.IsStatic || instanceIsValueType
                ? OpCodes.Call
                : OpCodes.Callvirt;

            _ilgen.Emit(op, method);
        }

        public override void EmitCall(ConstructorSymbol constructorSymbol)
        {
            var constructor = _emitter.GetCecilReference<MethodReference>(constructorSymbol);
            _ilgen.Emit(OpCodes.Call, constructor);
        }

        public override void EmitNew(ConstructorSymbol constructorSymbol)
        {
            var constructor = _emitter.GetCecilReference<MethodReference>(constructorSymbol);
            _ilgen.Emit(OpCodes.Newobj, constructor);
        }

        public override void EmitNewSZArray(TypeSymbol elementTypeSymbol)
        {
            var type = _emitter.GetCecilType(elementTypeSymbol);
            _ilgen.Emit(OpCodes.Newarr, type);
        }

        public override void EmitInit(TypeSymbol typeSymbol)
        {
            var type = _emitter.GetCecilType(typeSymbol);
            var variable = AllocateVariable(type);
            _ilgen.Emit(OpCodes.Ldloca, variable);
            _ilgen.Emit(OpCodes.Initobj, type);
            _ilgen.Emit(OpCodes.Ldloc, variable);
            FreeVariable(variable);
        }

        public override void EmitConvert(TypeSymbol sourceTypeSymbol, TypeSymbol targetTypeSymbol, bool isChecked)
        {
            var sourceType = _emitter.GetCecilType(sourceTypeSymbol);
            var targetType = _emitter.GetCecilType(targetTypeSymbol);

            var comparer = _externalSymbols.TypeReferenceComparer;

            if (comparer.Equals(sourceType, targetType))
            {
                // do nothing since same type
                return;
            }
            else if (comparer.Equals(targetType, _externalSymbols.CecilVoid))
            {
                // target does not expect a type and expression type is not void.
                EmitPop();
                return;
            }
            else if (comparer.Equals(sourceType, _externalSymbols.CecilVoid))
            {
                // source has no type (so no value was left on stack), but target expects a type (not void)
                EmitDefault(targetTypeSymbol);
                return;
            }
            else if (comparer.Equals(targetType, _externalSymbols.CecilObject))
            {
                if (sourceType.IsValueType)
                {
                    _ilgen.Emit(OpCodes.Box, sourceType);
                }
                return;
            }
            else if (comparer.Equals(sourceType, _externalSymbols.CecilObject))
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
            else if (!targetTypeSymbol.IsInterface 
                && !targetTypeSymbol.IsValueType 
                && !sourceTypeSymbol.IsValueType
                && sourceTypeSymbol.IsAssignableTo(targetTypeSymbol))
            {
                // do nothing since source is derived type from target
            }
            else if (!targetTypeSymbol.IsInterface 
                && !targetTypeSymbol.IsValueType 
                && !sourceTypeSymbol.IsValueType
                && targetTypeSymbol.IsAssignableTo(sourceTypeSymbol))
            {
                // target type is a derived type of source type, so try runtime cast
                _ilgen.Emit(OpCodes.Castclass, targetType);
            }
            else if (targetTypeSymbol.IsInterface && sourceTypeSymbol.IsAssignableTo(targetTypeSymbol))
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
        private bool TryEmitConvertToType(TypeReference sourceType, TypeReference targetType, bool isChecked)
        {
            return targetType.MetadataType switch
            {
                MetadataType.SByte => TryEmitConvertToSByte(sourceType, isChecked),
                MetadataType.Byte => TryEmitConvertToByte(sourceType, isChecked),
                MetadataType.Int16 => TryEmitConvertToInt16(sourceType, isChecked),
                MetadataType.UInt16 => TryEmitConvertToUInt16(sourceType, isChecked),
                MetadataType.Int32 => TryEmitConvertToInt32(sourceType, isChecked),
                MetadataType.UInt32 => TryEmitConvertToUInt32(sourceType, isChecked),
                MetadataType.Int64 => TryEmitConvertToInt64(sourceType, isChecked),
                MetadataType.UInt64 => TryEmitConvertToUInt64(sourceType, isChecked),
                MetadataType.Single => TryEmitConvertToSingle(sourceType), // always checked
                MetadataType.Double => TryEmitConvertToDouble(sourceType), // always checked
                _ => false
            };
        }

        private bool TryEmitConvertToSByte(TypeReference sourceType, bool isChecked)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.SByte:
                    break;

                case MetadataType.Byte:
                case MetadataType.UInt16:
                case MetadataType.UInt32:
                case MetadataType.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I1_Un : OpCodes.Conv_I1);
                    break;

                case MetadataType.Int16:
                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.Single:
                case MetadataType.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToByte(TypeReference sourceType, bool isChecked)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.Byte:
                    break;

                case MetadataType.UInt16:
                case MetadataType.UInt32:
                case MetadataType.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U1_Un : OpCodes.Conv_U1);
                    break;

                case MetadataType.SByte:
                case MetadataType.Int16:
                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.Single:
                case MetadataType.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToInt16(TypeReference sourceType, bool isChecked)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.SByte:
                case MetadataType.Byte:
                    _ilgen.Emit(OpCodes.Conv_I2);
                    break;

                case MetadataType.Int16:
                    break;

                case MetadataType.UInt16:
                case MetadataType.UInt32:
                case MetadataType.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I2_Un : OpCodes.Conv_I2);
                    break;

                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.Single:
                case MetadataType.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToUInt16(TypeReference sourceType, bool isChecked)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.SByte:
                case MetadataType.Byte:
                    _ilgen.Emit(OpCodes.Conv_U2);
                    break;

                case MetadataType.UInt16:
                    break;

                case MetadataType.UInt32:
                case MetadataType.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U2_Un : OpCodes.Conv_U2);
                    break;

                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.Single:
                case MetadataType.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToInt32(TypeReference sourceType, bool isChecked)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.SByte:
                case MetadataType.Byte:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                    _ilgen.Emit(OpCodes.Conv_I4);
                    break;

                case MetadataType.Int32:
                    break;

                case MetadataType.UInt32:
                case MetadataType.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I4_Un : OpCodes.Conv_I4);
                    break;

                case MetadataType.Int64:
                case MetadataType.Single:
                case MetadataType.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I4 : OpCodes.Conv_I4);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToUInt32(TypeReference sourceType, bool isChecked)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.SByte:
                case MetadataType.Int16:
                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.Single:
                case MetadataType.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U4 : OpCodes.Conv_U4);
                    break;

                case MetadataType.Byte:
                case MetadataType.UInt16:
                    _ilgen.Emit(OpCodes.Conv_U4);
                    break;

                case MetadataType.UInt32:
                    break;

                case MetadataType.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U4_Un : OpCodes.Conv_U4);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToInt64(TypeReference sourceType, bool isChecked)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.SByte:
                case MetadataType.Byte:
                case MetadataType.Int16:
                case MetadataType.UInt16:
                case MetadataType.Int32:
                case MetadataType.UInt32:
                    _ilgen.Emit(OpCodes.Conv_I8);
                    break;

                case MetadataType.UInt64:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_I8_Un : OpCodes.Conv_I8);
                    break;

                case MetadataType.Int64:
                case MetadataType.Single:
                case MetadataType.Double:
                    _ilgen.Emit(OpCodes.Conv_Ovf_I8);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToUInt64(TypeReference sourceType, bool isChecked)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.SByte:
                case MetadataType.Int16:
                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.Single:
                case MetadataType.Double:
                    _ilgen.Emit(isChecked ? OpCodes.Conv_Ovf_U8 : OpCodes.Conv_U8);
                    break;

                case MetadataType.Byte:
                case MetadataType.UInt16:
                case MetadataType.UInt32:
                    _ilgen.Emit(OpCodes.Conv_U8);
                    break;

                case MetadataType.UInt64:
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToSingle(TypeReference sourceType)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.SByte:
                case MetadataType.Int16:
                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.Double:
                    _ilgen.Emit(OpCodes.Conv_R4);
                    break;

                case MetadataType.Byte:
                case MetadataType.UInt16:
                case MetadataType.UInt32:
                case MetadataType.UInt64:
                    _ilgen.Emit(OpCodes.Conv_R_Un);
                    break;

                case MetadataType.Single:
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool TryEmitConvertToDouble(TypeReference sourceType)
        {
            switch (sourceType.MetadataType)
            {
                case MetadataType.SByte:
                case MetadataType.Int16:
                case MetadataType.Int32:
                case MetadataType.Int64:
                case MetadataType.Byte:
                case MetadataType.UInt16:
                case MetadataType.UInt32:
                case MetadataType.UInt64:
                case MetadataType.Single:
                    _ilgen.Emit(OpCodes.Conv_R8);
                    break;

                case MetadataType.Double:
                    break;

                default:
                    return false;
            }

            return true;
        }

        private bool IsUnsigned(TypeReference type)
        {
            switch (type.MetadataType)
            {
                case MetadataType.Byte:
                case MetadataType.Boolean:
                case MetadataType.UInt16:
                case MetadataType.UInt32:
                case MetadataType.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsFloatingPoint(TypeReference type)
        {
            switch (type.MetadataType)
            {
                case MetadataType.Single:
                case MetadataType.Double:
                    return true;
                default:
                    return false;
            }
        }

        public override void EmitAsType(TypeSymbol instanceTypeSymbol)
        {
            var instanceType = _emitter.GetCecilType(instanceTypeSymbol);
            _ilgen.Emit(OpCodes.Isinst, instanceType);
        }

        public override void EmitAdd(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? OpCodes.Add
                : IsUnsigned(operandType) ? OpCodes.Add_Ovf_Un
                : OpCodes.Add_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitSubtract(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? OpCodes.Sub
                : IsUnsigned(operandType) ? OpCodes.Sub_Ovf_Un
                : OpCodes.Sub_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitMultiply(TypeSymbol operandTypeSymbol, bool isChecked)
        {
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            var op = (!isChecked || IsFloatingPoint(operandType)) ? OpCodes.Mul
                : IsUnsigned(operandType) ? OpCodes.Mul_Ovf_Un
                : OpCodes.Mul_Ovf;
            _ilgen.Emit(op);
        }

        public override void EmitDivide(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            var op = IsUnsigned(operandType) ? OpCodes.Div_Un : OpCodes.Div;
            _ilgen.Emit(op);
        }

        public override void EmitRemainder(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
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
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            var mask = (operandType.MetadataType == MetadataType.Int64 || operandType.MetadataType == MetadataType.UInt64) ? 0x3F : 0x1F;
            EmitLoadInt32(mask);
            _ilgen.Emit(OpCodes.And);
            _ilgen.Emit(OpCodes.Shl);
        }

        public override void EmitShiftRight(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            var mask = (operandType.MetadataType == MetadataType.Int64 || operandType.MetadataType == MetadataType.UInt64) ? 0x3F : 0x1F;
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
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            if (operandType.MetadataType == MetadataType.Boolean)
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
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) ? OpCodes.Clt_Un : OpCodes.Clt);
        }

        public override void EmitLessThanOrEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) || IsFloatingPoint(operandType) ? OpCodes.Cgt_Un : OpCodes.Cgt);
            _ilgen.Emit(OpCodes.Ldc_I4_0);
            _ilgen.Emit(OpCodes.Ceq);
        }

        public override void EmitGreaterThan(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) ? OpCodes.Cgt_Un : OpCodes.Cgt);
        }

        public override void EmitGreaterThanOrEqual(TypeSymbol operandTypeSymbol)
        {
            var operandType = _emitter.GetCecilType(operandTypeSymbol);
            _ilgen.Emit(IsUnsigned(operandType) || IsFloatingPoint(operandType) ? OpCodes.Clt_Un : OpCodes.Clt);
            _ilgen.Emit(OpCodes.Ldc_I4_0);
            _ilgen.Emit(OpCodes.Ceq);
        }

        public override void EmitThrow(string message)
        {
            EmitThrow(_externalSymbols.GetTypeSymbol("System.InvalidOperationException"), message);
        }

        public override void EmitThrowAndReport(Diagnostic diagnostic)
        {
            EmitThrow(diagnostic.ToString());
            _diagnostics.Add(diagnostic);
        }

        public override void EmitThrow(TypeSymbol exceptionTypeSymbol, string message)
        {
            var constructorSymbol = exceptionTypeSymbol.GetFirstMember<ConstructorSymbol>(m => !m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].Type == _externalSymbols.String);
            var constructor = _emitter.GetCecilReference<MethodReference>(constructorSymbol!);
            _ilgen.Emit(OpCodes.Ldstr, message);
            _ilgen.Emit(OpCodes.Newobj, constructor);
            _ilgen.Emit(OpCodes.Throw);
        }
    }
}