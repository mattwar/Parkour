namespace Parkour.Semantics;
using Symbols;

public class NewExpression : Expression
{
    public Expression? TypeExpression { get; }
    public ImmutableList<Expression> Arguments { get; }
    public ConstructorSymbol? ConstructorSymbol { get; }

    public NewExpression(
        Expression? typeExpression,
        ImmutableList<Expression>? arguments,
        ISourceLocation? location,
        ConstructorSymbol? constructorSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            OptionalState(typeExpression)
            | CombineState(arguments)
            | NotNullOrDiagnosticState(constructorSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.TypeExpression = typeExpression;
        this.Arguments = arguments ?? ImmutableList<Expression>.Empty;
        this.ConstructorSymbol = constructorSymbol;
    }

    public override int ChildCount => 
        1 + Arguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.TypeExpression,
            _ => this.Arguments[index - 1]
        };
}
