namespace Parkour.Semantics;

using Parkour;
using Symbols;

public sealed class FieldDeclaration : MemberDeclaration
{
    public override FieldSymbol? Symbol { get; }

    public Expression? FieldType { get; }
    public Expression? Initializer { get; }

    private FieldDeclaration(
        string name,
        Access access,
        BitSet<Modifier> modifiers,
        ImmutableList<AttributeExpression> attributes,
        Expression? fieldType,
        Expression? initializer,
        ISourceLocation? location,
        FieldSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
          State(initializer)
          | NotNullState(symbol),
          name,
          access,
          modifiers,
          attributes,
          location,
          diagnostics)
    {
        this.FieldType = fieldType;
        this.Initializer = initializer;
        this.Symbol = symbol;
    }

    public FieldDeclaration(
        string name,
        Expression? fieldType,
        Expression? initializer,
        ISourceLocation? location)
        : this(
              name,
              Access.Public,
              Modifier.None, 
              ImmutableList<AttributeExpression>.Empty,
              fieldType, 
              initializer, 
              location, 
              null, 
              null)
    {
    }

    public override FieldDeclaration WithName(string name) =>
        name == this.Name ? this :
        new FieldDeclaration(
            name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.FieldType,
            this.Initializer,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override FieldDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new FieldDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.FieldType,
            this.Initializer,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public FieldDeclaration WithSymbol(FieldSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new FieldDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.FieldType,
            this.Initializer,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override FieldDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new FieldDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.FieldType,
            this.Initializer,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override FieldDeclaration WithAccess(Access access) =>
        access == this.Access ? this :
        new FieldDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.Attributes,
            this.FieldType,
            this.Initializer,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override FieldDeclaration WithModifiers(BitSet<Modifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new FieldDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.Attributes,
            this.FieldType,
            this.Initializer,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override FieldDeclaration WithAttributes(ImmutableList<AttributeExpression> attributes) =>
        attributes == this.Attributes ? this :
        new FieldDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            attributes,
            this.FieldType,
            this.Initializer,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public FieldDeclaration WithFieldType(Expression? fieldType) =>
        fieldType == this.FieldType ? this :
        new FieldDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            fieldType,
            this.Initializer,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public FieldDeclaration WithInitializer(Expression? initializer) =>
        initializer == this.Initializer ? this :
        new FieldDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.Attributes,
            this.FieldType,
            initializer,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override int ChildCount => 
        base.ChildCount + 2;

    public override SemanticElement? GetChild(int index)
    {
        if (index < base.ChildCount)
            return base.GetChild(index);
        index -= base.ChildCount;
        return index switch
        {
            0 => this.FieldType,
            1 => this.Initializer,
            _ => null
        };
    }

    public override FieldDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var attributes = rewriter.Rewrite(this.Attributes);
        var type = rewriter.Rewrite(this.FieldType);
        var initializer = rewriter.Rewrite(this.Initializer);
        return this
            .WithAttributes(attributes)
            .WithFieldType(type!)
            .WithInitializer(initializer);
    }
}

