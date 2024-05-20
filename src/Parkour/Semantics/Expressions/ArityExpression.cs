namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Filters referenced symbols to only those matching the arity.
/// </summary>
public class ArityExpression : AdjustedReferenceExpression
{
    public override Expression TypeOrMember { get; }
    public int Arity { get; }
    public override Symbol? ReferencedSymbol { get; }

    public ArityExpression(
        Expression typeOrMember,
        int arity,
        ISourceLocation? location,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(typeOrMember)
            | NotNullOrDiagnosticState(referencedSymbol, diagnostics),
            location,
            resultType,
            diagnostics)
    {
        this.TypeOrMember = typeOrMember;
        this.Arity = arity;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override ArityExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ArityExpression(
            this.TypeOrMember,
            this.Arity,
            location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override ArityExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ArityExpression(
            this.TypeOrMember,
            this.Arity,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            diagnostics
            );

    public override ArityExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ArityExpression(
            this.TypeOrMember,
            this.Arity,
            this.Location,
            this.ReferencedSymbol,
            resultType,
            this.Diagnostics
            );

    public ArityExpression WithTypeOrMember(Expression typeOrMember) =>
        typeOrMember == this.TypeOrMember ? this :
        new ArityExpression(
            typeOrMember,
            this.Arity,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ArityExpression WithArity(int arity) =>
        arity == this.Arity ? this :
        new ArityExpression(
            this.TypeOrMember,
            arity,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ArityExpression WithReferencedSymbol(Symbol? referencedSymbol) =>
        referencedSymbol == this.ReferencedSymbol ? this :
        new ArityExpression(
            this.TypeOrMember,
            this.Arity,
            this.Location,
            referencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.TypeOrMember,
            _ => null
        };

    public override ArityExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var typeOrMember = rewriter.Rewrite(this.TypeOrMember);
        return this.WithTypeOrMember(typeOrMember!);
    }
}