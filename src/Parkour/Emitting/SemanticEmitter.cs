using System.Diagnostics.CodeAnalysis;

namespace Parkour.Emitting;
using Binding;
using Semantics;
using Symbols;

/// <summary>
/// Emits bound <see cref="Declaration"/> and <see cref="Expression"/> into a <see cref="SymbolEmitter"/>
/// </summary>
public class SemanticEmitter
{
    public SemanticEmitter()
    {
    }

    public virtual void Emit(SymbolEmitContext context)
    {
        DefineTypeSymbols(context, context.Binding.DeclarationSymbols);
        DefineMemberSymbols(context, context.Binding.DeclarationSymbols);
        EmitSymbolBodies(context, context.Binding.DeclarationSymbols);

        context.Emitter.EmitDefinedTypesAndMembers();
    }

    #region Define Types
    /// <summary>
    /// Defines types for declared type symbols.
    /// </summary>
    protected virtual void DefineTypeSymbols(
        SymbolEmitContext context,
        Symbol symbol)
    {
        if (symbol is NamespaceSymbol ns)
        {
            foreach (var member in ns.Members)
            {
                DefineTypeSymbols(context, member);
            }
        }
        else if (symbol is TypeSymbol typeSymbol)
        {
            context.Emitter.DefineType(typeSymbol);

            foreach (var member in typeSymbol.Members)
            {
                DefineTypeSymbols(context, member);
            }
        }
    }

    /// <summary>
    /// Defines symbols for declared namespace and type members that are not types.
    /// This is done separately from defining types, since members may reference types.
    /// </summary>
    protected virtual void DefineMemberSymbols(
        SymbolEmitContext context,
        Symbol symbol)
    {
        switch (symbol)
        {
            case NamespaceSymbol namespaceSymbol:
                foreach (var nsMember in namespaceSymbol.Members)
                {
                    DefineMemberSymbols(context, nsMember);
                }
                break;

            case TypeSymbol typeSymbol:
                context.Emitter.DefineMember(typeSymbol);

                foreach (var typeMember in typeSymbol.Members)
                {
                    DefineMemberSymbols(context, typeMember);
                }
                break;

            case MemberSymbol memberSymbol:
                context.Emitter.DefineMember(memberSymbol);
                break;
        }
    }
    #endregion

    #region Emit Symbol Bodies
    protected virtual void EmitSymbolBodies(
        SymbolEmitContext context,
        Symbol symbol)
    {
        switch (symbol)
        {
            case ConstructorSymbol constructor:
                EmitConstructorBody(context, constructor);
                break;

            case MethodSymbol method:
                EmitMethodBody(context, method);
                break;

            case NamespaceSymbol ns:
                foreach (var member in ns.Members)
                {
                    EmitSymbolBodies(context, member);
                }
                break;

            case TypeSymbol ts:
                foreach (var member in ts.Members)
                {
                    EmitSymbolBodies(context, member);
                }
                break;
        }
    }

    protected virtual void EmitConstructorBody(
        SymbolEmitContext context,
        ConstructorSymbol symbol)
    {
        var decl = context.Binding.GetBoundSymbolDeclarations(symbol)
            .OfType<ConstructorDeclaration>()
            .FirstOrDefault();

        if (decl != null)
        {
            context.Emitter.EmitBody(
                symbol, 
                (_symbol, ilEmitter) => 
                    EmitBody(context.CreateILEmitContext(symbol, ilEmitter), decl.Body, _symbol.ConstructedType, decl.ReturnLabel));
        }
    }

    protected virtual void EmitMethodBody(
        SymbolEmitContext context,
        MethodSymbol symbol)
    {
        var decls = context.Binding.GetBoundSymbolDeclarations(symbol);
        var decl = decls
            .OfType<MethodDeclaration>()
            .FirstOrDefault();

        if (decl != null)
        {
            context.Emitter.EmitBody(
                symbol,
                (_symbol, ilEmitter) =>
                    EmitBody(context.CreateILEmitContext(symbol, ilEmitter), decl.Body, _symbol.ReturnType, decl.ReturnLabel));
        }
    }

