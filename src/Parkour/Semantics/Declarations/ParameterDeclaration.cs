namespace Parkour.Semantics;
using Symbols;

public sealed class ParameterDeclaration : Declaration
{
    public BitSet<SymbolModifier> Modifiers { get; }
    public ImmutableList<AttributeExpression> Attributes { get; }
    public Expression? ParameterType { get; }

    public override ParameterSymbol? Symbol { get; }

    private ParameterDeclaration(
        string name, 
        BitSet<SymbolModifier> modifiers,
        ImmutableList<AttributeExpression> attributes,
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
        this.Modifiers = modifiers;
        this.Attributes = attributes;
        this.ParameterType = parameterType;
        this.Symbol = symbol;
    }

    public ParameterDeclaration(
        string name,
        Expression? parameterType,
        ISourceLocation? location)
        : this(
              name,
              SymbolModifier.None,
              ImmutableList<AttributeExpression>.Empty,
              parameterType, 
              location, 
              null, 
              null)
    {
    }

    public override ParameterDeclaration WithName(string name) =>
        name == this.Name ? this :
        new ParameterDeclaration(
            name,
            this.Modifiers,
            this.Attributes,
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
            this.Attributes,
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
            this.Attributes,
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
            this.Attributes,
            this.ParameterType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public ParameterDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes) =>
        attributes == this.Attributes ? this :
        new ParameterDeclaration(
            this.Name,
            this.Modifiers,
            attributes,
            this.ParameterType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public ParameterDeclaration WithParameterType(Expression? parameterType) =>
        parameterType == this.ParameterType ? this :
        new ParameterDeclaration(
            this.Name,
            this.Modifiers,
            this.Attributes,
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
            this.Attributes,
            this.ParameterType,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override int ChildCount => 
        this.Attributes.Count + 1;

    public override SemanticElement? GetChild(int index)
    {
        if (index < this.Attributes.Count)
            return this.Attributes[index];
        index -= this.Attributes.Count;
        return index == 0
            ? this.ParameterType
            : null;
    }

    public override ParameterDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var type = rewriter.Rewrite(this.ParameterType);
        return this.WithParameterType(type!);
    }
}