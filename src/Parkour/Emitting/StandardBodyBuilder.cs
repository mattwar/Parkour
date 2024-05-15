using System.Diagnostics.CodeAnalysis;

namespace Parkour.Emitting;

using Semantics;
using Symbols;
using System.Net;

/// <summary>
/// Builds symbol bodies by emitting IL instructions into an <see cref="ILEmitter"/>
/// </summary>
public class StandardBodyBuilder : BodyBuilder
{
    protected MemberSymbol CurrentMember { get; }
    protected SymbolTable ExternalSymbols { get; }
    protected ILEmitter Emitter { get; }
    protected bool IsChecked { get; }

    public StandardBodyBuilder(
        MemberSymbol currentMember, 
        ILEmitter ilEmitter,
        bool isChecked = false)
    {
        this.CurrentMember = currentMember;
        this.ExternalSymbols = ilEmitter.ExternalSymbols;
        this.Emitter = ilEmitter;
        this.IsChecked = isChecked;
    }

    public override void BuildBody(
        Expression body, 
        TypeSymbol returnType, 
        LabelSymbol? returnLabel)
    {
        EmitExpression(body);
        var prevType = body.ResultType;

        if (returnLabel != null)
        {
            this.Emitter.EmitConvert(prevType, returnLabel.Type, true);
            this.Emitter.MarkLabel(returnLabel);
            prevType = returnLabel.Type;
        }

        this.Emitter.EmitConvert(prevType, returnType, true);
        this.Emitter.EmitReturn();
    }

    /// <summary>
    /// Emits an expression.
    /// </summary>
    protected virtual void EmitExpression(Expression expression)
    {
        if (expression.Diagnostics.Count > 0)
        {
            var error = expression.Diagnostics.FirstOrDefault(e => e.Severity == DiagnosticSeverity.Error);
            if (error != null)
            {
                this.Emitter.EmitThrow(error.ToString());
                return;
            }
        }

        switch (expression)
        {
            case AdjustedReferenceExpression are:
                EmitAdjustedReferenceExpression(are, asAddress: false);
                break;

            case AssignExpression assign:
                EmitAssign(assign);
                break;

            case AsTypeExpression asType:
                EmitAsType(asType);
                break;

            case BlockExpression block:
                EmitBlock(block);
                break;

            case BranchExpression branch:
                EmitBranch(branch);
                break;

            case CallExpression call:
                EmitCall(call);
                break;

            case ConditionExpression condition:
                EmitCondition(condition, asAddress: false);
                break;

            case ConstantExpression constant:
                EmitConstant(constant);
                break;

            case ConvertExpression convert:
                EmitConvert(convert);
                break;

            case DefaultExpression @default:
                EmitDefault(@default);
                break;

            case ElementExpression element:
                EmitElement(element);
                break;

            case IsTypeExpression isType:
                EmitIsType(isType);
                break;

            case LabelExpression label:
                EmitLabel(label);
                break;

            case LoopExpression loop:
                EmitLoop(loop);
                break;

            case MemberExpression mex:
                EmitMember(mex, asAddress: false);
                break;

            case NameExpression nre:
                EmitNameReference(nre, asAddress: false);
                break;

            case NewExpression ne:
                EmitNew(ne);
                break;

            case NewArrayInitExpression newArrayInit:
                EmitNewArrayInit(newArrayInit);
                break;

            case NewArraySizeExpression newArraySize:
                EmitNewArraySize(newArraySize);
                break;

            case OperatorExpression opex:
                EmitOperator(opex);
                break;

            case SymbolExpression sre:
                EmitSymbolReference(sre, asAddress: false);
                break;

            case ThisExpression me:
                EmitThis(me, asAddress: false);
                break;

            case TypeOfExpression tof:
                EmitTypeOf(tof);
                break;

            case VariableExpression variable:
                EmitVariable(variable);
                break;

            case VoidExpression vex:
                break;

            default:
                this.Emitter.EmitThrowAndReport(new Diagnostic($"Could not emit IL for expression '{expression.GetType().Name}'").WithLocation(expression.Location));
                break;
        }
    }

    protected virtual void EmitExpression(Expression expression, bool asAddress)
    {
        if (asAddress)
        {
            this.EmitExpressionAddress(expression);
        }
        else
        {
            this.EmitExpression(expression);
        }
    }

    /// <summary>
    /// Emits the expression and then adjusts the result on the stack as necessary to match the target type.
    /// </summary>
    protected virtual void EmitExpressionAsType(Expression expression, TypeSymbol targetType)
    {
        EmitExpression(expression);
        if (expression.ResultType != targetType)
        {
            this.Emitter.EmitConvert(expression.ResultType, targetType, true);
        }
    }

