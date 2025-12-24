namespace Parkour.Semantics;

using Symbols;

public class TypeOfExpression : Expression
{
    protected internal override string DebugText =>
        $"{nameof(TypeOfExpression)}: {(TypeSymbol != null ? TypeSymbol.FullName : Type.DebugText)}";

    public Expression Type { get; }
    public TypeSymbol? TypeSymbol { get; }

    private TypeOfExpression(
        Expression type,
        ISourceLocation? location,
        TypeSymbol? typeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(type)
            | NotNullOrDiagnosticState(typeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Type = type;
        this.TypeSymbol = typeSymbol;
    }

    public TypeOfExpression(
        Expression type,
        ISourceLocation? location)
        : this(type, location, null, null, null)
    {
    }

    public override TypeOfExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new TypeOfExpression(
            this.Type,
            location,
            this.TypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override TypeOfExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new TypeOfExpression(
            this.Type,
            this.Location,
            this.TypeSymbol,
            this.ResultType,
            diagnostics
            );

    public override TypeOfExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new TypeOfExpression(
            this.Type,
            this.Location,
            this.TypeSymbol,
            resultType,
            this.Diagnostics
            );

    public TypeOfExpression WithType(Expression type) =>
        type == this.Type ? this :
        new TypeOfExpression(
            type,
            this.Location,
            this.TypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public TypeOfExpression WithTypeSymbol(TypeSymbol? typeSymbol) =>
        typeSymbol == this.TypeSymbol ? this :
        new TypeOfExpression(
            this.Type,
            this.Location,
            typeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index == 0 ? this.Type : null;

    public override TypeOfExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var type = rewriter.Rewrite(this.Type);
        return this.WithType(type!);
    }
}
