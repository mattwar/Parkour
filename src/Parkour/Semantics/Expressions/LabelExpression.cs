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
    /// The default value the branch recieves when branched to without a value.
    /// </summary>
    public Expression? DefaultValue { get; }

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
            State(receivingType)
            | NotNullState(labelSymbol)
            | NotNullState(resultType),
            location,
            resultType, 
            diagnostics)
    {
        this.Name = name;
        this.ReceivingType = receivingType;
        this.LabelSymbol = labelSymbol;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.ReceivingType,
            _ => null
        };
}
