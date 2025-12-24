namespace Parkour.Semantics;

using Symbols;

public class TypeParameterDeclaration : Declaration
{
    public ImmutableList<AttributeExpression> Attributes { get; }
    public override TypeParameterSymbol? Symbol { get; }

    private TypeParameterDeclaration(
        string name,
        ImmutableList<AttributeExpression> attributes,
        ISourceLocation? location,
        TypeParameterSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullOrDiagnosticState(symbol, diagnostics),
            name,
            location,
            diagnostics)
    {
        this.Attributes = attributes;
        this.Symbol = symbol;
    }

    public TypeParameterDeclaration(
        string name,
        ISourceLocation? location)
        : this(
              name, 
              ImmutableList<AttributeExpression>.Empty,
              location, 
              null, 
              null)
    {
    }

    public override TypeParameterDeclaration WithName(string name) =>
        name == this.Name ? this :
        new TypeParameterDeclaration(
            name, 
            this.Attributes,
            this.Location, 
            this.Symbol,
            this.Diagnostics
            );

    public override TypeParameterDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new TypeParameterDeclaration(
            this.Name,
            this.Attributes,
            location, 
            this.Symbol,
            this.Diagnostics
            );

    public TypeParameterDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes) =>
        attributes == this.Attributes ? this :
        new TypeParameterDeclaration(
            this.Name,
            attributes,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public TypeParameterDeclaration WithSymbol(TypeParameterSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new TypeParameterDeclaration(
            this.Name,
            this.Attributes,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override TypeParameterDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new TypeParameterDeclaration(
            this.Name,
            this.Attributes,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override int ChildCount => this.Attributes.Count;
    
    public override SemanticElement? GetChild(int index) =>
        index >= 0 && index < this.Attributes.Count
            ? this.Attributes[index]
            : null;

    public override SemanticElement RewriteChildren(SemanticRewriter rewriter)
    {
        var attributes = rewriter.Rewrite(this.Attributes);
        return this.WithAttributes(attributes);
    }
}
