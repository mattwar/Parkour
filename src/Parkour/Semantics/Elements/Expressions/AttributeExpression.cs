namespace Parkour.Semantics;

using Symbols;

public class AttributeExpression : Expression
{
    /// <summary>
    /// The type of the attribute.
    /// </summary>
    public Expression Type { get; }

    /// <summary>
    /// The arguments to the attributes constructor, or named arguments referencing the attributes properties.
    /// </summary>
    public ImmutableList<Expression> Arguments { get; }

    /// <summary>
    /// The symbols and values associated with the attribute.
    /// </summary>
    public AttributeInfo? AttributeInfo { get; }

    private AttributeExpression(
        Expression type,
        ImmutableList<Expression> arguments,
        ISourceLocation? location,
        AttributeInfo? info,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(arguments),
            location,
            resultType,
            diagnostics)
    {
        this.Type = type;
        this.Arguments = arguments;
        this.AttributeInfo = info;
    }

    public AttributeExpression(
        Expression attributeType,
        ImmutableList<Expression> arguments,
        ISourceLocation? location)
        : this(attributeType, arguments, location, null, null, null)
    {
    }

    public override AttributeExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new AttributeExpression(
            this.Type,
            this.Arguments,
            location,
            this.AttributeInfo,
            this.ResultType,
            this.Diagnostics
            );

    public override AttributeExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new AttributeExpression(
            this.Type,
            this.Arguments,
            this.Location,
            this.AttributeInfo,
            this.ResultType,
            diagnostics
            );

    public override AttributeExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new AttributeExpression(
            this.Type,
            this.Arguments,
            this.Location,
            this.AttributeInfo,
            resultType,
            this.Diagnostics
            );

    public AttributeExpression WithType(Expression type) =>
        type == this.Type ? this :
        new AttributeExpression(
            type,
            this.Arguments,
            this.Location,
            this.AttributeInfo,
            this.ResultType,
            this.Diagnostics
            );

    public AttributeExpression WithArguments(ImmutableList<Expression> arguments) =>
        arguments == this.Arguments ? this :
        new AttributeExpression(
            this.Type,
            arguments,
            this.Location,
            this.AttributeInfo,
            this.ResultType,
            this.Diagnostics
            );

    public AttributeExpression WithAttributeInfo(AttributeInfo? info) =>
        info == this.AttributeInfo ? this :
        new AttributeExpression(
            this.Type,
            this.Arguments,
            this.Location,
            info,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 
        1 + this.Arguments.Count;

    public override SemanticElement? GetChild(int index)
    {
        if (index == 0)
            return this.Type;
        index--;
        if (index < this.Arguments.Count)
            return this.Arguments[index];
        return null;
    }

    public override AttributeExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var attrType = rewriter.Rewrite(this.Type);
        var args = rewriter.Rewrite(this.Arguments);
        return this.WithType(attrType!).WithArguments(args);
    }
}