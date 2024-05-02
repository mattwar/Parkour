namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Constructs a type expression by giving it type arguments.
/// </summary>
public class ConstructExpression : AdjustedReferenceExpression
{
    public override Expression Expression { get; }
    public ImmutableList<Expression> TypeArguments { get; }
    public Symbol? ConstructedSymbol { get; }

    public ConstructExpression(
        Expression expression,
        ImmutableList<Expression> typeArguments,
        ISourceLocation? location,
        Symbol? constructedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression)
            | CombineState(typeArguments)
            | NotNullOrDiagnosticState(constructedSymbol, diagnostics),
            location,
            resultType,
            diagnostics)
    {
        this.Expression = expression;
        this.TypeArguments = typeArguments;
        this.ConstructedSymbol = constructedSymbol;
    }

    public override Symbol? ReferencedSymbol => 
        ConstructedSymbol;

    public override int ChildCount =>
        1 + this.TypeArguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => this.TypeArguments[index - 1]
        };
}