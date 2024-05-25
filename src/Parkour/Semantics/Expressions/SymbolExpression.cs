namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// References a symbol via the current symbol table.
/// </summary>
public sealed class SymbolExpression : Expression
{
    internal protected override string DebugText => 
        $"{GetType().Name}: {ReferencedSymbol?.FullName ?? "???"} {ResultType?.FullName ?? "???"}";

    public string FullName { get; }
    public override Symbol? ReferencedSymbol { get; }

    public SymbolExpression(
        string fullName,
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
        this.FullName = fullName;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override SymbolExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new SymbolExpression(
            this.FullName,
            location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override SymbolExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new SymbolExpression(
            this.FullName,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            diagnostics
            );

    public override SymbolExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new SymbolExpression(
            this.FullName,
            this.Location,
            this.ReferencedSymbol,
            resultType,
            this.Diagnostics
            );

    public SymbolExpression WithFullName(string fullName) =>
        fullName == this.FullName ? this :
        new SymbolExpression(
            fullName,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public SymbolExpression WithReferencedSymbol(Symbol? referencedSymbol) =>
        referencedSymbol == this.ReferencedSymbol ? this :
        new SymbolExpression(
            this.FullName,
            this.Location,
            referencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
    public override SymbolExpression RewriteChildren(SemanticRewriter rewriter) => this;
}
