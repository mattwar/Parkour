namespace Parkour.Semantics;
using Symbols;

public sealed class ConvertExpression : Expression
{
    public ConversionKind Kind { get; }
    public Expression Expression { get; }
    public Expression? ConvertedType { get; }
    public Symbol? ConversionSymbol { get; }

    public ConvertExpression(
        ConversionKind kind,
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
        this.Kind = kind;
        this.Expression = expression;
        this.ConvertedType = convertedType;
        this.ConversionSymbol = conversionSymbol;
    }

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            1 => this.ConvertedType,
            _ => null
        };
}

