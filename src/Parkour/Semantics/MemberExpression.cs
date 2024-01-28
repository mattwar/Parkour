using Parkour.Symbols;

namespace Parkour.Semantics;

public sealed class MemberExpression : Expression
{
    public Expression Expression { get; }
    public string Name { get; }
    public override Symbol? ReferencedSymbol { get; }

    public MemberExpression(
        Expression expression,
        string name,
        ISourceLocation? location,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression)
            | NotNullOrDiagnosticState(referencedSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Expression = expression;
        this.Name = name;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => null
        };
}