    /// <summary>
    /// Emits the body of a method or constructor
    /// </summary>
    protected virtual void EmitBody(
        ILEmitContext context, Expression body, TypeSymbol returnType, LabelSymbol? returnLabel)
    {
        EmitExpression(context, body);
        var prevType = body.ResultType;

        if (returnLabel != null)
        {
            context.Emitter.EmitConvert(prevType, returnLabel.Type, true);
            context.Emitter.MarkLabel(returnLabel);
            prevType = returnLabel.Type;
        }

        context.Emitter.EmitConvert(prevType, returnType, true);
        context.Emitter.EmitReturn();
    }
    #endregion

    #region Emit Expressions
    /// <summary>
    /// Emits an expression.
    /// </summary>
    protected virtual void EmitExpression(ILEmitContext context, Expression expression, bool asAddress = false)
    {
        if (expression.Diagnostics.Count > 0)
        {
            var error = expression.Diagnostics.FirstOrDefault(e => e.Severity == DiagnosticSeverity.Error);
            if (error != null)
            {
                context.Emitter.EmitThrow(error.ToString());
                return;
            }
        }

        switch (expression)
        {
            case AssignExpression assign:
                EmitAssign(context, assign);
                break;

            case BlockExpression block:
                EmitBlock(context, block);
                break;

            case BranchExpression branch:
                EmitBranch(context, branch);
                break;

            case CallExpression call:
                EmitCall(context, call);
                break;

            case ConditionExpression condition:
                EmitCondition(context, condition, asAddress);
                break;

            case ConstantExpression constant:
                EmitConstant(context, constant);
                break;

            case DefaultExpression @default:
                EmitDefault(context, @default);
                break;

            case LabelExpression label:
                EmitLabel(context, label);
                break;

            case VariableExpression variable:
                EmitVariable(context, variable);
                break;

            case NameReferenceExpression nre:
                EmitNameReference(context, nre, asAddress);
                break;

            case SymbolReferenceExpression sre:
                EmitSymbolReference(context, sre, asAddress);
                break;

            case AdjustedReferenceExpression are:
                EmitAdjustedReferenceExpression(context, are, asAddress);
                break;

            case OperatorExpression opex:
                EmitOperator(context, opex);
                break;

            default:
                context.Emitter.EmitThrowAndReport($"Could not emit IL for expression '{expression.GetType().Name}'");
                break;
        }
    }

    /// <summary>
    /// Emits the expression and then adjusts the result on the stack as necessary to match the target type.
    /// </summary>
    protected virtual void EmitExpressionAsType(ILEmitContext context, Expression expression, TypeSymbol targetType)
    {
        EmitExpression(context, expression);
        if (expression.ResultType != targetType)
        {
            context.Emitter.EmitConvert(expression.ResultType, targetType, true);
        }
    }

    #region Assign Emit

    protected virtual void EmitAssign(ILEmitContext context, AssignExpression assign)
    {
        var targetSymbol = assign.Target.ReferencedSymbol;
        var targetMember = GetMemberExpression(assign.Target);

        switch (targetSymbol)
        {
            case VariableSymbol variableSymbol:
                EmitExpressionAsType(context, assign.Target, variableSymbol.VariableType);
                context.Emitter.EmitStoreVariable(variableSymbol);
                break;

            case ParameterSymbol parameterSymbol:
                EmitExpressionAsType(context, assign.Target, parameterSymbol.ParameterType);
                context.Emitter.EmitStoreParameter(parameterSymbol);
                break;

            case FieldSymbol fieldSymbol:
                if (!fieldSymbol.IsStatic)
                {
                    if (targetMember != null)
                    {
                        EmitExpression(context, targetMember.Instance, asAddress: targetMember.Instance.ResultType.IsValueType);
                    }
                    else if (IsMemberOfThis(context, fieldSymbol))
                    {
                        context.Emitter.EmitLoadInstance(fieldSymbol);
                    }
                    else
                    {
                        context.Emitter.EmitThrowAndReport($"Instance field '{fieldSymbol.FullName}' does not have instance.");
                    }
                }

                EmitExpressionAsType(context, assign.Target, fieldSymbol.FieldType);
                context.Emitter.EmitStoreField(fieldSymbol);
                break;

            case PropertySymbol propertySymbol:
                if (propertySymbol.SetMethod != null)
                {
                    if (!propertySymbol.IsStatic)
                    {
                        if (targetMember != null)
                        {
                            EmitExpression(context, targetMember.Instance, asAddress: targetMember.Instance.ResultType.IsValueType);
                        }
                        else if (IsMemberOfThis(context, propertySymbol))
                        {
                            context.Emitter.EmitLoadInstance(propertySymbol);
                        }
                        else
                        {
                            context.Emitter.EmitThrowAndReport($"Instance property '{propertySymbol.FullName}' does not have instance.");
                        }
                    }

                    EmitExpressionAsType(context, assign.Target, propertySymbol.PropertyType);

                    context.Emitter.EmitCall(propertySymbol.SetMethod);
                }               
                break;

            default:
                break;
        }
    }