    protected virtual void EmitExpressionAddress(Expression expression)
    {
        switch (expression)
        {
            case AdjustedReferenceExpression are:
                EmitAdjustedReferenceExpression(are, asAddress: true);
                break;

            case ConditionExpression condition:
                EmitCondition(condition, asAddress: true);
                break;

            case MemberExpression mex:
                EmitMember(mex, asAddress: true);
                break;

            case NameExpression nre:
                EmitNameReference(nre, asAddress: true);
                break;

            case ThisExpression me:
                EmitThis(me, asAddress: true);
                break;

            default:
                var tmp = new VariableSymbol("tmp", expression.ResultType);
                this.EmitExpression(expression);
                this.Emitter.EmitStoreVariable(tmp);
                this.Emitter.EmitLoadVariableAddress(tmp);
                break;
        }
    }

    #region Assign Emit

    private VariableSymbol CopyToTemporaryVariable(TypeSymbol type)
    {
        var variable = new VariableSymbol("tmp", type);
        this.Emitter.DeclareVariableStart(variable);
        this.Emitter.EmitDup();
        this.Emitter.EmitStoreVariable(variable);
        return variable;
    }

    private void RestoreFromTemporaryVariable(VariableSymbol variable)
    {
        this.Emitter.EmitLoadVariable(variable);
        this.Emitter.DeclareVariableEnd(variable);
    }

    protected virtual void EmitAssign(AssignExpression assign)
    {
        var targetSymbol = assign.Target.ReferencedSymbol;
        var targetMember = GetMemberExpression(assign.Target);
        VariableSymbol? temp = null;

        switch (targetSymbol)
        {
            case VariableSymbol variableSymbol:
                EmitExpressionAsType(assign.Source, variableSymbol.Type);
                this.Emitter.EmitStoreVariable(variableSymbol);
                if (assign.ResultType != SpecialSymbols.Void)
                    this.Emitter.EmitLoadVariable(variableSymbol);
                break;

            case ParameterSymbol parameterSymbol:
                EmitExpressionAsType(assign.Source, parameterSymbol.Type);
                this.Emitter.EmitStoreParameter(parameterSymbol);
                if (assign.ResultType != SpecialSymbols.Void)
                    this.Emitter.EmitLoadParameter(parameterSymbol);
                break;

            case FieldSymbol fieldSymbol:
                if (!fieldSymbol.IsStatic)
                {
                    if (targetMember != null)
                    {
                        EmitExpression(targetMember.Instance, asAddress: targetMember.Instance.ResultType.IsValueType);
                    }
                    else if (IsMemberOfThis(fieldSymbol))
                    {
                        this.Emitter.EmitLoadInstance();
                    }
                    else
                    {
                        this.Emitter.EmitThrowAndReport(new Diagnostic($"Instance field '{fieldSymbol.FullName}' does not have instance.").WithLocation(assign.Location));
                    }
                }

                EmitExpressionAsType(assign.Source, fieldSymbol.Type);

                temp = (assign.ResultType != SpecialSymbols.Void)
                    ? CopyToTemporaryVariable(fieldSymbol.Type)
                    : null;

                this.Emitter.EmitStoreField(fieldSymbol);
                break;

            case PropertySymbol propertySymbol:
                if (propertySymbol.SetMethod != null)
                {
                    if (!propertySymbol.IsStatic)
                    {
                        if (targetMember != null)
                        {
                            EmitExpression(targetMember.Instance, asAddress: targetMember.Instance.ResultType.IsValueType);
                        }
                        else if (IsMemberOfThis(propertySymbol))
                        {
                            this.Emitter.EmitLoadInstance();
                        }
                        else
                        {
                            this.Emitter.EmitThrowAndReport(new Diagnostic($"Instance property '{propertySymbol.FullName}' does not have instance.").WithLocation(assign.Target.Location));
                        }
                    }

                    EmitExpressionAsType(assign.Source, propertySymbol.Type);

                    temp = (assign.ResultType != SpecialSymbols.Void)
                        ? CopyToTemporaryVariable(propertySymbol.Type)
                        : null;

                    this.Emitter.EmitCall(propertySymbol.SetMethod);
                }
                break;

            default:
                if (assign.Target is ElementExpression ee)
                {
                    if (ee.Expression.ResultType is ArraySymbol array)
                    {
                        if (array.IsSZArray)
                        {
                            EmitExpression(ee.Expression); // the array
                            EmitExpressionAsType(ee.Arguments[0], this.ExternalSymbols.Int32); // the index
                            EmitExpressionAsType(assign.Source, array.ElementType);

                            temp = (assign.ResultType != SpecialSymbols.Void)
                                ? CopyToTemporaryVariable(array.ElementType)
                                : null;

                            this.Emitter.EmitStoreArrayElement(array.ElementType);
                        }
                    }
                    else if (ee.IndexerSymbol != null
                        && ee.IndexerSymbol.SetMethod != null)
                    {
                        EmitMethodCallInstance(ee.IndexerSymbol.SetMethod, ee.Expression, ee.Location);

                        // emit index arguments
                        EmitArguments(ee.IndexerSymbol.SetMethod.Parameters, ee.Arguments);

                        // final argument (value)
                        EmitExpressionAsType(assign.Source, ee.IndexerSymbol.ElementType);

                        temp = (assign.ResultType != SpecialSymbols.Void)
                            ? CopyToTemporaryVariable(ee.IndexerSymbol.ElementType)
                            : null;

                        this.Emitter.EmitCall(ee.IndexerSymbol.SetMethod);
                    }
                }
                break;
        }

        if (temp != null)
        {
            RestoreFromTemporaryVariable(temp);
        }
    }

