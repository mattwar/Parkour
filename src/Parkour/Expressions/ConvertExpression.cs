namespace Parkour.Expressions;
using Symbols;
using Syntax;

public sealed class ConvertExpression : Expression
{
    public ConversionKind Kind { get; }
    public Expression Expression { get; }
    public TypeSymbol ConvertedType { get; }
    public Symbol? ConversionSymbol { get; }

    public ConvertExpression(
        ConversionKind kind,
        Expression expression,
        TypeSymbol convertedType,
        Symbol? conversionSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            expression.State, 
            resultType ?? convertedType, 
            diagnostics,
            syntax)
    {
        this.Kind = kind;
        this.Expression = expression;
        this.ConvertedType = convertedType;
        this.ConversionSymbol = conversionSymbol;
    }
}