    private static bool IsMemberOfThis(ILEmitContext context, MemberSymbol memberSymbol)
    {
        if (context.CurrentMember.DeclaringType is { } currentType
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
                return GetMemberExpression(filter.Expression);
            default:
                return null;
        }
    }

    #endregion

    #region Block Emit
    /// <summary>
    /// Emits <see cref="BlockExpression"/>
    /// </summary>
    protected virtual void EmitBlock(ILEmitContext context, BlockExpression block)
    {
        var variables = block.Expressions
            .OfType<VariableExpression>()
            .Select(v => v.VariableSymbol)
            .OfType<VariableSymbol>()
            .ToList();

        // declare variable starts
        foreach (var variable in variables)
        {
            context.Emitter.DeclareVariableStart(variable);
        }

        for (int i = 0; i < block.Expressions.Count; i++)
        {
            if (i < block.Expressions.Count - 1)
            {
                EmitExpressionAsType(context, block.Expressions[i], SpecialSymbols.Void);
            }
            else
            {
                EmitExpression(context, block.Expressions[i]);
            }
        }

        // declare variable ends
        foreach (var variable in variables)
        {
            context.Emitter.DeclareVariableEnd(variable);
        }
    }
    #endregion

    #region Branch Emit
    /// <summary>
    /// Emits <see cref="BranchExpression"/>
    /// </summary>
    protected virtual void EmitBranch(ILEmitContext context, BranchExpression branch)
    {
        if (branch.LabelSymbol != null)
        {
            if (branch.Expression != null)
            {
                EmitExpressionAsType(context, branch.Expression, branch.LabelSymbol.Type);
            }

            context.Emitter.EmitBranch(branch.LabelSymbol);
        }
    }
    #endregion

    #region Call Emit
    /// <summary>
    /// Emits <see cref="CallExpression"/>
    /// </summary>
    protected virtual void EmitCall(ILEmitContext context, CallExpression call)
    {
        // TODO: support lambda/delegates
        if (call.CalledSymbol is MethodSymbol methodSymbol)
        {
            var instance = methodSymbol.IsStatic ? null : GetMemberInstance(call.Expression);
            EmitMethodCall(context, methodSymbol, instance, call.Arguments);
        }
        else if (call.CalledSymbol != null)
        {
            throw new InvalidOperationException($"Invalid called symbol type '{call.CalledSymbol.GetType().Name}' in EmitCall");
        }
    }

    protected virtual void EmitMethodCall(ILEmitContext context, MethodSymbol methodSymbol, Expression? instance, ImmutableList<Expression> arguments)
    {
        if (!methodSymbol.IsStatic)
        {
            if (instance == null)
            {
                context.Emitter.EmitThrowAndReport($"Non-static method '{methodSymbol.Name}' has no instance value.");
            }
            else if (methodSymbol.DeclaringSymbol is TypeSymbol declaringType)
            {
                EmitExpressionAsType(context, instance, declaringType);
            }
            else
            {
                context.Emitter.EmitThrowAndReport($"Non-static method '{methodSymbol.Name}' has no declaring type.");
            }
        }

        EmitArguments(context, methodSymbol.Parameters, arguments);

        context.Emitter.EmitCall(methodSymbol);
    }

