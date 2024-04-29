namespace Parkour.Emitting;
using Symbols;

/// <summary>
/// Emits symbol declarations and bodies (IL)
/// </summary>
public abstract class SymbolEmitter
{
    public abstract void DefineClass(ClassSymbol classSymbol);
    public abstract void DefineValueType(ValueTypeSymbol valueTypeSymbol);
    public abstract void DefineInterface(InterfaceSymbol interfaceSymbol);

    public abstract void DefineField(FieldSymbol fieldSymbol);
    public abstract void DefineMethod(MethodSymbol methodSymbol);
    public abstract void DefineConstructor(ConstructorSymbol constructorSymbol);
    public abstract void DefineProperty(PropertySymbol propertySymbol);
    public abstract void DefineIndexer(IndexerSymbol indexerSymbol);

    public abstract void EmitMethodBody(MethodSymbol methodSymbol, Action<MethodSymbol, ILEmitter> fnEmitBody);
    public abstract void EmitConstructorBody(ConstructorSymbol constructorSymbol, Action<ConstructorSymbol, ILEmitter> fnEmitBody);

    /// <summary>
    /// Finishes emitting defined type and members into output
    /// </summary>
    public abstract EmitResult EmitSymbols();

    public class EmitResult
    {
        public ImmutableList<Diagnostic> Diagnostics { get; }

        public EmitResult(ImmutableList<Diagnostic>? diagnostics)
        {
            this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        }
    }

    /// <summary>
    /// Emits IL instructions using symbol abstraction.
    /// </summary>
    public abstract class ILEmitter
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
        /// Branches unconditionally to the label:
        /// [a] [b] [c] => [a] [b] [c]
        /// </summary>
        public abstract void EmitBranch(LabelSymbol label);

        /// <summary>
        /// Branches to the label if the value on top of the evaluation stack is true, non-zero or non-null.
        /// Removes the value from the top of the stack:
        /// [bool] [a] [b] [c] => [a] [b] [c]
        /// </summary>
        public abstract void EmitBranchTrue(LabelSymbol label);

        /// <summary>
        /// Branches to the label if the value on the top of the evaluation stack is false, zero or null.
        /// Removes the value from the top of the stack:
        /// [bool] [a] [b] [c] => [a] [b] [c]
        /// </summary>
        public abstract void EmitBranchFalse(LabelSymbol label);

        /// <summary>
        /// Duplicates the value on top of the evaluation stack:
        /// [a] [b] [c] => [a] [a] [b] [c]
        /// </summary>
        public abstract void EmitDup();

        /// <summary>
        /// Removes the value on the top of the evaluation stack:
        /// [a] [b] [c] => [b] [c]
        /// </summary>
        public abstract void EmitPop();

        /// <summary>
        /// Returns from current method body.
        /// </summary>
        public abstract void EmitReturn();

        /// <summary>
        /// Loads the object corresponding to the current instance (this) onto the evaluation stack:
        /// [a] [b] [c] => [this] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadInstance();

        /// <summary>
        /// Loads the object corresponding to the current instance (this) onto the evaluation stack:
        /// [a] [b] [c] => [&this] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadInstanceAddress();

        /// <summary>
        /// Loads the parameter value onto the evaluation stack:
        /// [a] [b] [c] => [parameter-value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadParameter(ParameterSymbol parameter);

        /// <summary>
        /// Loads the address of the parameter onto the evaluation stack:
        /// [a] [b] [c] => [parameter-address] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadParameterAddress(ParameterSymbol parameter);

        /// <summary>
        /// Stores the value on the top of the evaluation stack into the parameter:
        /// [a] [b] [c] => stack: [b] [c], parameter: [a]
        /// </summary>
        public abstract void EmitStoreParameter(ParameterSymbol parameter);

        /// <summary>
        /// Loads the array element from the array and index on the top of the evaluation stack, onto the evaluation stack.
        /// [index] [array] [a] [b] [c] => [array[index]] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadArrayElement(TypeSymbol typeSymbol);

        /// <summary>
        /// Loads the address of the array element from the array and index on the top of the evaluation stack, onto the evaluation stack:
        /// [index] [array] [a] [b] [c] => [array-element-address] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadArrayElementAddress(TypeSymbol elementTypeSymbol);

        /// <summary>
        /// Stores the value on the top of the evaluation stack to into the array element address already on the evaluation stack.
        /// [value] [index] [array] [a] [b] [c] => stack: [a] [b] [c], array[index]: [value]
        /// </summary>
        public abstract void EmitStoreArrayElement(TypeSymbol elementTypeSymbol);

