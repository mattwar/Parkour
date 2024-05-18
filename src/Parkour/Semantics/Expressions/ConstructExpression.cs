namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Constructs a constructable type or member by giving it type arguments.
/// </summary>
public class ConstructExpression : AdjustedReferenceExpression
{
    public override Expression TypeOrMember { get; }
    public ImmutableList<Expression> TypeArguments { get; }
    public Symbol? ConstructedSymbol { get; }

    public ConstructExpression(
        Expression typeOrMember,
        ImmutableList<Expression> typeArguments,
        ISourceLocation? location,
        Symbol? constructedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(typeOrMember)
            | CombineState(typeArguments)
            | NotNullOrDiagnosticState(constructedSymbol, diagnostics),
            location,
            resultType,
            diagnostics)
    {
        this.TypeOrMember = typeOrMember;
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
            0 => this.TypeOrMember,
            _ => this.TypeArguments[index - 1]
        };
}