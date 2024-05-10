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

    public override int ChildCount => 
        this.Arguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index < this.Arguments.Count ? this.Arguments[index] : null;
}
