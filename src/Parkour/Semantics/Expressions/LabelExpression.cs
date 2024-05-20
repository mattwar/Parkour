namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Marks a location that a branch may target.
/// </summary>
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

    public override LabelExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new LabelExpression(
            this.Name,
            this.ReceivingType,
            location,
            this.LabelSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override LabelExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new LabelExpression(
            this.Name,
            this.ReceivingType,
            this.Location,
            this.LabelSymbol,
            this.ResultType,
            diagnostics
            );

    public override LabelExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new LabelExpression(
            this.Name,
            this.ReceivingType,
            this.Location,
            this.LabelSymbol,
            resultType,
            this.Diagnostics
            );

    public LabelExpression WithName(string name) =>
        name == this.Name ? this :
        new LabelExpression(
            name,
            this.ReceivingType,
            this.Location,
            this.LabelSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public LabelExpression WithReceivingType(Expression receivingType) =>
        receivingType == this.ReceivingType ? this :
        new LabelExpression(
            this.Name,
            receivingType,
            this.Location,
            this.LabelSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public LabelExpression WithLabelSymbol(LabelSymbol? symbol) =>
        symbol == this.LabelSymbol ? this :
        new LabelExpression(
            this.Name,
            this.ReceivingType,
            this.Location,
            symbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.ReceivingType,
            _ => null
        };

    public override LabelExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var recievingType = rewriter.Rewrite(this.ReceivingType);
        return this.WithReceivingType(recievingType!);
    }
}