    /// <summary>
    /// Emits a list of arguments corresponding with a list of parameters.
    /// </summary>
    protected virtual void EmitArguments(ILEmitContext context, ImmutableList<ParameterSymbol> parameters, ImmutableList<Expression> arguments)
    {
        for (int i = 0; i < arguments.Count; i++)
        {
            EmitArgument(context, parameters[i], arguments[i]);
        }
    }

    /// <summary>
    /// Emits a single argument corresponding to a parameter.
    /// </summary>
    protected virtual void EmitArgument(ILEmitContext context, ParameterSymbol parameter, Expression argument)
    {
        EmitExpressionAsType(context, argument, parameter.ParameterType);
    }

    #endregion

    #region Condition Emit

    protected virtual void EmitCondition(ILEmitContext context, ConditionExpression condition, bool asAddress)
    {
        var whenFalseLabel = new LabelSymbol("whenFalse");
        var endLabel = new LabelSymbol("conditionEnd");

        EmitExpressionAsType(context, condition.Test, context.Symbols.Boolean);
        context.Emitter.EmitBranchFalse(whenFalseLabel);

        if (asAddress)
            EmitExpression(context, condition.WhenTrue, asAddress);
        else
            EmitExpressionAsType(context, condition.WhenTrue, condition.ResultType);

        context.Emitter.EmitBranch(endLabel);

        context.Emitter.MarkLabel(whenFalseLabel);

        if (asAddress)
            EmitExpression(context, condition.WhenFalse, asAddress);
        else
            EmitExpressionAsType(context, condition.WhenFalse, condition.ResultType);

        context.Emitter.MarkLabel(endLabel);
    }

    #endregion

    #region Constant Emit
    /// <summary>
    /// Emits <see cref="ConstantExpectedAttribute"/>
    /// </summary>
    protected virtual void EmitConstant(ILEmitContext context, ConstantExpression constant)
    {
        EmitValue(context.Emitter, constant.Value);
    }

    private void EmitValue(SymbolILEmitter emitter, object? value)
    {
        switch (value)
        {
            case null:
                emitter.EmitLoadNull();
                break;
            case bool boolval:
                emitter.EmitLoadBool(boolval);
                break;
            case byte byteval:
                emitter.EmitLoadByte(byteval);
                break;
            case sbyte sbyteval:
                emitter.EmitLoadSByte(sbyteval);
                break;
            case short int16val:
                emitter.EmitLoadInt16(int16val);
                break;
            case ushort uint16val:
                emitter.EmitLoadUInt16(uint16val);
                break;
            case int int32val:
                emitter.EmitLoadInt32(int32val);
                break;
            case uint uint32val:
                emitter.EmitLoadUInt32(uint32val);
                break;
            case long int64val:
                emitter.EmitLoadInt64(int64val);
                break;
            case ulong uint64val:
                emitter.EmitLoadUInt64(uint64val);
                break;
            case float singleVal:
                emitter.EmitLoadSingle(singleVal);
                break;
            case double doubleVal:
                emitter.EmitLoadDouble(doubleVal);
                break;
            case decimal decimalVal:
                emitter.EmitLoadDecimal(decimalVal);
                break;
            case string stringVal:
                emitter.EmitLoadString(stringVal);
                break;
            case char charVal:
                emitter.EmitLoadChar(charVal);
                break;
            default:
                // some other object literal?
                emitter.EmitThrowAndReport($"Cannot represent a value of type '{value.GetType().FullName}' as a constant.");
                break;
        }
    }
    #endregion

    #region Default Emit
    /// <summary>
    /// Emits <see cref="DefaultExpression"/>
    /// </summary>
    protected virtual void EmitDefault(ILEmitContext context, DefaultExpression @default)
    {
        context.Emitter.EmitDefault(@default.ResultType);
    }
    #endregion

    #region Label Emit
    /// <summary>
    /// Emits <see cref="LabelExpression"/>
    /// </summary>
    protected virtual void EmitLabel(ILEmitContext context, LabelExpression label)
    {
        if (label.LabelSymbol != null)
        {
            context.Emitter.MarkLabel(label.LabelSymbol);
        }
    }
    #endregion

    #region MemberReference, NameReference, SymbolRefeference, AdjustedReference Emit

