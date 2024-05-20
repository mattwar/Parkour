namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Converts referenced types to an arrays of that type.
/// </summary>
public class ArrayExpression : AdjustedReferenceExpression
{
    public override Expression TypeOrMember { get; }
    public override Symbol? ReferencedSymbol { get; }

    public ArrayExpression(
        Expression elementType,
        ISourceLocation? location,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | NotNullOrDiagnosticState(referencedSymbol, diagnostics),
            location,
            resultType,
            diagnostics)
    {
        this.TypeOrMember = elementType;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override ArrayExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ArrayExpression(
            this.TypeOrMember,
            location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override ArrayExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ArrayExpression(
            this.TypeOrMember,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            diagnostics
            );

    public override ArrayExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ArrayExpression(
            this.TypeOrMember,
            this.Location,
            this.ReferencedSymbol,
            resultType,
            this.Diagnostics
            );

    public ArrayExpression WithReferencedSymbol(Symbol? referencedSymbol) =>
        referencedSymbol == this.ReferencedSymbol ? this :
        new ArrayExpression(
            this.TypeOrMember,
            this.Location,
            referencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ArrayExpression WithTypeOrMember(Expression typeOrMember) =>
        typeOrMember == this.TypeOrMember ? this :
        new ArrayExpression(
            typeOrMember,
            this.Location,
            this.ReferencedSymbol,
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

    public override ArrayExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var typeOrMember = rewriter.Rewrite(this.TypeOrMember);
        return this.WithTypeOrMember(typeOrMember!);
    }
}