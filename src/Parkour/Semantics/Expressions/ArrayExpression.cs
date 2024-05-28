namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Converts referenced types to an arrays of that type.
/// </summary>
public class ArrayExpression : AdjustedReferenceExpression
{
    public Expression ElementType { get; }
    public TypeSymbol? ArrayTypeSymbol { get; }

    public override Expression TypeOrMember => this.ElementType;
    public override Symbol? ReferencedSymbol => this.ArrayTypeSymbol;

    private ArrayExpression(
        Expression elementType,
        ISourceLocation? location,
        TypeSymbol? arrayTypeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | NotNullOrDiagnosticState(arrayTypeSymbol, diagnostics),
            location,
            resultType,
            diagnostics)
    {
        this.ElementType = elementType;
        this.ArrayTypeSymbol = arrayTypeSymbol;
    }

    public ArrayExpression(
        Expression elementType,
        ISourceLocation? location)
        : this(elementType, location, null, null, null)
    {
    }

    public override ArrayExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ArrayExpression(
            this.ElementType,
            location,
            this.ArrayTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override ArrayExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ArrayExpression(
            this.ElementType,
            this.Location,
            this.ArrayTypeSymbol,
            this.ResultType,
            diagnostics
            );

    public override ArrayExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ArrayExpression(
            this.ElementType,
            this.Location,
            this.ArrayTypeSymbol,
            resultType,
            this.Diagnostics
            );

    public ArrayExpression WithArrayTypeSymbol(TypeSymbol? arrayTypeSymbol) =>
        arrayTypeSymbol == this.ReferencedSymbol ? this :
        new ArrayExpression(
            this.ElementType,
            this.Location,
            arrayTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ArrayExpression WithElementType(Expression elementType) =>
        elementType == this.TypeOrMember ? this :
        new ArrayExpression(
            elementType,
            this.Location,
            this.ArrayTypeSymbol,
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
        var elementType = rewriter.Rewrite(this.ElementType);
        return this.WithElementType(elementType!);
    }
}