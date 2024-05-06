namespace Parkour.Semantics;

using Symbols;

public sealed class FieldDeclaration : MemberDeclaration
{
    public override FieldSymbol? Symbol { get; }

    public Expression FieldType { get; }
    public Expression? Initializer { get; }

    public FieldDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        Expression fieldType,
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

    public override FieldDeclaration WithName(string name) =>
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

    public override FieldDeclaration WithModifiers(SymbolModifier modifiers) =>
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

    public FieldDeclaration WithFieldType(Expression fieldType) =>
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
}

