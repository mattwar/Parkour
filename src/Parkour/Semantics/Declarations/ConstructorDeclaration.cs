
namespace Parkour.Semantics;

using Symbols;

public class ConstructorDeclaration : MemberDeclaration
{
    public override ConstructorSymbol? Symbol { get; }

    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public LabelSymbol? ReturnLabel { get; }

    public ConstructorDeclaration(
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        ImmutableList<ParameterDeclaration> parameters,
        Expression body,
        ISourceLocation? location,
        ConstructorSymbol? symbol,
        LabelSymbol? returnLabel,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(parameters) 
            | State(body)
            | NotNullState(symbol),
            modifiers.Contains(SymbolModifier.Static) ? ".cctor" : ".ctor",
            access,
            modifiers,
            location,
            diagnostics)
    {
        this.Parameters = parameters;
        this.Body = body;
        this.Symbol = symbol;
        this.ReturnLabel = returnLabel;
    }

    public override ConstructorDeclaration WithName(string name) =>
        this;

    public override ConstructorDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ConstructorDeclaration(
            this.Access,
            this.Modifiers,
            this.Parameters,
            this.Body,
            location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public ConstructorDeclaration WithSymbol(ConstructorSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new ConstructorDeclaration(
            this.Access,
            this.Modifiers,
            this.Parameters,
            this.Body,
            this.Location,
            symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public ConstructorDeclaration WithReturnLabel(LabelSymbol? returnLabel)=>
        returnLabel == this.ReturnLabel ? this :
        new ConstructorDeclaration(
            this.Access,
            this.Modifiers,
            this.Parameters,
            this.Body,
            this.Location,
            this.Symbol,
            returnLabel,
            this.Diagnostics
            );

    public override ConstructorDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ConstructorDeclaration(
            this.Access,
            this.Modifiers,
            this.Parameters,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            diagnostics
            );

    public override ConstructorDeclaration WithAccess(SymbolAccess access) =>
        access == this.Access ? this :
        new ConstructorDeclaration(
            access,
            this.Modifiers,
            this.Parameters,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public override ConstructorDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new ConstructorDeclaration(
            this.Access,
            modifiers,
            this.Parameters,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public ConstructorDeclaration WithParameters(ImmutableList<ParameterDeclaration> parameters) =>
        parameters == this.Parameters ? this :
        new ConstructorDeclaration(
            this.Access,
            this.Modifiers,
            parameters,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public ConstructorDeclaration WithBody(Expression body) =>
        body == this.Body ? this :
        new ConstructorDeclaration(
            this.Access,
            this.Modifiers,
            this.Parameters,
            body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public override int ChildCount =>
        this.Parameters.Count + 1;

    public override SemanticElement? GetChild(int index) =>
        index < this.Parameters.Count
            ? this.Parameters[index]
            : this.Body;

    public override ConstructorDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var parameters = rewriter.Rewrite(this.Parameters);
        var body = rewriter.Rewrite(this.Body);
        return this
            .WithParameters(parameters)
            .WithBody(body!);
    }
}
