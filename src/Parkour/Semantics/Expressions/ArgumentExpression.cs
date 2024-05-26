namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Used to annotate an argument in a call expression.
/// </summary>
public sealed class ArgumentExpression : Expression
{
    protected internal override string DebugText =>
        $"{GetType().Name}: {Name} = {Expression.DebugText}";

    public string? Name { get; }
    public BitSet<SymbolModifier> Modifiers { get; }
    public Expression Expression { get; }
    public Symbol? NamedSymbol { get; }

    private ArgumentExpression(
        string? name,
        BitSet<SymbolModifier> modifiers,
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
        this.Modifiers = modifiers;
        this.Expression = expression;
        this.NamedSymbol = namedSymbol;
    }

    public ArgumentExpression(
        string? name,
        Expression expression,
        ISourceLocation? location)
        : this(
              name, 
              SymbolModifier.None,
              expression, 
              location, 
              null, 
              null, 
              null)
    {
    }

    public override ArgumentExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ArgumentExpression(
            this.Name,
            this.Modifiers,
            this.Expression,
            location,
            this.NamedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override ArgumentExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ArgumentExpression(
            this.Name,
            this.Modifiers,
            this.Expression,
            this.Location,
            this.NamedSymbol,
            this.ResultType,
            diagnostics
            );

    public override ArgumentExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ArgumentExpression(
            this.Name,
            this.Modifiers,
            this.Expression,
            this.Location,
            this.NamedSymbol,
            resultType,
            this.Diagnostics
            );

    public ArgumentExpression WithName(string name) =>
        name == this.Name ? this :
        new ArgumentExpression(
            name,
            this.Modifiers,
            this.Expression,
            this.Location,
            this.NamedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ArgumentExpression WithModifiers(BitSet<SymbolModifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new ArgumentExpression(
            this.Name,
            modifiers,
            this.Expression,
            this.Location,
            this.NamedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ArgumentExpression WithExpression(Expression expression) =>
        expression == this.Expression ? this :
        new ArgumentExpression(
            this.Name,
            this.Modifiers,
            expression,
            this.Location,
            this.NamedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ArgumentExpression WithNamedSymbol(Symbol? namedSymbol) =>
        namedSymbol == this.NamedSymbol ? this :
        new ArgumentExpression(
            this.Name,
            this.Modifiers,
            this.Expression,
            this.Location,
            namedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1;
    
    public override SemanticElement? GetChild(int index) => 
        index == 0 ? this.Expression : null;

    public override ArgumentExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var expr = rewriter.Rewrite(this.Expression);
        return this.WithExpression(expr!);
    }
}