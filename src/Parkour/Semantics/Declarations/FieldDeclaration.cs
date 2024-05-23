namespace Parkour.Semantics;

using Symbols;

public sealed class FieldDeclaration : MemberDeclaration
{
    public override FieldSymbol? Symbol { get; }

    public Expression? FieldType { get; }
    public Expression? Initializer { get; }

    private FieldDeclaration(
        string name,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
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
              SymbolAccess.Public, 
              SymbolModifier.None, 
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
            this.FieldType,
            this.Initializer,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override FieldDeclaration WithAccess(SymbolAccess access) =>
        access == this.Access ? this :
        new FieldDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.FieldType,
            this.Initializer,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override FieldDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new FieldDeclaration(
            this.Name,
            this.Access,
            modifiers,
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
            this.FieldType,
            initializer,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.FieldType,
            1 => this.Initializer,
            _ => null
        };

    public override FieldDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var type = rewriter.Rewrite(this.FieldType);
        var initializer = rewriter.Rewrite(this.Initializer);
        return this
            .WithFieldType(type!)
            .WithInitializer(initializer);
    }
}

