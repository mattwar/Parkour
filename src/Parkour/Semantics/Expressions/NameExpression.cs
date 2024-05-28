namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// References a named symbol or symbols in the current scope,
/// based on the lookup rules of the binder.
/// </summary>
public sealed class NameExpression : Expression
{
    internal protected override string DebugText => 
        $"{GetType().Name}: {this.Name}: {ReferencedSymbol?.FullName ?? "???"}";

    public string Name { get; }
    public override Symbol? ReferencedSymbol { get; }

    private NameExpression(
        string name,
        ISourceLocation? location,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullOrDiagnosticState(referencedSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Name = name;
        this.ReferencedSymbol = referencedSymbol;
    }

    public NameExpression(
        string name,
        ISourceLocation? location)
        : this(name, location, null, null, null)
    {
    }

    public override NameExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new NameExpression(
            this.Name,
            location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override NameExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new NameExpression(
            this.Name,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            diagnostics
            );

    public override NameExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new NameExpression(
            this.Name,
            this.Location,
            this.ReferencedSymbol,
            resultType,
            this.Diagnostics
            );

    public NameExpression WithName(string name) =>
        name == this.Name ? this :
        new NameExpression(
            name,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NameExpression WithReferencedSymbol(Symbol? referencedSymbol) =>
        referencedSymbol == this.ReferencedSymbol ? this :
        new NameExpression(
            this.Name,
            this.Location,
            referencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
    public override NameExpression RewriteChildren(SemanticRewriter rewriter) => this;
}