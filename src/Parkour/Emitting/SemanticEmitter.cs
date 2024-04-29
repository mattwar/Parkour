using System.Diagnostics.CodeAnalysis;

namespace Parkour.Emitting;
using Binding;
using Semantics;
using Symbols;
using System;

/// <summary>
/// Emits bound <see cref="Declaration"/> and <see cref="Expression"/> into a <see cref="SymbolEmitter"/>
/// </summary>
public class SemanticEmitter
{
    public SemanticEmitter()
    {
    }

    public virtual void Emit(
        DeclarationBinding binding,
        SymbolEmitter symbolEmitter,
        List<Diagnostic> diagnostics
        )
    {
        var context = new SymbolEmitContext(binding, symbolEmitter, diagnostics);
        Emit(context);
    }

    protected virtual void Emit(SymbolEmitContext context)
    {
        DefineTypeSymbols(context, context.Binding.DeclaredSymbols);
        DefineMemberSymbols(context, context.Binding.DeclaredSymbols);
        EmitSymbolBodies(context, context.Binding.DeclaredSymbols);
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
                    EmitBody(context.CreateILEmitContext(symbol, ilEmitter), symbol, decl.Body, context.Symbols.Void, decl.ReturnLabel));
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
                      EmitBody(context.CreateILEmitContext(symbol, ilEmitter), symbol, decl.Body, _symbol.ReturnType, decl.ReturnLabel));
        }
    }

    /// <summary>
    /// Emits the body of a method or constructor
    /// </summary>
    protected virtual void EmitBody(
        ILEmitContext context, Symbol symbol, Expression body, TypeSymbol returnType, LabelSymbol? returnLabel)
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
            case AdjustedReferenceExpression are:
                EmitAdjustedReferenceExpression(context, are, asAddress);
                break;

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

            case ConvertExpression convert:
                EmitConvert(context, convert);
                break;

            case DefaultExpression @default:
                EmitDefault(context, @default);
                break;

            case ElementExpression element:
                EmitElement(context, element);
                break;

            case LabelExpression label:
                EmitLabel(context, label);
                break;

            case LoopExpression loop:
                EmitLoop(context, loop);
                break;

            case MemberExpression mex:
                EmitMember(context, mex, asAddress);
                break;

            case NameReferenceExpression nre:
                EmitNameReference(context, nre, asAddress);
                break;

            case NewExpression ne:
                EmitNew(context, ne);
                break;

            case NewArrayInitExpression newArrayInit:
                EmitNewArrayInit(context, newArrayInit);
                break;

            case NewArraySizeExpression newArraySize:
                EmitNewArraySize(context, newArraySize);
                break;

            case OperatorExpression opex:
                EmitOperator(context, opex);
                break;

            case SymbolReferenceExpression sre:
                EmitSymbolReference(context, sre, asAddress);
                break;

            case ThisExpression me:
                EmitThis(context, me, asAddress);
                break;

            case VariableExpression variable:
                EmitVariable(context, variable);
                break;

            case VoidExpression vex:
                break;

            default:
                context.Emitter.EmitThrowAndReport(new Diagnostic($"Could not emit IL for expression '{expression.GetType().Name}'").WithLocation(expression.Location));
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

    private static VariableSymbol CopyToTemporaryVariable(ILEmitContext context, TypeSymbol type)
    {
        var variable = new VariableSymbol("tmp", type);
        context.Emitter.DeclareVariableStart(variable);
        context.Emitter.EmitDup();
        context.Emitter.EmitStoreVariable(variable);
        return variable;
    }

    private static void RestoreFromTemporaryVariable(ILEmitContext context, VariableSymbol variable)
    {
        context.Emitter.EmitLoadVariable(variable);
        context.Emitter.DeclareVariableEnd(variable);
    }

    protected virtual void EmitAssign(ILEmitContext context, AssignExpression assign)
    {
        var targetSymbol = assign.Target.ReferencedSymbol;
        var targetMember = GetMemberExpression(assign.Target);
        VariableSymbol? temp = null;

        switch (targetSymbol)
        {
            case VariableSymbol variableSymbol:
                EmitExpressionAsType(context, assign.Source, variableSymbol.VariableType);
                context.Emitter.EmitStoreVariable(variableSymbol);
                if (assign.ResultType != SpecialSymbols.Void)
                    context.Emitter.EmitLoadVariable(variableSymbol);
                break;

            case ParameterSymbol parameterSymbol:
                EmitExpressionAsType(context, assign.Source, parameterSymbol.ParameterType);
                context.Emitter.EmitStoreParameter(parameterSymbol);
                if (assign.ResultType != SpecialSymbols.Void)
                    context.Emitter.EmitLoadParameter(parameterSymbol);
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
                        context.Emitter.EmitLoadInstance();
                    }
                    else
                    {
                        context.Emitter.EmitThrowAndReport(new Diagnostic($"Instance field '{fieldSymbol.FullName}' does not have instance.").WithLocation(assign.Location));
                    }
                }

                EmitExpressionAsType(context, assign.Source, fieldSymbol.FieldType);

                temp = (assign.ResultType != SpecialSymbols.Void)
                    ? CopyToTemporaryVariable(context, fieldSymbol.FieldType)
                    : null;

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
                            context.Emitter.EmitLoadInstance();
                        }
                        else
                        {
                            context.Emitter.EmitThrowAndReport(new Diagnostic($"Instance property '{propertySymbol.FullName}' does not have instance.").WithLocation(assign.Target.Location));
                        }
                    }

                    EmitExpressionAsType(context, assign.Source, propertySymbol.PropertyType);

                    temp = (assign.ResultType != SpecialSymbols.Void)
                        ? CopyToTemporaryVariable(context, propertySymbol.PropertyType)
                        : null;

                    context.Emitter.EmitCall(propertySymbol.SetMethod);
                }               
                break;

            default:
                if (assign.Target is ElementExpression ee)
                {
                    if (ee.Expression.ResultType is ArraySymbol array)
                    {
                        if (array.IsSZArray)
                        {
                            EmitExpression(context, ee.Expression); // the array
                            EmitExpressionAsType(context, ee.Arguments[0], context.Symbols.Int32); // the index
                            EmitExpressionAsType(context, assign.Source, array.ElementType);

                            temp = (assign.ResultType != SpecialSymbols.Void)
                                ? CopyToTemporaryVariable(context, array.ElementType)
                                : null;

                            context.Emitter.EmitStoreArrayElement(array.ElementType);
                        }
                    }
                    else if (ee.IndexerSymbol != null
                        && ee.IndexerSymbol.SetMethod != null)
                    {
                        //EmitMethodCall(context, ee.IndexerSymbol.SetMethod, ee.Expression, ee.Arguments.Add(assign.Source), ee.Location);
                        throw new NotImplementedException();
                    }
                }
                break;
        }

        if (temp != null)
        {
            RestoreFromTemporaryVariable(context, temp);
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
        if (call.CalledSymbol is MethodSymbol methodSymbol)
        {
            var instance = methodSymbol.IsStatic ? null : GetMemberInstance(call.Expression);
            EmitMethodCall(context, methodSymbol, instance, call.Arguments, call.Location);
        }
        else if (call.CalledSymbol != null)
        {
            context.Emitter.EmitThrowAndReport(new Diagnostic($"Unhandled called symbol type '{call.CalledSymbol.GetType().Name}' in EmitCall").WithLocation(call.Location));
        }
        else
        {
            context.Emitter.EmitThrowAndReport(new Diagnostic($"Unknown call symbol").WithLocation(call.Location));
        }
    }

    protected virtual void EmitMethodCall(
        ILEmitContext context, MethodSymbol methodSymbol, Expression? instance, ImmutableList<Expression> arguments, ISourceLocation? location)
    {
        if (!methodSymbol.IsStatic)
        {
            if (instance == null)
            {
                context.Emitter.EmitThrowAndReport(new Diagnostic($"Non-static method '{methodSymbol.Name}' has no instance value.").WithLocation(location));
            }
            else if (methodSymbol.DeclaringSymbol is TypeSymbol declaringType)
            {
                EmitExpressionAsType(context, instance, declaringType);
            }
            else
            {
                context.Emitter.EmitThrowAndReport(new Diagnostic($"Non-static method '{methodSymbol.Name}' has no declaring type.").WithLocation(location));
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
        EmitValue(context.Emitter, constant.Value, constant.Location);
    }

    private void EmitValue(SymbolILEmitter emitter, object? value, ISourceLocation? location)
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
                emitter.EmitThrowAndReport(new Diagnostic($"Cannot represent a value of type '{value.GetType().FullName}' in IL.").WithLocation(location));
                break;
        }
    }

    private void EmitConvert(ILEmitContext context, ConvertExpression convert)
    {
        if (convert.ConversionSymbol is MethodSymbol method)
        {
            // conversion symbol is a method, so just call it
            EmitMethodCall(context, method, null, ImmutableList<Expression>.Empty.Add(convert.Expression), convert.Location);
        }
        else
        {
            // otherwise conversion must be intrinsic
            EmitExpression(context, convert.Expression);
            context.Emitter.EmitConvert(convert.Expression.ResultType, convert.ResultType, isChecked: false);
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

    #region Element Emit

    protected virtual void EmitElement(ILEmitContext context, ElementExpression element)
    {
        EmitExpression(context, element.Expression);

        if (element.IndexerSymbol != null)
        {
            throw new NotImplementedException();
        }
        else if (element.Expression.ResultType is ArraySymbol array)
        {
            if (array.IsSZArray)
            {
                // emit index argument
                EmitExpressionAsType(context, element.Arguments[0], context.Symbols.Int32);
                context.Emitter.EmitLoadArrayElement(array.ElementType);
            }
            else
            {
                // use Array indexer API?
                throw new NotImplementedException();
            }
        }
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

    #region Loop Emit
    /// <summary>
    /// Emits <see cref="LoopExpression"/>
    /// </summary>
    protected virtual void EmitLoop(ILEmitContext context, LoopExpression loop)
    {
        var continueTarget = loop.ContinueTarget ?? new LabelSymbol("continue");
        context.Emitter.MarkLabel(continueTarget);
        EmitExpressionAsType(context, loop.Body, loop.ResultType);
        context.Emitter.EmitBranch(continueTarget);
        if (loop.BreakTarget != null)
            context.Emitter.MarkLabel(loop.BreakTarget!);
    }
    #endregion

    #region MemberReference, NameReference, SymbolRefeference, AdjustedReference Emit

    protected virtual void EmitMember(ILEmitContext context, MemberExpression member, bool asAddress)
    {
        if (member.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            EmitMemberReference(context, member.Instance, memberSymbol, asAddress, member.Location);
        }
    }

    protected virtual void EmitMemberReference(ILEmitContext context, Expression instance, MemberSymbol memberSymbol, bool asAddress, ISourceLocation? location)
    {
        if (!memberSymbol.IsStatic)
        {
            EmitExpression(context, instance, asAddress: instance.ResultType.IsValueType);
        }

        EmitSymbolReference(context, memberSymbol, asAddress, location);
    }

    protected virtual void EmitAdjustedReferenceExpression(ILEmitContext context, AdjustedReferenceExpression adjusted, bool asAddress)
    {
        if (adjusted.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            var instance = GetMemberInstance(adjusted);
            if (instance != null)
            {
                EmitMemberReference(context, instance, memberSymbol, asAddress, adjusted.Location);
            }
            else
            {
                EmitSymbolReference(context, memberSymbol, asAddress, adjusted.Location);
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
        // is this refering to an instance member? egads
        if (nameRef.ReferencedSymbol is MemberSymbol ms 
            && !ms.IsStatic)
        {
            if (IsMemberOfThis(context, ms))
            {
                if (ms.DeclaringSymbol is TypeSymbol ts && ts.IsValueType)
                {
                    context.Emitter.EmitLoadInstanceAddress();
                }
                else
                {
                    context.Emitter.EmitLoadInstance();
                }
            }
            else
            {
                // no instance?
                context.Emitter.EmitDefault(ms.DeclaringType!);
            }
        }

        EmitSymbolReference(context, nameRef.ReferencedSymbol!, asAddress, nameRef.Location);
    }

    protected virtual void EmitSymbolReference(ILEmitContext context, SymbolReferenceExpression symbolRef, bool asAddress)
    {
        if (symbolRef.ReferencedSymbol is MemberSymbol memberSymbol)
        {
            EmitSymbolReference(context, memberSymbol, asAddress, symbolRef.Location);
        }
    }

    protected virtual void EmitSymbolReference(ILEmitContext context, Symbol symbol, bool asAddress, ISourceLocation? location)
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

            case FieldSymbol fieldSymbol:
                if (asAddress)
                {
                    context.Emitter.EmitLoadFieldAddress(fieldSymbol);
                }
                else
                {
                    context.Emitter.EmitLoadField(fieldSymbol);
                }
                break;

            case PropertySymbol propertySymbol:
                if (asAddress)
                {
                    // copy to local, pass address of local, assign back?
                    throw new NotImplementedException();
                }
                else
                {
                    context.Emitter.EmitCall(propertySymbol.GetMethod!);
                }
                break;

            default:
                context.Emitter.EmitThrowAndReport(new Diagnostic($"Reference to symbol '{symbol.GetType().Name}' cannot be emitted into IL.").WithLocation(location));
                break;           
        }
    }

    #endregion

    #region New, NewArraySize, NewArrayInit
    protected virtual void EmitNew(ILEmitContext context, NewExpression @new)
    {
        if (@new.ConstructorSymbol != null)
        {
            EmitArguments(context, @new.ConstructorSymbol.Parameters, @new.Arguments);
            context.Emitter.EmitNew(@new.ConstructorSymbol);
        }
    }

    protected virtual void EmitNewArraySize(ILEmitContext context, NewArraySizeExpression newArraySize)
    {
        EmitExpressionAsType(context, newArraySize.Size, context.Symbols.Int32);
        context.Emitter.EmitNewArray(newArraySize.ElementTypeSymbol!);
    }

    protected virtual void EmitNewArrayInit(ILEmitContext context, NewArrayInitExpression newArrayInit)
    {
        context.Emitter.EmitLoadInt32(newArrayInit.Expressions.Count);

        var array = new VariableSymbol("array", newArrayInit.ResultType);
        context.Emitter.DeclareVariableStart(array);
        context.Emitter.EmitNewArray(newArrayInit.ElementTypeSymbol!);
        context.Emitter.EmitStoreVariable(array);

        for (int i = 0; i < newArrayInit.Expressions.Count; i++)
        {
            context.Emitter.EmitLoadVariable(array);  // array
            context.Emitter.EmitLoadInt32(i);         // index
            EmitExpressionAsType(context, newArrayInit.Expressions[i], newArrayInit.ElementTypeSymbol!);
            context.Emitter.EmitStoreArrayElement(newArrayInit.ElementTypeSymbol!);
        }

        if (newArrayInit.ResultType != SpecialSymbols.Void)
            context.Emitter.EmitLoadVariable(array);

        context.Emitter.DeclareVariableEnd(array);
    }
    #endregion

    #region Operator Emit
    protected virtual void EmitOperator(ILEmitContext context, OperatorExpression opex)
    {
        if (opex.OperatorSymbol is OperatorSymbol opSymbol)
        {
            // if the operator is backed by a method 
            if (opSymbol.CheckedMethod != null || opSymbol.UncheckMethod != null)
            {
                var methodSymbol = context.IsChecked
                    ? opSymbol.CheckedMethod ?? opSymbol.UncheckMethod
                    : opSymbol.UncheckMethod ?? opSymbol.CheckedMethod;

                EmitMethodCall(context, methodSymbol!, null, opex.Arguments, opex.Location);
            }
            else
            {
                // must be an intrinsic operator
                EmitIntrinsicOperator(context, opSymbol, opex.Arguments, isChecked: context.IsChecked, opex.Location);
            }
        }
        else if (opex.OperatorSymbol is MethodSymbol methodSymbol)
        {
            // operator expression was resolved to a method
            EmitMethodCall(context, methodSymbol, null, opex.Arguments, opex.Location);
        }
        else if (opex.OperatorSymbol != null)
        {
            context.Emitter.EmitThrowAndReport(new Diagnostic($"Invalid operator symbol of type '{opex.OperatorSymbol.GetType().Name}'").WithLocation(opex.Location));
        }
        else
        {
            context.Emitter.EmitThrowAndReport(new Diagnostic($"Unknown operator kind '{opex.Kind}'").WithLocation(opex.Location));
        }
    }

    /// <summary>
    /// Emits common operator expression
    /// </summary>
    protected virtual void EmitIntrinsicOperator(
        ILEmitContext context,
        OperatorSymbol opsym,
        ImmutableList<Expression> arguments,
        bool isChecked,
        ISourceLocation? location)
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
                context.Emitter.EmitThrowAndReport(new Diagnostic($"Unhandled operator kind '{opsym.Kind}' in EmitOperator.").WithLocation(location));
                break;
        }
    }

    #endregion

    #region This Emit

    protected virtual void EmitThis(ILEmitContext context, ThisExpression me, bool asAddress)
    {
        if (asAddress)
        {
            context.Emitter.EmitLoadInstanceAddress();
        }
        else
        {
            context.Emitter.EmitLoadInstance();
        }
    }

    #endregion

    #region Variable Emit
    /// <summary>
    /// Emits <see cref="VariableExpression"/> variable declaration
    /// </summary>
    protected virtual void EmitVariable(ILEmitContext context, VariableExpression variable)
    {
        if (variable.VariableSymbol != null)
        {
            context.Emitter.DeclareVariableStart(variable.VariableSymbol);

            if (variable.Initializer != null)
            {
                EmitExpression(context, variable.Initializer);
            }
            else
            {
                context.Emitter.EmitDefault(variable.VariableSymbol.VariableType);
            }

            context.Emitter.EmitStoreVariable(variable.VariableSymbol);

            if (variable.ResultType != SpecialSymbols.Void)
            {
                context.Emitter.EmitLoadVariable(variable.VariableSymbol);
            }
        }
    }
    #endregion

    #region Void Emit
    protected virtual void EmitVoid(ILEmitContext context, VoidExpression vex)
    {
        // do nothing.. it is void. :)
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
        Symbols = SymbolCache.From(binding.ExternalAndDeclaredSymbols);
        Binding = binding;
        Emitter = emitter;
        _diagnostics = diagnostics;
    }

    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    public virtual ILEmitContext CreateILEmitContext(MemberSymbol symbol, SymbolILEmitter ilEmitter) =>
        new ILEmitContext(this.Symbols, ilEmitter, symbol, isChecked: true);
}

public class ILEmitContext
{
    /// <summary>
    /// The <see cref="SymbolCache"/> that can be used to access all type and member symbols
    /// in the global namespace.
    /// </summary>
    public SymbolCache Symbols { get; }

    /// <summary>
    /// The IL emitter.
    /// </summary>
    public SymbolILEmitter Emitter { get; }

    /// <summary>
    /// The member that is currently being emitted.
    /// </summary>
    public MemberSymbol CurrentMember { get; }

    /// <summary>
    /// True if the code is being emitted in a checked context.
    /// </summary>
    public bool IsChecked { get; }

    public ILEmitContext(
        SymbolCache symbols,
        SymbolILEmitter emitter,
        MemberSymbol current,
        bool isChecked)
    {
        Symbols = symbols;
        Emitter = emitter;
        CurrentMember = current;
        IsChecked = isChecked;
    }

    public ILEmitContext WithIsChecked(bool isChecked)
    {
        if (isChecked == this.IsChecked)
            return this;
        return new ILEmitContext(Symbols, Emitter, CurrentMember, isChecked);
    }
}
