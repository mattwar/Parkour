namespace Parkour.Semantics;
using Symbols;
using System.Linq.Expressions;

/// <summary>
/// Puts the specified symbol and its members in scope.
/// </summary>
public class UsingDeclaration : Declaration
{
    public Expression Expression { get; }
    public AliasSymbol? AliasSymbol { get; }

    private UsingDeclaration(
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
        this.AliasSymbol = aliasSymbol;
    }

    public UsingDeclaration(
        string name,
        Expression expression,
        ISourceLocation? location)
        : this(name, expression, location, null, null)
    {
    }

    public override Symbol? Symbol => null;

    public override UsingDeclaration WithName(string name) =>
        name == this.Name ? this :
        new UsingDeclaration(
            name, 
            this.Expression, 
            this.Location,
            this.AliasSymbol,
            this.Diagnostics
            );

    public override UsingDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new UsingDeclaration(
            this.Name,
            this.Expression, 
            location, 
            this.AliasSymbol,
            this.Diagnostics
            );

    public override Declaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new UsingDeclaration(
            this.Name,
            this.Expression,
            this.Location,
            this.AliasSymbol,
            diagnostics
            );

    public UsingDeclaration WithExpression(Expression expression) =>
        expression == this.Expression ? this : 
        new UsingDeclaration(
            this.Name,
            expression,
            this.Location,
            this.AliasSymbol,
            this.Diagnostics
            );

    public UsingDeclaration WithAliasSymbol(AliasSymbol? aliasedSymbol) =>
        aliasedSymbol == this.AliasSymbol ? this :
        new UsingDeclaration(
            this.Name,
            this.Expression,
            this.Location,
            aliasedSymbol,
            this.Diagnostics
            );

    public override int ChildCount => 1;
    public override SemanticElement? GetChild(int index) =>
        index == 0 ? this.Expression : null;

    public override SemanticElement RewriteChildren(SemanticRewriter rewriter)
    {
        var expression = rewriter.Rewrite(this.Expression);
        return this.WithExpression(expression!);
    }
}