        /// <summary>
        /// Loads the field value onto the evaluation stack:
        /// [instance] [a] [b] [c] => [instance-field-value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadField(FieldSymbol field);

        /// <summary>
        /// Loads the address of the field onto the evaluation stack:
        /// [instance] [a] [b] [c] => [instance-field-address] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadFieldAddress(FieldSymbol field);

        /// <summary>
        /// Stores the value on the top of the evaluation stack into the field:
        /// [value] [instance] [a] [b] [c] => stack: [a] [b] [c], instance.field: [value]
        /// </summary>
        public abstract void EmitStoreField(FieldSymbol field);

        /// <summary>
        /// Loads the value of the variable onto the evaluation stack:
        /// variable: [x], stack: [a] [b] [c] => [x] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadVariable(VariableSymbol variable);

        /// <summary>
        /// Loads the address of the variabale onto the evaluation stack:
        /// variable: [x], stack: [variable-address] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadVariableAddress(VariableSymbol variable);

        /// <summary>
        /// Stores the value on the top of the evaluation stack into the variable:
        /// stack: [a] [b] [c] => stack: [b] [c], variable: [a]
        /// </summary>
        public abstract void EmitStoreVariable(VariableSymbol variable);

        /// <summary>
        /// Loads the null value onto the top of the evaluation stack:
        /// [a] [b] [c] => [null] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadNull();

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadBool(bool value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadSByte(sbyte value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadByte(byte value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadInt16(short value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadUInt16(ushort value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadInt32(int value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadUInt32(uint value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadInt64(long value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadUInt64(ulong value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadSingle(float value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadDouble(double value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadDecimal(decimal value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadString(string value);

        /// <summary>
        /// Loads a value onto the top of the evaluation stack:
        /// [a] [b] [c] => [value] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadChar(char value);

        /// <summary>
        /// Loads the pointer to the method's function onto the top of the evaluation stack:
        /// [a] [b] [c] => [function-pointer] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadMethod(MethodSymbol methodSymbol);

        /// <summary>
        /// Loads the runtime token for a symbol onto the top of the evaluation stack:
        /// [a] [b] [c] => [token] [a] [b] [c]
        /// </summary>
        public abstract void EmitLoadToken(MemberSymbol symbol);

        /// <summary>
        /// Loads the default value for a type onto the top of the evaluation stack:
        /// [a] [b] [c] => [default] [a] [b] [c]
        /// </summary>
        public abstract void EmitDefault(TypeSymbol type);

        /// <summary>
        /// Calls the method using the arguments on the top of the evaluation stack:
        /// [arg2] [arg1] [instance] [a] [b] [c] => [result] [a] [b] [c]
        /// </summary>
        public abstract void EmitCall(MethodSymbol method);

        /// <summary>
        /// Calls the constructor as a method, using the arguments on top of the evaluation stack:
        /// [arg2] [arg1] [instance] [a] [b] [c] => [a] [b] [c]
        /// </summary>
        public abstract void EmitCall(ConstructorSymbol method);

        /// <summary>
        /// Creates and puts a new instance on the top of the evaluation stack,
        /// usign the arguments currently on the top of the evaluation stack.
        /// [arg2] [arg1] [a] [b] [c] => [new-instance] [a] [b] [c]
        /// </summary>
        public abstract void EmitNew(ConstructorSymbol constructor);

        /// <summary>
        /// Creates a new zero-based single dimensional array.
        /// [size] [a] [b] [c] => [array] [a] [b] [c]
        /// </summary>
        public abstract void EmitNewArray(TypeSymbol elementTypeSymbol);

        /// <summary>
        /// Loads an initialized value-type value onto the top of the evaluation stack.
        /// [arg2] [arg1] [a] [b] [c] => [value-type] [a] [b] [c]
        /// </summary>
        public abstract void EmitInit(TypeSymbol type);

        /// <summary>
        /// Replaces the value on the top of the evaluation stack with its converted value:
        /// [a] [b] [c] => [x] [b] [c]
        /// </summary>
        public abstract void EmitConvert(TypeSymbol sourceType, TypeSymbol targetType, bool isChecked);

        /// <summary>
        /// Adds the top two primitive numeric values on the execution stack:
        /// [a] [b] [c] => [b + a] [c]
        /// </summary>
        public abstract void EmitAdd(TypeSymbol operandType, bool isChecked);

        /// <summary>
        /// Subtracts the top two primitive numeric values on the execution stack:
        /// [a] [b] [c] => [b - a] [c]
        /// </summary>
        public abstract void EmitSubtract(TypeSymbol operandType, bool isChecked);

        /// <summary>
        /// Multiplies the top two primitive numeric values on the execution stack:
        /// [a] [b] [c] => [b * a] [c]
        /// </summary>
        public abstract void EmitMultiply(TypeSymbol operandType, bool isChecked);

        /// <summary>
        /// Divides the top two primitive numeric values on the execution stack and pushes the result on to the stack:
        /// [a] [b] [c] => [b / a] [c]
        /// </summary>
        public abstract void EmitDivide(TypeSymbol operandType);

        /// <summary>
        /// Divides the top two primitive numeric values on the execution stack and pushes the remainder onto the stack:
        /// [a] [b] [c] => [b % a] [c]
        /// </summary>
        public abstract void EmitRemainder(TypeSymbol operandType);

        /// <summary>
        /// Negates the primitive numeric value on the top of the execution stack:
        /// [a] [b] [c] => [-a] [b] [c]
        /// </summary>
        public abstract void EmitNegate(TypeSymbol operandType);

        /// <summary>
        /// Increments the primitive numberic value on the top of the execution stack:
        /// [a] [b] [c] => [a++] [b] [c]
        /// </summary>
        public abstract void EmitIncrement(TypeSymbol operandType, bool isChecked);

        /// <summary>
        /// Decrements the primitive numberic value on the top of the execution stack:
        /// [a] [b] [c] => [a--] [b] [c]
        /// </summary>
        public abstract void EmitDecrement(TypeSymbol operandType, bool isChecked);

        /// <summary>
        /// Performs a bitwise and with the top two integer values on the execution stack:
        /// [a] [b] [c] => [b & a] [c]
        /// </summary>
        public abstract void EmitAnd();

        /// <summary>
        /// Performs a bitwise or with the top two integer values on the execution stack:
        /// [a] [b] [c] => [b | a] [c]
        /// </summary>
        public abstract void EmitOr();

        /// <summary>
        /// Performs a bitwise XOR with the top two integer values on the execution stack:
        /// [a] [b] [c] => [b ^ a] [c]
        /// </summary>
        public abstract void EmitXor();

        /// <summary>
        /// Performs a bitwise NOT with the top two integer values on the execution stack:
        /// [a] [b] [c] => [~a] [b] [c]
        /// </summary>
        public abstract void EmitNot();

        /// <summary>
        /// Peforms a bitwise shift-left with the top two integer values on the execution stack:
        /// [a] [b] [c] => [b << a] [c]
        /// </summary>
        public abstract void EmitShiftLeft(TypeSymbol operandType);

        /// <summary>
        /// Performs a bitswise shift-right with the top two integer values on the execution stack:
        /// [a] [b] [c] => [b >> a] [c]
        /// </summary>
        public abstract void EmitShiftRight(TypeSymbol operandType);

        /// <summary>
        /// Compares the top two values on the execution stack for equality:
        /// [a] [b] [c] => [b == a] [c]
        /// </summary>
        public abstract void EmitEqual(TypeSymbol operandType);

        /// <summary>
        /// Compares the top two values on the execution stack for inequality:
        /// [a] [b] [c] => [b != a] [c]
        /// </summary>
        public abstract void EmitNotEqual(TypeSymbol operandType);

        /// <summary>
        /// Compares the top two numeric values on the execution stack for less than:
        /// [a] [b] [c] => [b < a] [c]
        /// </summary>
        public abstract void EmitLessThan(TypeSymbol operandType);

        /// <summary>
        /// Compares the top two numeric values on the execution stack for less than or equals:
        /// [a] [b] [c] => [b <= a] [c]
        /// </summary>
        public abstract void EmitLessThanOrEqual(TypeSymbol operandType);

        /// <summary>
        /// Compares the top two numeric values on the execution stack for greater than:
        /// [a] [b] [c] => [b > a] [c]
        /// </summary>
        public abstract void EmitGreaterThan(TypeSymbol operandType);

        /// <summary>
        /// Compares the top two numeric values on the execution stack for greater than or equals:
        /// [a] [b] [c] => [b >= a] [c]
        /// </summary>
        public abstract void EmitGreaterThanOrEqual(TypeSymbol operandType);

        /// <summary>
        /// Throws an exception of <see cref="exceptionType"/> at runtime.
        /// </summary>
        public abstract void EmitThrow(TypeSymbol exceptionType, string message);

        /// <summary>
        /// Throw an <see cref="InvalidOperationException"/> at runtime.
        /// </summary>
        public abstract void EmitThrow(string message);

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> at runtime and reports a <see cref="Diagnostic"/>.
        /// </summary>
        public abstract void EmitThrowAndReport(Diagnostic diagnostic);
    }
}

