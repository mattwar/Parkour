namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Used to associate an argument with a parameter in a call expression.
/// </summary>
public sealed class NamedArgumentExpression : Expression
{
    protected internal override string DebugText =>
        $"{GetType().Name}: {Name} = {Expression.DebugText}";

    public string Name { get; }
    public Expression Expression { get; }
    public Symbol? NamedSymbol { get; }

    private NamedArgumentExpression(
        string name,
        Expression expression,
        ISourceLocation? location,
        Symbol? namedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Name = name;
        this.Expression = expression;
        this.NamedSymbol = namedSymbol;
    }

    public NamedArgumentExpression(
        string name,
        Expression expression,
        ISourceLocation? location)
        : this(name, expression, location, null, null, null)
    {
    }

    public override NamedArgumentExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new NamedArgumentExpression(
            this.Name,
            this.Expression,
            location,
            this.NamedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override NamedArgumentExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new NamedArgumentExpression(
            this.Name,
            this.Expression,
            this.Location,
            this.NamedSymbol,
            this.ResultType,
            diagnostics
            );

    public override NamedArgumentExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new NamedArgumentExpression(
            this.Name,
            this.Expression,
            this.Location,
            this.NamedSymbol,
            resultType,
            this.Diagnostics
            );

    public NamedArgumentExpression WithName(string name) =>
        name == this.Name ? this :
        new NamedArgumentExpression(
            name,
            this.Expression,
            this.Location,
            this.NamedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NamedArgumentExpression WithExpression(Expression expression) =>
        expression == this.Expression ? this :
        new NamedArgumentExpression(
            this.Name,
            expression,
            this.Location,
            this.NamedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NamedArgumentExpression WithNamedSymbol(Symbol? namedSymbol) =>
        namedSymbol == this.NamedSymbol ? this :
        new NamedArgumentExpression(
            this.Name,
            this.Expression,
            this.Location,
            namedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1;
    
    public override SemanticElement? GetChild(int index) => 
        index == 0 ? this.Expression : null;

    public override NamedArgumentExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var expr = rewriter.Rewrite(this.Expression);
        return this.WithExpression(expr!);
    }
}