    protected virtual void EmitMember(ILEmitContext context, MemberExpression member, bool asAddress)
    {
        if (member.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            EmitMemberReference(context, member.Instance, memberSymbol, asAddress);
        }
    }

    protected virtual void EmitMemberReference(ILEmitContext context, Expression instance, MemberSymbol memberSymbol, bool asAddress)
    {
        if (!memberSymbol.IsStatic)
        {
            EmitExpression(context, instance, asAddress: instance.ResultType.IsValueType);
        }

        EmitSymbolReference(context, memberSymbol, asAddress);
    }

    protected virtual void EmitAdjustedReferenceExpression(ILEmitContext context, AdjustedReferenceExpression adjusted, bool asAddress)
    {
        if (adjusted.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            var instance = GetMemberInstance(adjusted);
            if (instance != null)
            {
                EmitMemberReference(context, instance, memberSymbol, asAddress);
            }
            else
            {
                EmitSymbolReference(context, memberSymbol, asAddress);
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
                return GetMemberInstance(filter.Expression);

            default:
                return null;
        }
    }

    protected virtual void EmitNameReference(ILEmitContext context, NameReferenceExpression nameRef, bool asAddress)
    {
        if (nameRef.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            EmitSymbolReference(context, memberSymbol, asAddress);
        }
    }

    protected virtual void EmitSymbolReference(ILEmitContext context, SymbolReferenceExpression symbolRef, bool asAddress)
    {
        if (symbolRef.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            EmitSymbolReference(context, memberSymbol, asAddress);
        }
    }

    protected virtual void EmitSymbolReference(ILEmitContext context, Symbol symbol, bool asAddress)
    {
        switch (symbol)
        {
            case VariableSymbol variableSymbol:
                if (asAddress)
                {
                    context.Emitter.EmitLoadVariableAddress(variableSymbol);
                }
                else
                {
                    context.Emitter.EmitLoadVariable(variableSymbol);
                }
                break;

            case ParameterSymbol parameterSymbol:
                if (asAddress)
                {
                    context.Emitter.EmitLoadParameterAddress(parameterSymbol);
                }
                else
                {
                    context.Emitter.EmitLoadParameter(parameterSymbol);
                }
                break;

            default:
                context.Emitter.EmitThrowAndReport($"Reference to symbol type '{symbol.GetType().Name}' not supported in EmitSymbolReference");
                break;           
        }
    }

    #endregion

    #region Operator Emit
    protected virtual void EmitOperator(ILEmitContext context, OperatorExpression opex)
    {
        if (opex.OperatorSymbol is OperatorSymbol opSymbol)
        {
            EmitOperator(context, opSymbol, opex.Arguments, isChecked: true);
        }
        else if (opex.OperatorSymbol is MethodSymbol methodSymbol)
        {
            EmitMethodCall(context, methodSymbol, null, opex.Arguments);
        }
        else if (opex.OperatorSymbol != null)
        {
            throw new InvalidOperationException($"Invalid custom operator symbol type '{opex.OperatorSymbol.GetType().Name}' in EmitOperator");
        }
    }

    /// <summary>
    /// Emits common operator expression
    /// </summary>
    protected virtual void EmitOperator(
        ILEmitContext context,
        OperatorSymbol opsym,
        ImmutableList<Expression> arguments,
        bool isChecked)
    {
        var operandType = opsym.Parameters[0].ParameterType;
        switch (opsym.Kind)
        {
            case OperatorKind.Add:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitAdd(operandType, isChecked);
                break;
            case OperatorKind.Subtract:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitSubtract(operandType, isChecked);
                break;
            case OperatorKind.Divide:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitDivide(operandType);
                break;
            case OperatorKind.Multiply:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitMultiply(operandType, isChecked);
                break;
            case OperatorKind.Remainder:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitRemainder(operandType);
                break;
            case OperatorKind.Negate:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitNegate(operandType);
                break;
            case OperatorKind.Increment:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitIncrement(operandType, isChecked);
                break;
            case OperatorKind.Decrement:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitDecrement(operandType, isChecked);
                break;
            case OperatorKind.BitwiseAnd:
            case OperatorKind.LogicalAnd:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitAnd();
                break;
            case OperatorKind.BitwiseOr:
            case OperatorKind.LogicalOr:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitOr();
                break;
            case OperatorKind.BitwiseXor:
            case OperatorKind.LogicalXor:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitXor();
                break;
            case OperatorKind.BitwiseNot:
            case OperatorKind.LogicalNot:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitNot();
                break;
            case OperatorKind.ShiftLeft:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitShiftLeft(operandType);
                break;
            case OperatorKind.ShiftRight:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitShiftRight(operandType);
                break;
            case OperatorKind.Equal:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitEqual(operandType);
                break;
            case OperatorKind.NotEqual:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitNotEqual(operandType);
                break;
            case OperatorKind.LessThan:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitLessThan(operandType);
                break;
            case OperatorKind.LessThanOrEqual:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitLessThanOrEqual(operandType);
                break;
            case OperatorKind.GreaterThan:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitGreaterThan(operandType);
                break;
            case OperatorKind.GreaterThanOrEqual:
                EmitArguments(context, opsym.Parameters, arguments);
                context.Emitter.EmitGreaterThanOrEqual(operandType);
                break;

            case OperatorKind.LogicalAndAlso:
                var andAlsoFalse = new LabelSymbol("andAlsoFalse");
                var andAlsoEnd = new LabelSymbol("andAlsoEnd");
                EmitArgument(context, opsym.Parameters[0], arguments[0]);
                context.Emitter.EmitBranchFalse(andAlsoFalse);
                EmitArgument(context, opsym.Parameters[1], arguments[1]);
                context.Emitter.EmitBranch(andAlsoEnd);
                context.Emitter.MarkLabel(andAlsoFalse);
                context.Emitter.EmitLoadBool(false);
                context.Emitter.MarkLabel(andAlsoEnd);
                break;

            case OperatorKind.LogicalOrElse:
                var orElseTrue = new LabelSymbol("orElseTrue");
                var orElseEnd = new LabelSymbol("orElseEnd");
                EmitArgument(context, opsym.Parameters[0], arguments[0]);
                context.Emitter.EmitBranchTrue(orElseTrue);
                EmitArgument(context, opsym.Parameters[1], arguments[1]);
                context.Emitter.EmitBranch(orElseEnd);
                context.Emitter.EmitLoadBool(true);
                context.Emitter.MarkLabel(orElseEnd);
                break;

            default:
                context.Emitter.EmitThrowAndReport($"Unhandled operator kind '{opsym.Kind}' in EmitOperator.");
                break;
        }
    }

    #endregion

    #region Variable Emit
    /// <summary>
    /// Emits <see cref="VariableExpression"/>
    /// </summary>
    protected virtual void EmitVariable(ILEmitContext context, VariableExpression variable)
    {
        if (variable.VariableSymbol != null)
        {
            context.Emitter.DeclareVariableStart(variable.VariableSymbol);
        }
    }
    #endregion

    #endregion // expressions
}

public class SymbolEmitContext
{
    public SymbolCache Symbols { get; }
    public DeclarationBinding Binding { get; }
    public SymbolEmitter Emitter { get; }

    private readonly List<Diagnostic> _diagnostics;

    public SymbolEmitContext(
        DeclarationBinding binding,
        SymbolEmitter emitter,
        List<Diagnostic> diagnostics
        )
    {
        Symbols = SymbolCache.From(binding.GlobalNamespace);
        Binding = binding;
        Emitter = emitter;
        _diagnostics = diagnostics;
    }

    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    public virtual ILEmitContext CreateILEmitContext(MemberSymbol symbol, SymbolILEmitter ilEmitter) =>
        new ILEmitContext(this.Symbols, ilEmitter, symbol);
}

public class ILEmitContext
{
    public SymbolCache Symbols { get; }
    public SymbolILEmitter Emitter { get; }
    public MemberSymbol CurrentMember { get; }

    public ILEmitContext(
        SymbolCache symbols,
        SymbolILEmitter emitter,
        MemberSymbol current)
    {
        Symbols = symbols;
        Emitter = emitter;
        CurrentMember = current;
    }
}
