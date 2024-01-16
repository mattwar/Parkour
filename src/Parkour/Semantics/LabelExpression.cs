namespace Parkour.Semantics;
using Symbols;

public class LabelExpression : Expression
{
    public string Name { get; }

    /// <summary>
    /// The type that the label receives via branch or flow.
    /// </summary>
    public Expression? ReceivingType { get; }

    /// <summary>
    /// The <see cref="Symbols.LabelSymbol"/> associated with this label.
    /// </summary>
    public LabelSymbol? LabelSymbol { get; }

    public LabelExpression(
        string name,
        Expression? receivingType,
        ISourceLocation? location,
        LabelSymbol? labelSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            ContainsState.None, 
            location,
            resultType ?? labelSymbol?.Type, 
            diagnostics)
    {
        this.Name = name;
        this.ReceivingType = receivingType;
        this.LabelSymbol = labelSymbol;
    }
}