    private bool IsMemberOfThis(MemberSymbol memberSymbol)
    {
        if (this.CurrentMember.DeclaringType is { } currentType
            && memberSymbol.DeclaringType is { } memberType)
        {
            return currentType == memberType
                || currentType.IsSubTypeOf(memberType);
        }

        return false;
    }

    /// <summary>
    /// Gets the underlying member expression from an expression that may have augments.
    /// </summary>
    protected virtual MemberExpression? GetMemberExpression(Expression expression)
    {
        switch (expression)
        {
            case MemberExpression member:
                return member;
            case AdjustedReferenceExpression filter:
                return GetMemberExpression(filter.ElementType);
            default:
                return null;
        }
    }

    #endregion

    #region AsType Emit
    public virtual void EmitAsType(AsTypeExpression asType)
    {
        if (asType.TypeSymbol != null)
        {
            if (asType.TypeSymbol.IsValueType)
            {
                this.EmitExpression(asType.Expression);
                var isSameType = TypeEqualityComparer.Instance.Equals(asType.Expression.ResultType, asType.TypeSymbol);
                if (!isSameType)
                {
                    this.Emitter.EmitPop();
                    this.Emitter.EmitDefault(asType.TypeSymbol);
                }
            }
            else if (asType.Expression.ResultType.IsValueType)
            {
                // box and check
                this.EmitExpressionAsType(asType.Expression, ExternalSymbols.Object);
                this.Emitter.EmitAsType(asType.TypeSymbol);
            }
            else if (asType.Expression.ResultType == SpecialSymbols.Void)
            {
                this.EmitExpression(asType.Expression);
                this.Emitter.EmitPop();
                this.Emitter.EmitDefault(asType.TypeSymbol);
            }
            else
            {
                this.EmitExpression(asType.Expression);
                this.Emitter.EmitAsType(asType.TypeSymbol);
            }
        }
        else
        {
            this.Emitter.EmitLoadNull();
        }
    }
    #endregion

    #region Block Emit
    /// <summary>
    /// Emits <see cref="BlockExpression"/>
    /// </summary>
    protected virtual void EmitBlock(BlockExpression block)
    {
        var variables = block.Expressions
            .OfType<VariableExpression>()
            .Select(v => v.VariableSymbol)
            .OfType<VariableSymbol>()
            .ToList();

        // declare variable starts
        foreach (var variable in variables)
        {
            this.Emitter.DeclareVariableStart(variable);
        }

        for (int i = 0; i < block.Expressions.Count; i++)
        {
            if (i < block.Expressions.Count - 1)
            {
                EmitExpressionAsType(block.Expressions[i], SpecialSymbols.Void);
            }
            else
            {
                EmitExpression(block.Expressions[i]);
            }
        }

        // declare variable ends
        foreach (var variable in variables)
        {
            this.Emitter.DeclareVariableEnd(variable);
        }
    }
    #endregion

    #region Branch Emit
    /// <summary>
    /// Emits <see cref="BranchExpression"/>
    /// </summary>
    protected virtual void EmitBranch(BranchExpression branch)
    {
        if (branch.LabelSymbol != null)
        {
            if (branch.Expression != null)
            {
                EmitExpressionAsType(branch.Expression, branch.LabelSymbol.Type);
            }

            this.Emitter.EmitBranch(branch.LabelSymbol);
        }
    }
    #endregion

