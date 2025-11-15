namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Augments type expressions with array semantics.
/// </summary>
public class ArrayExpression : AugmentedReferenceExpression
{
    /// <summary>
    /// The element type of the array.
    /// </summary>
    public Expression ElementType { get; }

    /// <summary>
    /// The type of the array, determined by semantic analysis.
    /// </summary>
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

    /// <summary>
    /// Returns a new instance of <see cref="ArrayExpression"/> with the specified array type symbol.
    public ArrayExpression WithArrayTypeSymbol(TypeSymbol? arrayTypeSymbol) =>
        arrayTypeSymbol == this.ReferencedSymbol ? this :
        new ArrayExpression(
            this.ElementType,
            this.Location,
            arrayTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    /// <summary>
    /// Returns a new instance of <see cref="ArrayExpression"/> with the specified element type. 
    /// </summary>
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