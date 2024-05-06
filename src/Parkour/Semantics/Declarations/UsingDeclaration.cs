namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Puts the specified symbol and its members in scope.
/// </summary>
public class UsingDeclaration : Declaration
{
    public Expression Expression { get; }
    public AliasSymbol? AliasedSymbol { get; }

    public UsingDeclaration(
        string name,
        Expression expression,
        ISourceLocation? location,
        AliasSymbol? aliasSymbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression),
            name,
            location,
            diagnostics)
    {
        this.Expression = expression;
        this.AliasedSymbol = aliasSymbol;
    }

    public override Symbol? Symbol => null;

    public override UsingDeclaration WithName(string name) =>
        new UsingDeclaration(
            name, 
            this.Expression, 
            this.Location,
            this.AliasedSymbol,
            this.Diagnostics
            );

    public override UsingDeclaration WithLocation(ISourceLocation? location) =>
        new UsingDeclaration(
            this.Name,
            this.Expression, 
            location, 
            this.AliasedSymbol,
            this.Diagnostics
            );

    public override Declaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        new UsingDeclaration(
            this.Name,
            this.Expression,
            this.Location,
            this.AliasedSymbol,
            diagnostics
            );

    public UsingDeclaration WithExpression(Expression expression) =>
        new UsingDeclaration(
            this.Name,
            expression,
            this.Location,
            this.AliasedSymbol,
            this.Diagnostics
            );

    public override int ChildCount => 1;
    public override SemanticElement? GetChild(int index) =>
        index == 0 ? this.Expression : null;
}