    #region Call Emit
    /// <summary>
    /// Emits <see cref="CallExpression"/>
    /// </summary>
    protected virtual void EmitCall(CallExpression call)
    {
        if (call.CalledSymbol is MethodSymbol methodSymbol)
        {
            var instance = methodSymbol.IsStatic ? null : GetMemberInstance(call.Expression);
            EmitMethodCall(methodSymbol, instance, call.Arguments, call.Location);
        }
        else if (call.CalledSymbol != null)
        {
            this.Emitter.EmitThrowAndReport(new Diagnostic($"Unhandled called symbol type '{call.CalledSymbol.GetType().Name}' in EmitCall").WithLocation(call.Location));
        }
        else
        {
            this.Emitter.EmitThrowAndReport(new Diagnostic($"Unknown call symbol").WithLocation(call.Location));
        }
    }

    protected virtual void EmitMethodCall(
        MethodSymbol methodSymbol, 
        Expression? instance, 
        ImmutableList<Expression> arguments, 
        ISourceLocation? location)
    {
        EmitMethodCallInstance(methodSymbol, instance, location);
        EmitArguments(methodSymbol.Parameters, arguments);
        this.Emitter.EmitCall(methodSymbol);
    }

    protected virtual void EmitMethodCallInstance(MethodSymbol methodSymbol, Expression? instance, ISourceLocation? location)
    {
        if (!methodSymbol.IsStatic)
        {
            if (instance == null)
            {
                this.Emitter.EmitThrowAndReport(new Diagnostic($"Non-static method '{methodSymbol.Name}' has no instance value.").WithLocation(location));
            }
            else if (methodSymbol.DeclaringSymbol is TypeSymbol declaringType)
            {
                EmitExpressionAsType(instance, declaringType);
            }
            else
            {
                this.Emitter.EmitThrowAndReport(new Diagnostic($"Non-static method '{methodSymbol.Name}' has no declaring type.").WithLocation(location));
            }
        }
    }

    /// <summary>
    /// Emits a list of arguments corresponding with a list of parameters.
    /// </summary>
    protected virtual void EmitArguments(ImmutableList<ParameterSymbol> parameters, ImmutableList<Expression> arguments)
    {
        for (int i = 0; i < arguments.Count; i++)
        {
            EmitArgument(parameters[i], arguments[i]);
        }
    }

    /// <summary>
    /// Emits a single argument corresponding to a parameter.
    /// </summary>
    protected virtual void EmitArgument(ParameterSymbol parameter, Expression argument)
    {
        EmitExpressionAsType(argument, parameter.Type);
    }

    #endregion

    #region Condition Emit

    protected virtual void EmitCondition(ConditionExpression condition, bool asAddress)
    {
        var whenFalseLabel = new LabelSymbol("whenFalse");
        var endLabel = new LabelSymbol("conditionEnd");

        EmitExpressionAsType(condition.Test, this.ExternalSymbols.Boolean);
        this.Emitter.EmitBranchFalse(whenFalseLabel);

        if (asAddress)
            EmitExpression(condition.WhenTrue, asAddress);
        else
            EmitExpressionAsType(condition.WhenTrue, condition.ResultType);

        this.Emitter.EmitBranch(endLabel);

        this.Emitter.MarkLabel(whenFalseLabel);

        if (asAddress)
            EmitExpression(condition.WhenFalse, asAddress);
        else
            EmitExpressionAsType(condition.WhenFalse, condition.ResultType);

        this.Emitter.MarkLabel(endLabel);
    }

    #endregion

    #region Constant Emit
    /// <summary>
    /// Emits <see cref="ConstantExpectedAttribute"/>
    /// </summary>
    protected virtual void EmitConstant(ConstantExpression constant)
    {
        var valueType = EmitValue(constant.Value, constant.Location);
        this.Emitter.EmitConvert(valueType, constant.ResultType, isChecked: false);
    }

