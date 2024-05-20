namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Converts the value produced by the expression to the specified type.
/// </summary>
public class ConvertExpression : Expression
{
    public Expression Expression { get; }
    public Expression? ConvertedType { get; }
    public Symbol? ConversionSymbol { get; }

    public ConvertExpression(
        Expression expression,
        Expression? convertedType,
        ISourceLocation? location,
        Symbol? conversionSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression)
            | State(convertedType)
            | NotNullState(resultType), 
            location,
            resultType, 
            diagnostics)
    {
        this.Expression = expression;
        this.ConvertedType = convertedType;
        this.ConversionSymbol = conversionSymbol;
    }

    public override ConvertExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ConvertExpression(
            this.Expression,
            this.ConvertedType,
            location,
            this.ConversionSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override ConvertExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ConvertExpression(
            this.Expression,
            this.ConvertedType,
            this.Location,
            this.ConversionSymbol,
            this.ResultType,
            diagnostics
            );

    public override ConvertExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ConvertExpression(
            this.Expression,
            this.ConvertedType,
            this.Location,
            this.ConversionSymbol,
            resultType,
            this.Diagnostics
            );

    public ConvertExpression WithExpression(Expression expression) =>
        expression == this.Expression ? this :
        new ConvertExpression(
            expression,
            this.ConvertedType,
            this.Location,
            this.ConversionSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ConvertExpression WithConvertedType(Expression convertedType) =>
        convertedType == this.ConvertedType ? this :
        new ConvertExpression(
            this.Expression,
            convertedType,
            this.Location,
            this.ConversionSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ConvertExpression WithConversionSymbol(Symbol? conversionSymbol) =>
        conversionSymbol == this.ConversionSymbol ? this :
        new ConvertExpression(
            this.Expression,
            this.ConvertedType,
            this.Location,
            conversionSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            1 => this.ConvertedType,
            _ => null
        };

    public override ConvertExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var expression = rewriter.Rewrite(this.Expression);
        var convertedType = rewriter.Rewrite(this.ConvertedType);
        return this
            .WithExpression(expression!)
            .WithConvertedType(convertedType!);
    }
}
