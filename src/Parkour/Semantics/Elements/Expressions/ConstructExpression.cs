namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Constructs a constructable type or member by giving it type arguments.
/// </summary>
public class ConstructExpression : AugmentedReferenceExpression
{
    public override Expression TypeOrMember { get; }
    public ImmutableList<Expression> TypeArguments { get; }
    public Symbol? ConstructedSymbol { get; }

    public override Symbol? ReferencedSymbol =>
        ConstructedSymbol;

    private ConstructExpression(
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

    public ConstructExpression(
        Expression typeOrMember,
        ImmutableList<Expression> typeArguments,
        ISourceLocation? location)
        : this(typeOrMember, typeArguments, location, null, null, null)
    {
    }

    public override ConstructExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ConstructExpression(
            this.TypeOrMember,
            this.TypeArguments,
            location,
            this.ConstructedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override ConstructExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ConstructExpression(
            this.TypeOrMember,
            this.TypeArguments,
            this.Location,
            this.ConstructedSymbol,
            this.ResultType,
            diagnostics
            );

    public override ConstructExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ConstructExpression(
            this.TypeOrMember,
            this.TypeArguments,
            this.Location,
            this.ConstructedSymbol,
            resultType,
            this.Diagnostics
            );

    public ConstructExpression WithTypeOrMember(Expression typeOrMember) =>
        typeOrMember == this.TypeOrMember ? this :
        new ConstructExpression(
            typeOrMember,
            this.TypeArguments,
            this.Location,
            this.ConstructedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ConstructExpression WithTypeArguments(ImmutableList<Expression> typeArguments) =>
        typeArguments == this.TypeArguments ? this :
        new ConstructExpression(
            this.TypeOrMember,
            typeArguments,
            this.Location,
            this.ConstructedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ConstructExpression WithConstructedSymbol(Symbol? constructedSymbol) =>
        constructedSymbol == this.ConstructedSymbol ? this :
        new ConstructExpression(
            this.TypeOrMember,
            this.TypeArguments,
            this.Location,
            constructedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount =>
        1 + this.TypeArguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.TypeOrMember,
            _ => this.TypeArguments[index - 1]
        };

    public override ConstructExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var typeOrMember = rewriter.Rewrite(this.TypeOrMember);
        var typeArguments = rewriter.Rewrite(this.TypeArguments);
        return this
            .WithTypeOrMember(typeOrMember!)
            .WithTypeArguments(typeArguments);
    }
}