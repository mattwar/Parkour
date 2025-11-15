namespace Parkour.Semantics;

using Parkour;
using Symbols;

/// <summary>
/// An operator invocation.
/// </summary>
public class OperatorExpression : Expression
{
    protected internal override string DebugText =>
        $"{nameof(OperatorExpression)}: {Operator}";

    /// <summary>
    /// The operator being invoked.
    /// </summary>
    public Operator Operator { get; }

    /// <summary>
    /// The arguments to the operator.
    /// </summary>
    public ImmutableList<Expression> Arguments { get; }

    /// <summary>
    /// The <see cref="Symbol"/> for the operator, determined during semantic analysis.
    /// </summary>
    public Symbol? OperatorSymbol { get; }

    private OperatorExpression(
        Operator op, 
        ImmutableList<Expression> arguments,
        ISourceLocation? location,
        Symbol? operatorSymbol,
        TypeSymbol? resultType, 
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(arguments)
            | NotNullOrDiagnosticState(operatorSymbol, diagnostics)
            | NotNullState(resultType), 
            location,
            resultType, 
            diagnostics)
    {
        this.Operator = op;
        this.Arguments = arguments;
        this.OperatorSymbol = operatorSymbol;
    }

    public OperatorExpression(
        Operator op,
        ImmutableList<Expression> arguments,
        ISourceLocation? location)
        : this(op, arguments, location, null, null, null)
    {
    }

    public override OperatorExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new OperatorExpression(
            this.Operator,
            this.Arguments,
            location,
            this.OperatorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override OperatorExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new OperatorExpression(
            this.Operator,
            this.Arguments,
            this.Location,
            this.OperatorSymbol,
            this.ResultType,
            diagnostics
            );

    public override OperatorExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new OperatorExpression(
            this.Operator,
            this.Arguments,
            this.Location,
            this.OperatorSymbol,
            resultType,
            this.Diagnostics
            );

    public OperatorExpression WithOperator(Operator op) =>
        op == this.Operator ? this :
        new OperatorExpression(
            op,
            this.Arguments,
            this.Location,
            this.OperatorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public OperatorExpression WithArguments(ImmutableList<Expression> arguments) =>
        arguments == this.Arguments ? this :
        new OperatorExpression(
            this.Operator,
            arguments,
            this.Location,
            this.OperatorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public OperatorExpression WithOperatorSymbol(Symbol? operatorSymbol) =>
        operatorSymbol == this.OperatorSymbol ? this :
        new OperatorExpression(
            this.Operator,
            this.Arguments,
            this.Location,
            operatorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount =>
        this.Arguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index < this.Arguments.Count ? this.Arguments[index] : null;

    public override OperatorExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var arguments = rewriter.Rewrite(this.Arguments);
        return this.WithArguments(arguments);
    }
}
