using System.Diagnostics.CodeAnalysis;

namespace Parkour.Emitting;
using Binding;
using Semantics;
using Symbols;

public abstract class SemanticEmitter
{
    public SemanticEmitter()
    {
    }

    protected virtual void Emit(
        SymbolEmitContext context)
    {
        DefineTypeSymbols(context, context.Binding.DeclarationSymbols);
        DefineMemberSymbols(context, context.Binding.DeclarationSymbols);
        EmitSymbolBodies(context, context.Binding.DeclarationSymbols);

        context.Emitter.EmitDefinedTypesAndMembers();
    }

    public class SymbolEmitContext
    {
        public SymbolCache Symbols { get; }
        public DeclarationBinding Binding { get; }
        public SymbolEmitter Emitter { get; }

        private readonly List<Diagnostic> _diagnostics;

        public SymbolEmitContext(
            SymbolCache symbols, 
            DeclarationBinding binding,
            SymbolEmitter emitter,
            List<Diagnostic> diagnostics
            )
        {
            Symbols = symbols;
            Binding = binding;
            Emitter = emitter;
            _diagnostics = diagnostics;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        public virtual ILEmitContext CreateILEmitContext(ILEmitter ilEmitter) =>
            new ILEmitContext(this.Symbols, ilEmitter);
    }

    public class ILEmitContext
    {
        public SymbolCache Symbols { get; }
        public ILEmitter Emitter { get; }

        public ILEmitContext(
            SymbolCache symbols,
            ILEmitter emitter)
        {
            Symbols = symbols;
            Emitter = emitter;
        }
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
                    EmitBody(context.CreateILEmitContext(ilEmitter), decl.Body, _symbol.ConstructedType, decl.ReturnLabel));
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
                    EmitBody(context.CreateILEmitContext(ilEmitter), decl.Body, _symbol.ReturnType, decl.ReturnLabel));
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

    /// <summary>
    /// Emits an expression.
    /// </summary>
    protected virtual void EmitExpression(ILEmitContext context, Expression expression)
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
            case BlockExpression block:
                EmitBlock(context, block);
                break;

            case BranchExpression branch:
                EmitBranch(context, branch);
                break;

            case CallExpression call:
                EmitCall(context, call);
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
            if (!methodSymbol.IsStatic)
            {
                var instance = GetCallInstance(call.Expression);
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

            EmitArguments(context, methodSymbol.Parameters, call.Arguments);

            context.Emitter.EmitCall(methodSymbol);
        }
    }

    /// <summary>
    /// Emits arguments and converts them to expected parameter types.
    /// </summary>
    protected virtual void EmitArguments(ILEmitContext context, ImmutableList<ParameterSymbol> parameters, ImmutableList<Expression> arguments)
    {
        for (int i = 0; i < arguments.Count; i++)
        {
            EmitExpressionAsType(context, arguments[i], parameters[i].ParameterType);
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
    /// <summary>
    /// Emits <see cref="ConstantExpectedAttribute"/>
    /// </summary>
    protected virtual void EmitConstant(ILEmitContext context, ConstantExpression constant)
    {
        EmitValue(context.Emitter, constant.Value);
    }

    private void EmitValue(ILEmitter emitter, object? value)
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

    #endregion
}