    private TypeSymbol EmitValue(object? value, ISourceLocation? location)
    {
        switch (value)
        {
            case null:
                this.Emitter.EmitLoadNull();
                return this.ExternalSymbols.Object;
            case bool boolval:
                this.Emitter.EmitLoadBool(boolval);
                return this.ExternalSymbols.Boolean;
            case byte byteval:
                this.Emitter.EmitLoadByte(byteval);
                return this.ExternalSymbols.Byte;
            case sbyte sbyteval:
                this.Emitter.EmitLoadSByte(sbyteval);
                return this.ExternalSymbols.SByte;
            case short int16val:
                this.Emitter.EmitLoadInt16(int16val);
                return this.ExternalSymbols.Int16;
            case ushort uint16val:
                this.Emitter.EmitLoadUInt16(uint16val);
                return this.ExternalSymbols.UInt16;
            case int int32val:
                this.Emitter.EmitLoadInt32(int32val);
                return this.ExternalSymbols.Int32;
            case uint uint32val:
                this.Emitter.EmitLoadUInt32(uint32val);
                return this.ExternalSymbols.UInt32;
            case long int64val:
                this.Emitter.EmitLoadInt64(int64val);
                return this.ExternalSymbols.Int64;
            case ulong uint64val:
                this.Emitter.EmitLoadUInt64(uint64val);
                return this.ExternalSymbols.UInt64;
            case float singleVal:
                this.Emitter.EmitLoadSingle(singleVal);
                return this.ExternalSymbols.Single;
            case double doubleVal:
                this.Emitter.EmitLoadDouble(doubleVal);
                return this.ExternalSymbols.Double;
            case decimal decimalVal:
                this.Emitter.EmitLoadDecimal(decimalVal);
                return this.ExternalSymbols.Decimal;
            case string stringVal:
                this.Emitter.EmitLoadString(stringVal);
                return this.ExternalSymbols.String;
            case char charVal:
                this.Emitter.EmitLoadChar(charVal);
                return this.ExternalSymbols.Char;
            default:
                // some other object literal?
                this.Emitter.EmitThrowAndReport(new Diagnostic($"Cannot represent a value of type '{value.GetType().FullName}' in IL.").WithLocation(location));
                return this.ExternalSymbols.DoesNotReturn;
        }
    }

    private void EmitConvert(ConvertExpression convert)
    {
        if (convert.ConversionSymbol is MethodSymbol method)
        {
            // conversion symbol is a method, so just call it
            EmitMethodCall(method, null, ImmutableList<Expression>.Empty.Add(convert.Expression), convert.Location);
        }
        else
        {
            // otherwise conversion must be intrinsic
            EmitExpression(convert.Expression);
            this.Emitter.EmitConvert(convert.Expression.ResultType, convert.ResultType, isChecked: false);
        }
    }

    #endregion

    #region Default Emit
    /// <summary>
    /// Emits <see cref="DefaultExpression"/>
    /// </summary>
    protected virtual void EmitDefault(DefaultExpression @default)
    {
        this.Emitter.EmitDefault(@default.ResultType);
    }
    #endregion

    #region Element Emit

    protected virtual void EmitElement(ElementExpression element)
    {
        if (element.IndexerSymbol != null)
        {
            if (element.IndexerSymbol.GetMethod != null)
            {
                EmitMethodCall(element.IndexerSymbol.GetMethod, element.Expression, element.Arguments, element.Location);
            }
            else
            {
                this.Emitter.EmitThrowAndReport(new Diagnostic($"Invalid indexer get method"));
            }
        }
        else if (element.Expression.ResultType is ArraySymbol array)
        {
            if (array.IsSZArray)
            {
                // array instance
                EmitExpression(element.Expression);
                // index
                EmitExpressionAsType(element.Arguments[0], this.ExternalSymbols.Int32);
                // load me
                this.Emitter.EmitLoadArrayElement(array.ElementType);
            }
            else
            {
                // use Array indexer API?
                throw new NotImplementedException();
            }
        }
    }

    #endregion

    #region IsType Emit
    public virtual void EmitIsType(IsTypeExpression isType)
    {
        if (isType.TypeSymbol != null)
        {
            // TODO: consider type parameters
            if (isType.Expression.ResultType.IsValueType)
            {
                // we statically know this.
                this.EmitExpression(isType.Expression);
                this.Emitter.EmitPop();
                var isAssignable = isType.Expression.ResultType.IsAssignableTo(isType.TypeSymbol);
                this.Emitter.EmitLoadBool(isAssignable);
                return;
            }
            else if (isType.Expression.ResultType == SpecialSymbols.Void)
            {
                this.EmitExpression(isType.Expression);
                this.Emitter.EmitLoadBool(isType.TypeSymbol == SpecialSymbols.Void);
            }
            else 
            {
                this.EmitExpression(isType.Expression);

                var isAssignable = isType.Expression.ResultType.IsAssignableTo(isType.TypeSymbol);
                if (isAssignable)
                {
                    // statically known
                    this.Emitter.EmitPop();
                    this.Emitter.EmitLoadBool(true);
                }
                else
                {
                    this.Emitter.EmitAsType(isType.TypeSymbol);
                    this.Emitter.EmitLoadNull();
                    this.Emitter.EmitNotEqual(isType.TypeSymbol);
                }
            }
        }
        else
        {
            this.Emitter.EmitLoadBool(false);
        }
    }
    #endregion

    #region Label Emit
    /// <summary>
    /// Emits <see cref="LabelExpression"/>
    /// </summary>
    protected virtual void EmitLabel(LabelExpression label)
    {
        if (label.LabelSymbol != null)
        {
            this.Emitter.MarkLabel(label.LabelSymbol);
        }
    }
    #endregion

