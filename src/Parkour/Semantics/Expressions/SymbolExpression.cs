namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// References a symbol by looking it up by name in the current symbol table.
/// </summary>
public sealed class SymbolExpression : Expression
{
    internal protected override string DebugText => 
        $"{GetType().Name}: {ReferencedSymbol?.FullName ?? "???"} {ResultType?.FullName ?? "???"}";

    /// <summary>
    /// The full name of the symbol in the symbol table.
    /// </summary>
    public string Name { get; }

    public override Symbol? ReferencedSymbol { get; }

    private SymbolExpression(
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

    public SymbolExpression(
        string name,
        ISourceLocation? location)
        : this(name, location, null, null, null)
    {
    }

    public override SymbolExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new SymbolExpression(
            this.Name,
            location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override SymbolExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new SymbolExpression(
            this.Name,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            diagnostics
            );

    public override SymbolExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new SymbolExpression(
            this.Name,
            this.Location,
            this.ReferencedSymbol,
            resultType,
            this.Diagnostics
            );

    public SymbolExpression WithName(string name) =>
        name == this.Name ? this :
        new SymbolExpression(
            name,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public SymbolExpression WithReferencedSymbol(Symbol? referencedSymbol) =>
        referencedSymbol == this.ReferencedSymbol ? this :
        new SymbolExpression(
            this.Name,
            this.Location,
            referencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
    public override SymbolExpression RewriteChildren(SemanticRewriter rewriter) => this;
}
