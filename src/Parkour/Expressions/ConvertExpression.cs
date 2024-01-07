namespace Parkour.Expressions;
using Symbols;

public sealed class ConvertExpression : Expression
{
    public ConversionKind Kind { get; }
    public Expression Expression { get; }
    public TypeSymbol ConvertedType { get; }
    public Symbol? Operator { get; }

    public ConvertExpression(
        ConversionKind kind,
        Expression expression,
        TypeSymbol convertedType,
        Symbol? @operator = null,
        TypeSymbol? resultType = null,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(expression.State, resultType ?? convertedType, diagnostics)
    {
        this.Kind = kind;
        this.Expression = expression;
        this.ConvertedType = convertedType;
        this.Operator = @operator;
    }
}

