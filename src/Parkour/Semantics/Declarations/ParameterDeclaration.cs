namespace Parkour.Semantics;
using Symbols;

public sealed class ParameterDeclaration : Declaration
{
    public BitSet<SymbolModifier> Modifiers { get; }
    public Expression? ParameterType { get; }

    public override ParameterSymbol? Symbol { get; }

    public ParameterDeclaration(
        string name, 
        BitSet<SymbolModifier> modifiers,
        Expression? parameterType,
        ISourceLocation? location,
        ParameterSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(parameterType)
            | NotNullState(symbol), 
            name, 
            location,
            diagnostics)
    {
        this.ParameterType = parameterType;
        this.Symbol = symbol;
    }

    public override ParameterDeclaration WithName(string name) =>
        name == this.Name ? this :
        new ParameterDeclaration(
            name,
            this.Modifiers,
            this.ParameterType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override ParameterDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ParameterDeclaration(
            this.Name,
            this.Modifiers,
            this.ParameterType,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public override ParameterDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ParameterDeclaration(
            this.Name,
            this.Modifiers,
            this.ParameterType,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public ParameterDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new ParameterDeclaration(
            this.Name,
            modifiers,
            this.ParameterType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public ParameterDeclaration WithParameterType(Expression parameterType) =>
        parameterType == this.ParameterType ? this :
        new ParameterDeclaration(
            this.Name,
            this.Modifiers,
            parameterType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public ParameterDeclaration WithSymbol(ParameterSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new ParameterDeclaration(
            this.Name,
            this.Modifiers,
            this.ParameterType,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        this.ParameterType;

    public override ParameterDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var type = rewriter.Rewrite(this.ParameterType);
        return this.WithParameterType(type!);
    }
}