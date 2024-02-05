namespace Parkour.Emitting;
using Symbols;

/// <summary>
/// Emits symbol declarations and bodies (IL)
/// </summary>
public abstract class SymbolEmitter
{
    /// <summary>
    /// Define a type to be emitted.
    /// </summary>
    public abstract void DefineType(TypeSymbol type);

    /// <summary>
    /// Define a member to be emitted (after all types are defined).
    /// </summary>
    public abstract void DefineMember(MemberSymbol symbol);

    /// <summary>
    /// Emit member body IL instructions.
    /// </summary>
    public abstract void EmitBody<TSymbol>(TSymbol symbol, Action<TSymbol, SymbolILEmitter> fnEmitBody) where TSymbol : Symbol;

    /// <summary>
    /// Finishes emitting defined type and members into output
    /// </summary>
    public abstract void EmitDefinedTypesAndMembers();
}

/// <summary>
/// Emits IL instructions using symbol abstraction.
/// </summary>
public abstract class SymbolILEmitter
{
    /// <summary>
    /// Declares the start of a variable's use.
    /// </summary>
    public abstract void DeclareVariableStart(VariableSymbol variable);

    /// <summary>
    /// Declares the end of a variable's use.
    /// </summary>
    public abstract void DeclareVariableEnd(VariableSymbol variable);

    /// <summary>
    /// Marks the label as existing at the current location in the emitted IL.
    /// </summary>
    public abstract void MarkLabel(LabelSymbol label);

    /// <summary>
    /// Branches unconditionally to the label.
    /// </summary>
    public abstract void EmitBranch(LabelSymbol label);

    /// <summary>
    /// Branches to the label if the value on top of the evaluation stack is true, non-zero or non-null.
    /// Removes the value from the top of the stack.
    /// </summary>
    public abstract void EmitBranchTrue(LabelSymbol label);

    /// <summary>
    /// Branches to the label if the value on the top of the evaluation stack is false, zero or null.
    /// Removes the value from the top of the stack.
    /// </summary>
    public abstract void EmitBranchFalse(LabelSymbol label);

    /// <summary>
    /// Duplicates the value on top of the evaluation stack.
    /// </summary>
    public abstract void EmitDup();

    /// <summary>
    /// Removes the value on the top of the evaluation stack.
    /// </summary>
    public abstract void EmitPop();

    /// <summary>
    /// Returns from current method body.
    /// </summary>
    public abstract void EmitReturn();

    /// <summary>
    /// Loads the parameter corresponding to the current instance onto the evaluation stack.
    /// </summary>
    public abstract void EmitLoadInstance(MemberSymbol member);

    /// <summary>
    /// Loads the parameter value onto the evaluation stack.
    /// </summary>
    public abstract void EmitLoadParameter(ParameterSymbol parameter);

    /// <summary>
    /// Loads the address of the parameter onto the evaluation stack.
    /// </summary>
    public abstract void EmitLoadParameterAddress(ParameterSymbol parameter);

    /// <summary>
    /// Stores the value on the top of the evaluation stack into the parameter
    /// </summary>
    public abstract void EmitStoreParameter(ParameterSymbol parameter);

    /// <summary>
    /// Loads the field value onto the evaluation stack.
    /// </summary>
    public abstract void EmitLoadField(FieldSymbol field);

    /// <summary>
    /// Loads the address of the field onto the evaluation stack.
    /// </summary>
    public abstract void EmitLoadFieldAddress(FieldSymbol field);

    /// <summary>
    /// Stores the value on the top of the evaluation stack into the field.
    /// </summary>
    public abstract void EmitStoreField(FieldSymbol field);

    /// <summary>
    /// Loads the value of the variable onto the evaluation stack.
    /// </summary>
    public abstract void EmitLoadVariable(VariableSymbol variable);

    /// <summary>
    /// Loads the address of the variabale onto the evaluation stack.
    /// </summary>
    public abstract void EmitLoadVariableAddress(VariableSymbol variable);

    /// <summary>
    /// Stores the value on the top of the evaluation stack into the variable.
    /// </summary>
    public abstract void EmitStoreVariable(VariableSymbol variable);

    /// <summary>
    /// Loads the null value onto the top of the evaluation stack.
    /// </summary>
    public abstract void EmitLoadNull();
    public abstract void EmitLoadBool(bool value);
    public abstract void EmitLoadSByte(sbyte value);
    public abstract void EmitLoadByte(byte value);
    public abstract void EmitLoadInt16(short value);
    public abstract void EmitLoadUInt16(ushort value);
    public abstract void EmitLoadInt32(int value);
    public abstract void EmitLoadUInt32(uint value);
    public abstract void EmitLoadInt64(long value);
    public abstract void EmitLoadUInt64(ulong value);
    public abstract void EmitLoadSingle(float value);
    public abstract void EmitLoadDouble(double value);
    public abstract void EmitLoadDecimal(decimal value);
    public abstract void EmitLoadString(string value);
    public abstract void EmitLoadChar(char value);
    public abstract void EmitLoadMethod(MethodSymbol methodSymbol);
    public abstract void EmitLoadToken(MemberSymbol symbol);
    public abstract void EmitDefault(TypeSymbol type);