    #region Loop Emit
    /// <summary>
    /// Emits <see cref="LoopExpression"/>
    /// </summary>
    protected virtual void EmitLoop(LoopExpression loop)
    {
        var continueTarget = loop.ContinueTarget ?? new LabelSymbol("continue");
        this.Emitter.MarkLabel(continueTarget);
        EmitExpressionAsType(loop.Body, loop.ResultType);
        this.Emitter.EmitBranch(continueTarget);
        if (loop.BreakTarget != null)
            this.Emitter.MarkLabel(loop.BreakTarget!);
    }
    #endregion

    #region MemberReference, NameReference, SymbolRefeference, AdjustedReference Emit

    protected virtual void EmitMember(MemberExpression member, bool asAddress)
    {
        if (member.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            EmitMemberReference(member.Instance, memberSymbol, asAddress, member.Location);
        }
    }

    protected virtual void EmitMemberReference(Expression instance, MemberSymbol memberSymbol, bool asAddress, ISourceLocation? location)
    {
        if (!memberSymbol.IsStatic)
        {
            EmitExpression(instance, asAddress: instance.ResultType.IsValueType);
        }

        EmitSymbolReference(memberSymbol, asAddress, location);
    }

    protected virtual void EmitAdjustedReferenceExpression(AdjustedReferenceExpression adjusted, bool asAddress)
    {
        if (adjusted.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            var instance = GetMemberInstance(adjusted);
            if (instance != null)
            {
                EmitMemberReference(instance, memberSymbol, asAddress, adjusted.Location);
            }
            else
            {
                EmitSymbolReference(memberSymbol, asAddress, adjusted.Location);
            }
        }
    }

    /// <summary>
    /// Drills down through <see cref="AdjustedReferenceExpression"/> to find the <see cref="MemberExpression"/> and return its instance.
    /// </summary>
    protected virtual Expression? GetMemberInstance(Expression expression)
    {
        switch (expression)
        {
            case MemberExpression member:
                return member.Instance;

            case AdjustedReferenceExpression filter:
                return GetMemberInstance(filter.ElementType);

            default:
                return null;
        }
    }

    protected virtual void EmitNameReference(NameExpression nameRef, bool asAddress)
    {
        // is this refering to an instance member? egads
        if (nameRef.ReferencedSymbol is MemberSymbol ms
            && !ms.IsStatic)
        {
            if (IsMemberOfThis(ms))
            {
                if (ms.DeclaringSymbol is TypeSymbol ts && ts.IsValueType)
                {
                    this.Emitter.EmitLoadInstanceAddress();
                }
                else
                {
                    this.Emitter.EmitLoadInstance();
                }
            }
            else
            {
                // no instance?
                this.Emitter.EmitDefault(ms.DeclaringType!);
            }
        }

        EmitSymbolReference(nameRef.ReferencedSymbol!, asAddress, nameRef.Location);
    }

    protected virtual void EmitSymbolReference(SymbolExpression symbolRef, bool asAddress)
    {
        if (symbolRef.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            EmitSymbolReference(memberSymbol, asAddress, symbolRef.Location);
        }
    }

    protected virtual void EmitSymbolReference(Symbol symbol, bool asAddress, ISourceLocation? location)
    {
        switch (symbol)
        {
            case VariableSymbol variableSymbol:
                if (asAddress)
                {
                    this.Emitter.EmitLoadVariableAddress(variableSymbol);
                }
                else
                {
                    this.Emitter.EmitLoadVariable(variableSymbol);
                }
                break;

            case ParameterSymbol parameterSymbol:
                if (asAddress)
                {
                    this.Emitter.EmitLoadParameterAddress(parameterSymbol);
                }
                else
                {
                    this.Emitter.EmitLoadParameter(parameterSymbol);
                }
                break;

            case FieldSymbol fieldSymbol:
                if (fieldSymbol.IsConstant)
                {
                    var valueType = this.EmitValue(fieldSymbol.ConstantValue, location);
                    this.Emitter.EmitConvert(valueType, fieldSymbol.Type, isChecked: false);

                    if (asAddress)
                    {
                        var tmp = new VariableSymbol("tmp", fieldSymbol.Type);
                        this.Emitter.EmitStoreVariable(tmp);
                        this.Emitter.EmitLoadVariableAddress(tmp);
                    }
                }
                else
                {
                    if (asAddress)
                    {
                        this.Emitter.EmitLoadFieldAddress(fieldSymbol);
                    }
                    else
                    {
                        this.Emitter.EmitLoadField(fieldSymbol);
                    }
                }
                break;

            case PropertySymbol propertySymbol:
                if (asAddress)
                {
                    // copy to local, pass address of local, assign back?
                    throw new NotImplementedException();
                }
                else if (propertySymbol.GetMethod != null)
                {
                    this.Emitter.EmitCall(propertySymbol.GetMethod);
                }
                else
                {
                    this.Emitter.EmitThrowAndReport(new Diagnostic($"Unbound property getter for '{symbol.FullName}'"));
                }
                break;

            default:
                this.Emitter.EmitThrowAndReport(new Diagnostic($"Reference to unsupported symbol type '{symbol.GetType().Name}' cannot be emitted into IL.").WithLocation(location));
                break;
        }
    }

