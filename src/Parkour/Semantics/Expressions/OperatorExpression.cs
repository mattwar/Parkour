namespace Parkour.Semantics;
using Symbols;

public class OperatorExpression : Expression
{
    public string Kind { get; }
    public ImmutableList<Expression> Arguments { get; }
    public Symbol? OperatorSymbol { get; }

    public OperatorExpression(
        string kind, 
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
        this.Kind = kind;
        this.Arguments = arguments;
        this.OperatorSymbol = operatorSymbol;
    }

    public override OperatorExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new OperatorExpression(
            this.Kind,
            this.Arguments,
            location,
            this.OperatorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override OperatorExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new OperatorExpression(
            this.Kind,
            this.Arguments,
            this.Location,
            this.OperatorSymbol,
            this.ResultType,
            diagnostics
            );

    public override OperatorExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new OperatorExpression(
            this.Kind,
            this.Arguments,
            this.Location,
            this.OperatorSymbol,
            resultType,
            this.Diagnostics
            );

    public OperatorExpression WithKind(string kind) =>
        kind == this.Kind ? this :
        new OperatorExpression(
            kind,
            this.Arguments,
            this.Location,
            this.OperatorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public OperatorExpression WithArguments(ImmutableList<Expression> arguments) =>
        arguments == this.Arguments ? this :
        new OperatorExpression(
            this.Kind,
            arguments,
            this.Location,
            this.OperatorSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public OperatorExpression WithOperatorSymbol(OperatorSymbol? operatorSymbol) =>
        operatorSymbol == this.OperatorSymbol ? this :
        new OperatorExpression(
            this.Kind,
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