    /// <summary>
    /// Calls the method using the arguments on the top of the evaluation stack.
    /// </summary>
    public abstract void EmitCall(MethodSymbol method);

    /// <summary>
    /// Calls the constructor using the arguments on top of the evaluation stack.
    /// </summary>
    public abstract void EmitCall(ConstructorSymbol method);

    /// <summary>
    /// Creates and puts a new instance on the top of the evaluation stack,
    /// usign the arguments currently on the top of the evaluation stack.
    /// </summary>
    public abstract void EmitNew(ConstructorSymbol constructor);

    /// <summary>
    /// Loads an initialized value-type value onto the top of the evaluation stack.
    /// </summary>
    public abstract void EmitInit(TypeSymbol type);

    /// <summary>
    /// Replaces the value on the top of the evaluation stack with its converted value.
    /// </summary>
    public abstract void EmitConvert(TypeSymbol sourceType, TypeSymbol targetType, bool isChecked);

    /// <summary>
    /// Adds the top two primitive numeric values on the execution stack
    /// </summary>
    public abstract void EmitAdd(TypeSymbol operandType, bool isChecked);

    /// <summary>
    /// Subtracts the top two primitive numeric values on the execution stack.
    /// </summary>
    public abstract void EmitSubtract(TypeSymbol operandType, bool isChecked);

    /// <summary>
    /// Multiplies the top two primitive numeric values on the execution stack.
    /// </summary>
    public abstract void EmitMultiply(TypeSymbol operandType, bool isChecked);

    /// <summary>
    /// Divides the top two primitive numeric values on the execution stack and pushes the result on to the stack.
    /// </summary>
    public abstract void EmitDivide(TypeSymbol operandType);

    /// <summary>
    /// Divides the top two primitive numeric values on the execution stack and pushes the remainder onto the stack.
    /// </summary>
    public abstract void EmitRemainder(TypeSymbol operandType);

    /// <summary>
    /// Negates the primitive numeric value on the top of the execution stack.
    /// </summary>
    public abstract void EmitNegate(TypeSymbol operandType);

    /// <summary>
    /// Performs a bitwise and with the top two integer values on the execution stack.
    /// </summary>
    public abstract void EmitAnd();

    /// <summary>
    /// Performs a bitwise or with the top two integer values on the execution stack.
    /// </summary>
    public abstract void EmitOr();

    /// <summary>
    /// Performs a bitwise XOR with the top two integer values on the execution stack.
    /// </summary>
    public abstract void EmitXor();

    /// <summary>
    /// Performs a bitwise NOT with the top two integer values on the execution stack.
    /// </summary>
    public abstract void EmitNot();

    /// <summary>
    /// Peforms a bitwise shift-left with the top two integer values on the execution stack.
    /// </summary>
    public abstract void EmitShiftLeft(TypeSymbol operandType);

    /// <summary>
    /// Performs a bitswise shift-right with the top two integer values on the execution stack.
    /// </summary>
    public abstract void EmitShiftRight(TypeSymbol operandType);

    /// <summary>
    /// Compares the top two values on the execution stack for equality.
    /// </summary>
    public abstract void EmitEquals(TypeSymbol operandType);

    /// <summary>
    /// Compares the top two values on the execution stack for inequality.
    /// </summary>
    public abstract void EmitNotEquals(TypeSymbol operandType);

    /// <summary>
    /// Compares the top two numeric values on the execution stack for less than.
    /// </summary>
    public abstract void EmitLessThan(TypeSymbol operandType);

    /// <summary>
    /// Compares the top two numeric values on the execution stack for less than or equals.
    /// </summary>
    public abstract void EmitLessThanOrEqual(TypeSymbol operandType);

    /// <summary>
    /// Compares the top two numeric values on the execution stack for greater than.
    /// </summary>
    public abstract void EmitGreaterThan(TypeSymbol operandType);

    /// <summary>
    /// Compares the top two numeric values on the execution stack for greater than or equals.
    /// </summary>
    public abstract void EmitGreaterThanOrEquals(TypeSymbol operandType);

    /// <summary>
    /// Throw an <see cref="InvalidOperationException"/> at runtime.
    /// </summary>
    public abstract void EmitThrow(string message);

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> at runtime and reports a <see cref="Diagnostic"/>.
    /// </summary>
    public abstract void EmitThrowAndReport(string message);

    /// <summary>
    /// Throws an exception of <see cref="exceptionType"/> at runtime.
    /// </summary>
    public abstract void EmitThrow(TypeSymbol exceptionType, string message);
}