    #endregion

    #region New, NewArraySize, NewArrayInit
    protected virtual void EmitNew(NewExpression @new)
    {
        if (@new.ConstructorSymbol != null)
        {
            EmitArguments(@new.ConstructorSymbol.Parameters, @new.Arguments);
            this.Emitter.EmitNew(@new.ConstructorSymbol);
        }
    }

    protected virtual void EmitNewArraySize(NewArraySizeExpression newArraySize)
    {
        EmitExpressionAsType(newArraySize.Size, this.ExternalSymbols.Int32);
        this.Emitter.EmitNewArray(newArraySize.ElementTypeSymbol!);
    }

    protected virtual void EmitNewArrayInit(NewArrayInitExpression newArrayInit)
    {
        this.Emitter.EmitLoadInt32(newArrayInit.Expressions.Count);

        var array = new VariableSymbol("array", newArrayInit.ResultType);
        this.Emitter.DeclareVariableStart(array);
        this.Emitter.EmitNewArray(newArrayInit.ElementTypeSymbol!);
        this.Emitter.EmitStoreVariable(array);

        for (int i = 0; i < newArrayInit.Expressions.Count; i++)
        {
            this.Emitter.EmitLoadVariable(array);  // array
            this.Emitter.EmitLoadInt32(i);         // index
            EmitExpressionAsType(newArrayInit.Expressions[i], newArrayInit.ElementTypeSymbol!);
            this.Emitter.EmitStoreArrayElement(newArrayInit.ElementTypeSymbol!);
        }

        if (newArrayInit.ResultType != SpecialSymbols.Void)
            this.Emitter.EmitLoadVariable(array);

        this.Emitter.DeclareVariableEnd(array);
    }
    #endregion

    #region Operator Emit
    protected virtual void EmitOperator(OperatorExpression opex)
    {
        if (opex.OperatorSymbol is OperatorSymbol opSymbol)
        {
            // if the operator is backed by a method 
            if (opSymbol.CheckedMethod != null || opSymbol.UncheckMethod != null)
            {
                var methodSymbol = this.IsChecked
                    ? opSymbol.CheckedMethod ?? opSymbol.UncheckMethod
                    : opSymbol.UncheckMethod ?? opSymbol.CheckedMethod;

                EmitMethodCall(methodSymbol!, null, opex.Arguments, opex.Location);
            }
            else
            {
                // must be an intrinsic operator
                EmitIntrinsicOperator(opSymbol, opex.Arguments, opex.Location);
            }
        }
        else if (opex.OperatorSymbol is MethodSymbol methodSymbol)
        {
            // operator expression was resolved to a method
            EmitMethodCall(methodSymbol, null, opex.Arguments, opex.Location);
        }
        else if (opex.OperatorSymbol != null)
        {
            this.Emitter.EmitThrowAndReport(new Diagnostic($"Invalid operator symbol of type '{opex.OperatorSymbol.GetType().Name}'").WithLocation(opex.Location));
        }
        else
        {
            this.Emitter.EmitThrowAndReport(new Diagnostic($"Unknown operator kind '{opex.Kind}'").WithLocation(opex.Location));
        }
    }

