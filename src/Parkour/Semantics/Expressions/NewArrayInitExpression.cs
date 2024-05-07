namespace Parkour.Semantics;

using Symbols;

public class NewArrayInitExpression : Expression
{
    public Expression? ElementType { get; }
    public ImmutableList<Expression> Expressions { get; }
    public TypeSymbol? ElementTypeSymbol { get; }

    public NewArrayInitExpression(
        Expression? elementType,
        ImmutableList<Expression> expressions,
        ISourceLocation? location,
        TypeSymbol? elementTypeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | CombineState(expressions)
            | NotNullOrDiagnosticState(elementTypeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        ElementType = elementType;
        Expressions = expressions;
        ElementTypeSymbol = elementTypeSymbol;
    }

    public override int ChildCount => 
        1 + Expressions.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.ElementType,
            _ => this.Expressions[index - 1]
        };
}
