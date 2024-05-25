
namespace Parkour.Semantics;

using Symbols;

public class ConstructorDeclaration : MemberDeclaration
{
    public override ConstructorSymbol? Symbol { get; }

    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public LabelSymbol? ReturnLabel { get; }

    private ConstructorDeclaration(
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        ImmutableList<AttributeExpression> attributes,
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
            attributes,
            location,
            diagnostics)
    {
        this.Parameters = parameters;
        this.Body = body;
        this.Symbol = symbol;
        this.ReturnLabel = returnLabel;
    }

    public ConstructorDeclaration(
        ImmutableList<ParameterDeclaration> parameters,
        Expression body,
        ISourceLocation? location)
        : this(
              SymbolAccess.Public, 
              SymbolModifier.None, 
              ImmutableList<AttributeExpression>.Empty,
              parameters, 
              body, 
              location, 
              null, 
              null, 
              null)
    {
    }

    public override ConstructorDeclaration WithName(string name) =>
        this;

    public override ConstructorDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ConstructorDeclaration(
            this.Access,
            this.Modifiers,
            this.Attributes,
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
            this.Attributes,
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
            this.Attributes,
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
            this.Attributes,
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
            this.Attributes,
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
            this.Attributes,
            this.Parameters,
            this.Body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public override ConstructorDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes) =>
        attributes == this.Attributes ? this :
        new ConstructorDeclaration(
            this.Access,
            this.Modifiers,
            attributes,
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
            this.Attributes,
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
            this.Attributes,
            this.Parameters,
            body,
            this.Location,
            this.Symbol,
            this.ReturnLabel,
            this.Diagnostics
            );

    public override int ChildCount =>
        base.ChildCount + this.Parameters.Count + 1;

    public override SemanticElement? GetChild(int index)
    {
        if (index < base.ChildCount)
            return base.GetChild(index);
        index -= base.ChildCount;
        return index < this.Parameters.Count
            ? this.Parameters[index]
            : this.Body;
    }

    public override ConstructorDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var attributes = rewriter.Rewrite(this.Attributes);
        var parameters = rewriter.Rewrite(this.Parameters);
        var body = rewriter.Rewrite(this.Body);
        return this
            .WithAttributes(attributes)
            .WithParameters(parameters)
            .WithBody(body!);
    }
}