    /// <summary>
    /// Emits common operator expression
    /// </summary>
    protected virtual void EmitIntrinsicOperator(
        OperatorSymbol opsym,
        ImmutableList<Expression> arguments,
        ISourceLocation? location)
    {
        var operandType = opsym.Parameters[0].Type;
        switch (opsym.Kind)
        {
            case OperatorKind.Add:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitAdd(operandType, this.IsChecked);
                break;
            case OperatorKind.Subtract:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitSubtract(operandType, this.IsChecked);
                break;
            case OperatorKind.Divide:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitDivide(operandType);
                break;
            case OperatorKind.Multiply:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitMultiply(operandType, this.IsChecked);
                break;
            case OperatorKind.Remainder:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitRemainder(operandType);
                break;
            case OperatorKind.Negate:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitNegate(operandType);
                break;
            case OperatorKind.Increment:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitIncrement(operandType, this.IsChecked);
                break;
            case OperatorKind.Decrement:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitDecrement(operandType, this.IsChecked);
                break;
            case OperatorKind.BitwiseAnd:
            case OperatorKind.LogicalAnd:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitAnd();
                break;
            case OperatorKind.BitwiseOr:
            case OperatorKind.LogicalOr:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitOr();
                break;
            case OperatorKind.BitwiseXor:
            case OperatorKind.LogicalXor:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitXor();
                break;
            case OperatorKind.BitwiseNot:
            case OperatorKind.LogicalNot:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitNot();
                break;
            case OperatorKind.ShiftLeft:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitShiftLeft(operandType);
                break;
            case OperatorKind.ShiftRight:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitShiftRight(operandType);
                break;
            case OperatorKind.Equal:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitEqual(operandType);
                break;
            case OperatorKind.NotEqual:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitNotEqual(operandType);
                break;
            case OperatorKind.LessThan:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitLessThan(operandType);
                break;
            case OperatorKind.LessThanOrEqual:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitLessThanOrEqual(operandType);
                break;
            case OperatorKind.GreaterThan:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitGreaterThan(operandType);
                break;
            case OperatorKind.GreaterThanOrEqual:
                EmitArguments(opsym.Parameters, arguments);
                this.Emitter.EmitGreaterThanOrEqual(operandType);
                break;

            case OperatorKind.LogicalAndAlso:
                var andAlsoFalse = new LabelSymbol("andAlsoFalse");
                var andAlsoEnd = new LabelSymbol("andAlsoEnd");
                EmitArgument(opsym.Parameters[0], arguments[0]);
                this.Emitter.EmitBranchFalse(andAlsoFalse);
                EmitArgument(opsym.Parameters[1], arguments[1]);
                this.Emitter.EmitBranch(andAlsoEnd);
                this.Emitter.MarkLabel(andAlsoFalse);
                this.Emitter.EmitLoadBool(false);
                this.Emitter.MarkLabel(andAlsoEnd);
                break;

            case OperatorKind.LogicalOrElse:
                var orElseTrue = new LabelSymbol("orElseTrue");
                var orElseEnd = new LabelSymbol("orElseEnd");
                EmitArgument(opsym.Parameters[0], arguments[0]);
                this.Emitter.EmitBranchTrue(orElseTrue);
                EmitArgument(opsym.Parameters[1], arguments[1]);
                this.Emitter.EmitBranch(orElseEnd);
                this.Emitter.EmitLoadBool(true);
                this.Emitter.MarkLabel(orElseEnd);
                break;

            default:
                this.Emitter.EmitThrowAndReport(new Diagnostic($"Unhandled operator kind '{opsym.Kind}' in EmitOperator.").WithLocation(location));
                break;
        }
    }

    #endregion

    #region This Emit

    protected virtual void EmitThis(ThisExpression me, bool asAddress)
    {
        if (asAddress)
        {
            this.Emitter.EmitLoadInstanceAddress();
        }
        else
        {
            this.Emitter.EmitLoadInstance();
        }
    }

    #endregion

    #region TypeOf Emit

    private MethodSymbol? _getTypeFromHandleSymbol;

    protected virtual void EmitTypeOf(TypeOfExpression tof)
    {
        if (_getTypeFromHandleSymbol == null)
        {
            _getTypeFromHandleSymbol = this.Emitter.ExternalSymbols.Type.GetFirstMember("GetTypeFromHandle") as MethodSymbol;
        }

        if (tof.TypeSymbol != null && _getTypeFromHandleSymbol != null)
        {
            this.Emitter.EmitLoadToken(tof.TypeSymbol);
            this.Emitter.EmitCall(_getTypeFromHandleSymbol);
        }
        else
        {
            this.Emitter.EmitThrowAndReport(new Diagnostic($"Missing runtime method Type.GetTypeFromHandle"));
        }
    }

    #endregion

    #region Variable Emit
    /// <summary>
    /// Emits <see cref="VariableExpression"/> variable declaration
    /// </summary>
    protected virtual void EmitVariable(VariableExpression variable)
    {
        if (variable.VariableSymbol != null)
        {
            this.Emitter.DeclareVariableStart(variable.VariableSymbol);

            if (variable.Initializer != null)
            {
                EmitExpression(variable.Initializer);
            }
            else
            {
                this.Emitter.EmitDefault(variable.VariableSymbol.Type);
            }

            this.Emitter.EmitStoreVariable(variable.VariableSymbol);

            if (variable.ResultType != SpecialSymbols.Void)
            {
                this.Emitter.EmitLoadVariable(variable.VariableSymbol);
            }
        }
    }
    #endregion

    #region Void Emit
    protected virtual void EmitVoid(VoidExpression vex)
    {
        // do nothing.. it is void. :)
    }
    #endregion
}
