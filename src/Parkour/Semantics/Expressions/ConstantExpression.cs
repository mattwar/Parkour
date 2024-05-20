namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// A value known at compile time.
/// </summary>
public sealed class ConstantExpression : Expression
{
    public object? Value { get; }

    public ConstantExpression(
        object? value,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullState(resultType), 
            location,
            resultType, 
            diagnostics)
    {
        this.Value = value;
    }

    public override ConstantExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ConstantExpression(
            this.Value,
            location,
            this.ResultType,
            this.Diagnostics
            );

    public override ConstantExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ConstantExpression(
            this.Value,
            this.Location,
            this.ResultType,
            diagnostics
            );

    public override ConstantExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ConstantExpression(
            this.Value,
            this.Location,
            resultType,
            this.Diagnostics
            );

    public ConstantExpression WithValue(object? value) =>
        value == this.Value ? this :
        new ConstantExpression(
            value,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
    public override ConstantExpression RewriteChildren(SemanticRewriter rewriter) => this;
}